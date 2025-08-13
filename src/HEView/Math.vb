Imports Microsoft.VisualBasic.Data.GraphTheory.GridGraph
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Imaging.Physics

Public Module Math

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

        Next
    End Function

End Module
