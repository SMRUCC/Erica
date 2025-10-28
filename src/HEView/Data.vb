Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Framework

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

        If GetType(T) Is GetType(IHCCellScan) Then
            Call tbl.add("antibody", From cell As IHCCellScan In all Select cell.antibody)
        End If

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

        Return tbl
    End Function

    <Extension>
    Public Function CellGuid(cell As CellScan) As String
        Return New String() {cell.tile_id, CInt(cell.physical_x), CInt(cell.physical_y)}.JoinBy("-").MD5
    End Function
End Module
