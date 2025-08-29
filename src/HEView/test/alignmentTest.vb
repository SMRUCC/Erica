Imports System.Drawing
Imports HEView
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Parallel
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
Imports std = System.Math

Module alignmentTest

    Private Function createDemo() As SlideSample
        Dim img As BitmapBuffer

        Using gfx As IGraphics = DriverLoad.CreateDefaultRasterGraphics(New Size(1200, 900), Color.Transparent)
            Call gfx.FillRectangle(Brushes.Violet, New Rectangle(30, 60, 200, 200))
            Call gfx.FillCircles(Brushes.SkyBlue, {New PointF(150, 200), New PointF(30, 30)}, 10)

            Call gfx.Flush()

            img = DirectCast(DirectCast(gfx, GdiRasterGraphics).ImageResource, SkiaImage).ToBitmap.GetMemoryBuffer
        End Using

        Dim vx As New List(Of Double)
        Dim vy As New List(Of Double)
        Dim vr As New List(Of Double)
        Dim vg As New List(Of Double)
        Dim vb As New List(Of Double)

        For x As Integer = 0 To img.Width - 1
            For y As Integer = 0 To img.Height - 1
                Dim c As Color = img.GetPixel(x, y)

                If Not c.IsTransparent Then
                    Call vx.Add(x + 1)
                    Call vy.Add(y + 1)
                    Call vr.Add(c.R)
                    Call vg.Add(c.G)
                    Call vb.Add(c.B)
                End If
            Next
        Next

        Return New SlideSample With {
            .x = New Vector(vx),
            .y = New Vector(vy),
            .intensity = {New Vector(vr), New Vector(vg), New Vector(vb)}
        }
    End Function

    Sub New()
        VectorTask.n_threads = 20
    End Sub

    Sub test()
        Dim ref = createDemo()
        Dim sample = ref.Transform(std.PI * 1.33, 36, -99, 1.2, 0.988)
        Dim result = SlideAlignment.MakeAlignment(ref, sample, maxit:=100)

        Call SlideSample.Render(ref).Save("Z:/ref.bmp")
        Call SlideSample.Render(result).Save("Z:/align.bmp")

        Pause()
    End Sub
End Module
