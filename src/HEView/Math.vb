Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.GraphTheory.GridGraph
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Imaging.Physics
Imports Microsoft.VisualBasic.Math.Distributions

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
            target.moranI = If(moranVal.observed.IsNaNImaginary, -100, moranVal.observed)
            target.pvalue = pv
        Next

        Return all
    End Function

    ''' <summary>
    ''' split the large region as cell cluster
    ''' </summary>
    ''' <param name="cells"></param>
    ''' <returns></returns>
    <Extension>
    Public Function Split(cells As IEnumerable(Of CellScan)) As IEnumerable(Of CellScan)

    End Function

End Module
