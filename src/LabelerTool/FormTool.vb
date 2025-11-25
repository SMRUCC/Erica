Imports System.Drawing.Drawing2D
Imports System.IO
Imports HEView
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports std = System.Math

Public Class FormTool

    Dim allDataObjects As CellScan()
    Dim sizeOriginal As Size
    Dim polygonPoints As New List(Of Point)
    Dim renderedBitmap As Image
    Dim currentMousePos As Point

    Dim worldBounds As RectangleF
    Dim bitmapSize As Size

    Public ReadOnly Property isDrawing As Boolean
        Get
            Return ToolStripButton1.Checked
        End Get
    End Property

    Const defaultTitle As String = "Cell Label Tool"

    Private Sub FormTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call SkiaDriver.Register()
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Using file As New OpenFileDialog With {.Filter = "Excel Table(*.csv)|*.csv"}
            If file.ShowDialog = DialogResult.OK Then
                allDataObjects = HEView.Data _
                    .TableReader(file.FileName.Open(FileMode.Open, doClear:=False, [readOnly]:=True)) _
                    .ToArray

                With Rasterizer.MeasureRasterRange(allDataObjects)
                    worldBounds = New RectangleF(0, 0, .width.Max, .height.Max)
                End With

                Call RenderDataToBitmap(file.FileName)

                Text = $"{defaultTitle} [{file.FileName}]"
                ToolStripStatusLabel1.Text = file.FileName
                ToolStripButton1.Checked = True
            End If
        End Using
    End Sub

    Private Sub RenderDataToBitmap(sourcedata As String)
        Dim factor As Double = 20
        Dim max As Double = 1500

        If worldBounds.Width > worldBounds.Height Then
            ' scale by width
            Dim w = std.Max(max, worldBounds.Width / factor)
            Dim scale As Double = worldBounds.Width / w
            Dim h = worldBounds.Height / scale

            bitmapSize = New Size(w, h)
        Else
            Dim h = std.Max(max, worldBounds.Height / factor)
            Dim scale As Double = worldBounds.Height / h
            Dim w = worldBounds.Width / scale

            bitmapSize = New Size(w, h)
        End If

        Using g As IGraphics = DriverLoad.CreateDefaultRasterGraphics(bitmapSize, fill_color:=Color.White)
            ' 计算从世界坐标到位图坐标的缩放比例
            Dim scaleX As Single = CSng(bitmapSize.Width / worldBounds.Width)
            Dim scaleY As Single = CSng(bitmapSize.Height / worldBounds.Height)

            For Each obj In allDataObjects
                ' 世界坐标 -> 位图坐标
                Dim bmpX As Single = CSng(obj.physical_x * scaleX)
                Dim bmpY As Single = CSng(obj.physical_y * scaleY)
                g.FillEllipse(Brushes.Blue, bmpX - 1, bmpY - 1, 2, 2)
            Next

            renderedBitmap = DirectCast(g, GdiRasterGraphics).ImageResource
        End Using

        sourcedata = $"{sourcedata.ParentPath}/{sourcedata.BaseName}_renderedBitmap.png"

        renderedBitmap.SaveAs(sourcedata)
        PictureBox1.BackgroundImage = System.Drawing.Image.FromStream(New MemoryStream(sourcedata.ReadBinary))
    End Sub

    ' 【核心】将 PictureBox 坐标转换为世界坐标
    Private Function PictureBoxToWorldPoint(p As Point) As PointF
        If PictureBox1.Image Is Nothing Then Return Nothing

        ' 1. 获取PictureBox上图像的实际显示区域和缩放比
        Dim imgRect As Rectangle = GetImageDisplayRect(PictureBox1)
        If imgRect.Width = 0 OrElse imgRect.Height = 0 Then Return Nothing

        ' 2. PictureBox坐标 -> 位图坐标
        ' 考虑到图像可能居中，需要减去偏移量，再除以缩放比
        Dim bmpX As Single = (p.X - imgRect.Left) / (imgRect.Width / bitmapSize.Width)
        Dim bmpY As Single = (p.Y - imgRect.Top) / (imgRect.Height / bitmapSize.Height)

        ' 3. 位图坐标 -> 世界坐标
        Dim worldX As Double = (bmpX / bitmapSize.Width) * worldBounds.Width
        Dim worldY As Double = (bmpY / bitmapSize.Height) * worldBounds.Height

        Return New PointF(CSng(worldX), CSng(worldY))
    End Function

    ' 辅助函数：获取PictureBox在Zoom模式下，图片的实际显示矩形
    Private Function GetImageDisplayRect(pb As PictureBox) As Rectangle
        Dim rect As New Rectangle()
        Dim img As Bitmap = renderedBitmap

        If img Is Nothing Then Return rect

        ' 计算缩放比例
        Dim scaleX As Single = CSng(pb.ClientSize.Width / img.Width)
        Dim scaleY As Single = CSng(pb.ClientSize.Height / img.Height)
        Dim scale As Single = std.Min(scaleX, scaleY) ' Zoom模式使用较小的缩放比

        ' 计算缩放后的尺寸
        rect.Width = CInt(img.Width * scale)
        rect.Height = CInt(img.Height * scale)

        ' 计算居中后的位置
        rect.X = (pb.ClientSize.Width - rect.Width) / 2
        rect.Y = (pb.ClientSize.Height - rect.Height) / 2

        Return rect
    End Function

    ' 【核心】判断点是否在多边形内部 (射线法)
    Private Function IsPointInPolygon(point As PointF, polygon As List(Of PointF)) As Boolean
        If polygon.Count < 3 Then Return False

        Dim p1 As PointF, p2 As PointF
        Dim isInside As Boolean = False

        For i As Integer = 0 To polygon.Count - 1
            p1 = polygon(i)
            p2 = polygon((i + 1) Mod polygon.Count)

            If point.Y > std.Min(p1.Y, p2.Y) AndAlso point.Y <= std.Max(p1.Y, p2.Y) AndAlso
               point.X <= std.Max(p1.X, p2.X) AndAlso
               p1.Y <> p2.Y Then
                Dim xinters As Single = (point.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X
                If p1.X = p2.X OrElse point.X <= xinters Then
                    isInside = Not isInside
                End If
            End If
        Next

        Return isInside
    End Function

    ' --- 5. 事件处理 ---

    ' 鼠标点击：添加多边形顶点
    Private Sub PictureBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseDown
        If Not isDrawing Then Return
        If e.Button = MouseButtons.Left Then
            polygonPoints.Add(e.Location)
            PictureBox1.Invalidate() ' 触发重绘
        End If
    End Sub

    ' 绘制事件：绘制背景图和用户的多边形
    Private Sub PictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles PictureBox1.Paint
        ' 先绘制背景位图
        If PictureBox1.Image IsNot Nothing Then
            e.Graphics.DrawImage(PictureBox1.Image, GetImageDisplayRect(PictureBox1))
        End If

        ' 再绘制多边形
        If polygonPoints.Count > 0 Then
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using pen As New System.Drawing.Pen(Color.Red, 2)
                If polygonPoints.Count > 1 Then
                    e.Graphics.DrawLines(pen, polygonPoints.ToArray())
                End If
                ' 绘制顶点
                For Each p In polygonPoints
                    e.Graphics.FillEllipse(System.Drawing.Brushes.Red, p.X - 3, p.Y - 3, 6, 6)
                Next
            End Using
        End If
    End Sub

    ' 完成多边形按钮
    Private Sub btnCompletePolygon_Click(sender As Object, e As EventArgs) Handles btnCompletePolygon.Click
        If polygonPoints.Count < 3 Then
            ToolStripStatusLabel1.Text = "请至少绘制3个点来构成一个多边形。"
            Return
        End If
        If String.IsNullOrWhiteSpace(txtLabel.Text) Then
            ToolStripStatusLabel1.Text = "请输入要应用的标签。"
            Return
        End If

        ToolStripButton1.Checked = False ' 停止绘制

        ' 1. 将PictureBox上的多边形顶点转换到世界坐标系
        Dim worldPolygon As New List(Of PointF)
        For Each p In polygonPoints
            worldPolygon.Add(PictureBoxToWorldPoint(p))
        Next

        ' 2. 遍历所有原始数据，判断是否在多边形内
        Dim labeledCount As Integer = 0
        For Each obj As CellScan In allDataObjects
            If IsPointInPolygon(New PointF(CSng(obj.x), CSng(obj.y)), worldPolygon) Then
                obj.Label = txtLabel.Text
                labeledCount += 1
            End If
        Next

        Dim msg As String = $"成功为 {labeledCount} 个对象打上标签 '{txtLabel.Text}'。"

        ToolStripStatusLabel1.Text = msg
        MessageBox.Show(msg, "标注完成", MessageBoxButtons.OK, MessageBoxIcon.Information)

        ' (可选) 重新渲染以高亮显示已标注的点
        ' RenderDataToBitmap() ' 如果需要根据标签改变颜色，可以在这里修改渲染逻辑
        ' PictureBox1.Invalidate()
    End Sub

    ' 清除多边形按钮
    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        polygonPoints.Clear()
        ToolStripButton1.Checked = True
        PictureBox1.Invalidate() ' 清除屏幕上的多边形
    End Sub
End Class
