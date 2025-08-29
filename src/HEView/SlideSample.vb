Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports std = System.Math

Public Class SlideSample

    Public Property x As Vector
    Public Property y As Vector
    Public Property intensity As Vector()

    Public Shared Function Render(slide As SlideSample, Optional heatmap As Integer = -1, Optional dims As Size? = Nothing) As Bitmap
        If dims Is Nothing Then
            Dim xlim = slide.x.Range.MinMax
            Dim ylim = slide.y.Range.MinMax

            dims = New Size(xlim(1) * 1.25, ylim(1) * 1.25)
        End If

        Dim heat As Double()

        If heatmap > -1 Then
            heat = slide.intensity(heatmap)
        Else
            heat = slide.intensity(0) _
                .Select(Function(v, i)
                            Return slide.intensity _
                                .Select(Function(x) x(i)) _
                                .Average
                        End Function) _
                .ToArray
        End If

        Dim pixels As PixelData() = heat _
            .Select(Function(val, i) New PixelData(slide.x(i), slide.y(i), val)) _
            .ToArray
        Dim raster As New PixelRender(ScalerPalette.Jet.Description, 30, Color.White)
        Dim img As Bitmap = raster.RenderRasterImage(pixels, dims)

        Return img
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="theta">旋转角度（弧度）</param>
    ''' <param name="tx">平移量</param>
    ''' <param name="ty">平移量</param>
    ''' <param name="sx">缩放因子</param>
    ''' <param name="sy">缩放因子</param>
    ''' <returns></returns>
    Public Function Transform(theta As Single, tx As Single, ty As Single, sx As Single, sy As Single) As SlideSample
        Dim x_scaled = x * sx
        Dim y_scaled = y * sy
        ' 旋转（逆时针旋转theta弧度）
        Dim x_rot = x_scaled * std.Cos(theta) - y_scaled * std.Sin(theta)
        Dim y_rot = x_scaled * std.Sin(theta) + y_scaled * std.Cos(theta)
        ' 平移
        Dim x_new = x_rot + tx
        Dim y_new = y_rot + ty

        Return New SlideSample With {
            .x = x_new,
            .y = y_new,
            .intensity = intensity.ToArray
        }
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Function RasterizePoints(xmin As Single, xmax As Single, ymin As Single, ymax As Single, res As Single) As Double()()()
        Return SlideRasterize.RasterizePoints(Me, xmin, xmax, ymin, ymax, res)
    End Function
End Class
