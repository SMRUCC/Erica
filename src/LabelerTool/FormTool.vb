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
    Dim isDrawing As Boolean = False
    Dim currentMousePos As Point

    Dim worldBounds As RectangleF
    Dim bitmapSize As Size

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

                Call RenderDataToBitmap()
            End If
        End Using
    End Sub

    Private Sub RenderDataToBitmap()
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

        Call renderedBitmap.SaveAs("./renderedBitmap.png")

        PictureBox1.BackgroundImage = renderedBitmap.CTypeGdiImage
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
End Class
