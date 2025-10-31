Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Framework
Imports Microsoft.VisualBasic.Linq

Public Module Data

    <Extension>
    Public Function Tabular(Of T As CellScan)(cells As IEnumerable(Of T)) As DataFrame
        Dim all As CellScan() = cells.ToArray
        Dim tbl As New DataFrame With {
            .rownames = all _
                .Select(Function(c)
                            Return c.CellGuid
                        End Function) _
                .ToArray
        }

        Call tbl.add("tile_id", From cell As CellScan In all Select cell.tile_id)
        Call tbl.add("x", From cell As CellScan In all Select cell.x)
        Call tbl.add("y", From cell As CellScan In all Select cell.y)
        Call tbl.add("physical_x", From cell As CellScan In all Select cell.physical_x)
        Call tbl.add("physical_y", From cell As CellScan In all Select cell.physical_y)
        Call tbl.add("area", From cell As CellScan In all Select cell.area)
        Call tbl.add("ratio", From cell As CellScan In all Select cell.ratio)
        Call tbl.add("size", From cell As CellScan In all Select cell.points)
        Call tbl.add("r1", From cell As CellScan In all Select cell.r1)
        Call tbl.add("r2", From cell As CellScan In all Select cell.r2)
        Call tbl.add("theta", From cell As CellScan In all Select cell.theta)
        Call tbl.add("weight", From cell As CellScan In all Select cell.weight)
        Call tbl.add("density", From cell As CellScan In all Select cell.density)
        Call tbl.add("moran-I", From cell As CellScan In all Select cell.moranI)
        Call tbl.add("p-value", From cell As CellScan In all Select cell.pvalue)

        If GetType(T) Is GetType(IHCCellScan) Then
            Dim ihcCells As IHCCellScan() = all _
                .Select(Function(cell) DirectCast(cell, IHCCellScan)) _
                .ToArray
            Dim antibodyList As String() = ihcCells _
                .Select(Function(c) As IEnumerable(Of String)
                            If c.antibody Is Nothing Then
                                Return Nothing
                            Else
                                Return c.antibody.Keys
                            End If
                        End Function) _
                .IteratesALL _
                .Distinct _
                .ToArray

            For Each name As String In antibodyList
                Call tbl.add(name, From cell As IHCCellScan
                                   In all
                                   Select If(cell.antibody Is Nothing, 0.0, cell.antibody(name)))
            Next
        End If

        Return tbl
    End Function

    <Extension>
    Public Function CellGuid(cell As CellScan) As String
        Return New String() {cell.tile_id, CInt(cell.physical_x), CInt(cell.physical_y)}.JoinBy("-").MD5
    End Function
End Module
