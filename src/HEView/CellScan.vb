Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.ConcaveHull
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Math.MachineVision.CCL
Imports std = System.Math

Public Class CellScan

    Public Property x As Single
    Public Property y As Single
    Public Property physical As PointF
    Public Property area As Double
    Public Property ratio As Double
    Public Property scan_x As Double()
    Public Property scan_y As Double()

    Public Shared Iterator Function CellLookups(grid As BitmapBuffer, Optional offset As Point = Nothing) As IEnumerable(Of CellScan)
        Dim bin As BitmapBuffer = grid.ostuFilter(flip:=False)
        Dim CELLS = CCLabeling.Process(bin, background:=Color.White, 0).ToArray

        For Each shape As Polygon2D In CELLS
            Dim rect As RectangleF = shape.GetRectangle

            Yield New CellScan With {
                .area = rect.Width * rect.Height,
                .ratio = std.Max(rect.Width, rect.Height) / std.Min(rect.Width, rect.Height),
                .scan_x = shape.xpoints,
                .scan_y = shape.ypoints,
                .x = rect.X,
                .y = rect.Y,
                .physical = New PointF(.x + offset.X, .y + offset.Y)
            }
        Next
    End Function

    Public Function GetShape(Optional physical As Boolean = False) As PointF()
        Dim raw As PointF()

        If physical Then
            Dim offset_x = Me.physical.X - x
            Dim offset_y = Me.physical.Y - y

            raw = scan_x _
                .Select(Function(xi, i)
                            Return New PointF(xi + offset_x, scan_y(i) + offset_y)
                        End Function) _
                .ToArray
        Else
            raw = scan_x _
                .Select(Function(xi, i)
                            Return New PointF(xi, scan_y(i))
                        End Function) _
                .ToArray
        End If

        Return raw.ConcaveHull
    End Function

End Class
