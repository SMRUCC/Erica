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
    Dim renderedBitmap As Bitmap
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
        If worldBounds.Width > worldBounds.Height Then
            ' scale by width
            Dim w = std.Max(3000, worldBounds.Width / 10)
            Dim scale As Double = worldBounds.Width / w
            Dim h = worldBounds.Height / scale

            bitmapSize = New Size(w, h)
        Else
            Dim h = std.Max(3000, worldBounds.Height / 10)
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
                Dim bmpX As Single = CSng(obj.x * scaleX)
                Dim bmpY As Single = CSng(obj.y * scaleY)
                g.FillEllipse(Brushes.Blue, bmpX - 1, bmpY - 1, 2, 2)
            Next
        End Using
    End Sub
End Class
