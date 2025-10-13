Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.ConcaveHull
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Math.MachineVision.CCL
Imports std = System.Math

Public Class CellScan : Implements Layout2D

    Public Property x As Double Implements Layout2D.X
    Public Property y As Double Implements Layout2D.Y

    ' 20250904 PointF can not be serialized in bson handler
    ' decompose the PointF as physical_x and physical_y

    Public Property physical_x As Double
    Public Property physical_y As Double
    Public Property area As Double
    Public Property ratio As Double
    Public Property scan_x As Double()
    Public Property scan_y As Double()
    Public Property moranI As Double
    Public Property pvalue As Double
    Public Property points As Integer
    Public Property width As Double
    Public Property height As Double
    Public Property density As Double
    Public Property average_dist As Double

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="grid"></param>
    ''' <param name="offset"></param>
    ''' <param name="binary_processing">
    ''' make ostu binary processing of the input image in this function?
    ''' </param>
    ''' <returns></returns>
    Public Shared Iterator Function CellLookups(grid As BitmapBuffer,
                                                Optional offset As Point = Nothing,
                                                Optional binary_processing As Boolean = True) As IEnumerable(Of CellScan)

        Dim bin As BitmapBuffer = If(binary_processing, grid.ostuFilter(flip:=False), grid)
        Dim CELLS = CCLabeling.Process(bin, background:=Color.White, 0).ToArray

        For Each shape As Polygon2D In CELLS
            Dim rect As RectangleF = shape.GetRectangle

            If rect.Width = 0.0 OrElse rect.Height = 0.0 Then
                Continue For
            End If

            Yield New CellScan With {
                .area = rect.Width * rect.Height,
                .ratio = std.Max(rect.Width, rect.Height) / std.Min(rect.Width, rect.Height),
                .scan_x = shape.xpoints,
                .scan_y = shape.ypoints,
                .x = rect.X,
                .y = rect.Y,
                .physical_x = .x + offset.X,
                .physical_y = .y + offset.Y,
                .points = shape.length,
                .height = rect.Height,
                .width = rect.Width
            }
        Next
    End Function

    Public Function GetShape(Optional physical As Boolean = False) As PointF()
        Dim raw As PointF()

        If physical Then
            Dim offset_x = Me.physical_x - x
            Dim offset_y = Me.physical_y - y

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
