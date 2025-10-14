Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.GraphTheory.GridGraph
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Imaging.Physics
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.Distributions
Imports Microsoft.VisualBasic.Math.Quantile
Imports Microsoft.VisualBasic.Math.Statistics.Linq
Imports std = System.Math

Public Module Math

    <Extension>
    Public Function MoranI(cells As IEnumerable(Of CellScan), Optional knn As Integer = 16) As CellScan()
        Dim all As CellScan() = cells.ToArray

        ' skip of the possible blank collection
        ' if scan on a blank white image
        If all.IsNullOrEmpty Then
            Return all
        End If

        ' median of the cell radius
        Dim medianR As Double = (From cell As CellScan
                                 In all.AsParallel
                                 Select ((cell.width + cell.height) / 2)).Median
        If medianR < 5 Then
            medianR = 5
        End If

        Dim view As Grid(Of CellScan()) = all.EncodeGrid(radius:=medianR)
        Dim cutoff As Double = medianR * 2

        Call "evaluate the cells population moran-I".info

        For Each i As Integer In TqdmWrapper.Range(0, all.Length, wrap_console:=App.EnableTqdm)
            Dim target As CellScan = all(i)
            Dim nearby = view.SpatialLookup(target, medianR) _
                .OrderBy(Function(a) target.DistanceTo(a)) _
                .Take(knn) _
                .ToArray
            Dim averageDist As Double = If(nearby.Length = 0, 0, nearby.Average(Function(a) target.DistanceTo(a)))
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

            target.average_dist = averageDist
            target.density = data.Length / knn
            target.moranI = moranVal.observed
            target.pvalue = pv
        Next

        Dim real As CellScan() = all _
            .Where(Function(cell)
                       Return Not cell.moranI.IsNaNImaginary
                   End Function) _
            .ToArray
        Dim minVal As Double

        If real.Length > 0 Then
            minVal = Aggregate cell As CellScan
                     In real
                     Into Min(cell.moranI)
        Else
            minVal = 0
        End If

        For i As Integer = 0 To all.Length - 1
            If all(i).moranI.IsNaNImaginary Then
                all(i).moranI = minVal
            End If
        Next

        Return all
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="cells"></param>
    ''' <param name="noise">quantile level for filter the polygon shape points. all cell shapes which has its shape points less than this quantile level will be treated as noise</param>
    ''' <returns></returns>
    <Extension>
    Public Function FilterNoise(cells As IEnumerable(Of CellScan), Optional noise As Double = 0.25) As CellScan()
        Dim all As CellScan() = cells.ToArray
        Dim q As QuantileEstimationGK = all.Select(Function(c) c.points).GKQuantile
        Dim filter As Integer = CInt(q.Query(noise))

        all = all _
            .Where(Function(cell) cell.points > filter) _
            .ToArray

        Return all
    End Function

    ''' <summary>
    ''' split the large region as cell cluster
    ''' </summary>
    ''' <param name="cells"></param>
    ''' <param name="noise">
    ''' the quantile level of the cell size, all cells that with cell size less than this quantile cutoff will be treated as noised.
    ''' </param>
    ''' <returns></returns>
    <Extension>
    Public Iterator Function Split(cells As IEnumerable(Of CellScan)) As IEnumerable(Of CellScan)
        Dim all As CellScan() = cells.ToArray

        If all.Length = 0 Then
            ' no data if scan on a blank white image
            Return
        End If

        Dim averagePt As Double = Aggregate cell As CellScan In all Into Average(cell.points)
        Dim maxR As Double = Aggregate cell As CellScan In all Into Average((cell.width + cell.height) / 2)
        Dim minR As Double = Aggregate cell As CellScan
                             In all.Skip(all.Length / 3)
                             Into Average((cell.width + cell.height) / 2)

        Call "split the large cell block".info

        For Each cell As CellScan In TqdmWrapper.Wrap(all)
            If cell.points <= averagePt Then
                Yield cell
                Continue For
            End If

            Dim region As PointF() = cell.scan_x _
                .Select(Function(xi, i) New PointF(xi, cell.scan_y(i))) _
                .ToArray
            Dim pack As New CirclePacker(region, New DoubleRange(minR, maxR))
            Dim centers As Polygon2D() = pack.PackCircles.ToArray
            Dim offset_x = cell.physical_x - cell.x
            Dim offset_y = cell.physical_y - cell.y

            For Each center As Polygon2D In centers
                Dim rect As RectangleF = center.GetRectangle
                Dim cx As Double = center.xpoints.Average
                Dim cy As Double = center.ypoints.Average
                Dim shape = center.GetFillPoints.ToArray

                Yield New CellScan With {
                    .height = rect.Height,
                    .points = center.length,
                    .width = rect.Width,
                    .area = .width * .height,
                    .x = cx,
                    .y = cy,
                    .scan_x = shape.X.ToArray,
                    .scan_y = shape.Y.ToArray,
                    .ratio = std.Max(.width, .height) / std.Min(.width, .height),
                    .physical_x = .x + offset_x,
                    .physical_y = .y + offset_y
                }
            Next
        Next
    End Function
End Module
