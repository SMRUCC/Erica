Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Framework

Public Module Data

    <Extension>
    Public Function Tabular(cells As IEnumerable(Of CellScan)) As DataFrame
        Dim all As CellScan() = cells.ToArray
        Dim tbl As New DataFrame With {
            .rownames = all _
                .Select(Function(c)
                            Return $"{c.physical_x},{c.physical_y}".MD5
                        End Function) _
                .ToArray
        }

        Call tbl.add("x", From cell As CellScan In all Select cell.x)
        Call tbl.add("y", From cell As CellScan In all Select cell.y)
        Call tbl.add("physical_x", From cell As CellScan In all Select cell.physical_x)
        Call tbl.add("physical_y", From cell As CellScan In all Select cell.physical_y)
        Call tbl.add("area", From cell As CellScan In all Select cell.area)
        Call tbl.add("ratio", From cell As CellScan In all Select cell.ratio)
        Call tbl.add("size", From cell As CellScan In all Select cell.points)
        Call tbl.add("r1", From cell As CellScan In all Select cell.r1)
        Call tbl.add("r2", From cell As CellScan In all Select cell.r2)
        Call tbl.add("density", From cell As CellScan In all Select cell.density)
        Call tbl.add("moran-I", From cell As CellScan In all Select cell.moranI)
        Call tbl.add("p-value", From cell As CellScan In all Select cell.pvalue)

        Return tbl
    End Function
End Module
