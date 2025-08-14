Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.GraphTheory.GridGraph
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Imaging.Physics
Imports Microsoft.VisualBasic.Math.Distributions
Imports Microsoft.VisualBasic.Math.Quantile
Imports std = System.Math

Public Module Math

    <Extension>
    Public Function MoranI(cells As IEnumerable(Of CellScan), Optional knn As Integer = 16) As CellScan()
        Dim all As CellScan() = cells.ToArray
        Dim averageR As Double = Aggregate cell As CellScan In all Into Average((cell.width + cell.height) / 2)
        Dim view As Grid(Of CellScan()) = all.EncodeGrid(radius:=averageR)

        For i As Integer = 0 To all.Length - 1
            Dim target = all(i)
            Dim nearby = view.SpatialLookup(target, averageR) _
                .OrderBy(Function(a) target.DistanceTo(a)) _
                .Take(knn) _
                .ToArray
            Dim data = nearby.Select(Function(a) a.ratio).ToArray
            Dim c1 = nearby.X
            Dim c2 = nearby.Y
            Dim moranVal = Moran.calc_moran(data, c1, c2)
            Dim pv As Double = pnorm.eval(moranVal.observed,
                                     mean:=moranVal.expected,
                                     sd:=moranVal.sd,
                                     resolution:=1000)

            If moranVal.observed <= -1 / (data.Length - 1) Then
                pv = 2 * pv
            Else
                pv = 2 * (1 - pv)
            End If

            If pv < 0 Then
                pv = 1 / Single.MaxValue
            ElseIf pv.IsNaNImaginary Then
                pv = 1
            End If

            target.density = data.Length / knn
            target.moranI = moranVal.observed
            target.pvalue = pv
        Next

        Dim minVal As Double = Aggregate cell As CellScan
                               In all
                               Where Not cell.moranI.IsNaNImaginary
                               Into Min(cell.moranI)

        For i As Integer = 0 To all.Length - 1
            If all(i).moranI.IsNaNImaginary Then
                all(i).moranI = minVal
            End If
        Next

        Return all
    End Function

    ''' <summary>
    ''' split the large region as cell cluster
    ''' </summary>
    ''' <param name="cells"></param>
    ''' <returns></returns>
    <Extension>
    Public Iterator Function Split(cells As IEnumerable(Of CellScan), Optional noise As Double = 0.25) As IEnumerable(Of CellScan)
        Dim all As CellScan() = cells.ToArray
        Dim q As QuantileEstimationGK = all.Select(Function(c) c.points).GKQuantile
        Dim filter As Double = q.Query(noise)

        all = all _
            .Where(Function(cell) cell.points > filter) _
            .ToArray

        Dim averagePt As Double = Aggregate cell As CellScan In all Into Average(cell.points)
        Dim maxR As Double = Aggregate cell As CellScan In all Into Average((cell.width + cell.height) / 2)
        Dim minR As Double = Aggregate cell As CellScan In all.Skip(all.Length / 3) Into Average((cell.width + cell.height) / 2)

        For Each cell As CellScan In all
            If cell.points <= averagePt Then
                Yield cell
                Continue For
            End If

            Dim region As PointF() = cell.scan_x _
                .Select(Function(xi, i) New PointF(xi, cell.scan_y(i))) _
                .ToArray
            Dim pack As New CirclePacker(region, New DoubleRange(minR, maxR))
            Dim centers As Polygon2D() = pack.PackCircles.ToArray
            Dim offset_x = cell.physical.X - cell.x
            Dim offset_y = cell.physical.Y - cell.y

            For Each center As Polygon2D In centers
                Dim rect As RectangleF = center.GetRectangle
                Dim cx As Double = center.xpoints.Average
                Dim cy As Double = center.ypoints.Average

                Yield New CellScan With {
                    .height = rect.Height,
                    .points = center.length,
                    .width = rect.Width,
                    .area = .width * .height,
                    .x = cx,
                    .y = cy,
                    .scan_x = center.xpoints,
                    .scan_y = center.ypoints,
                    .ratio = std.Max(.width, .height) / std.Min(.width, .height),
                    .physical = New PointF(.x + offset_x, .y + offset_y)
                }
            Next
        Next
    End Function
End Module
