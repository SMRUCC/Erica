Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Data.Framework
Imports Microsoft.VisualBasic.Data.Framework.IO
Imports Microsoft.VisualBasic.Linq

Public Module Data

    <Extension>
    Public Function AntibodyNameList(ihcCells As IHCCellScan()) As String()
        Return ihcCells _
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
    End Function

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
        Call tbl.add("label", From cell As CellScan In all Select cell.label)
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
        Call tbl.add("mean_distance", From cell As CellScan In all Select cell.average_dist)
        Call tbl.add("moran-I", From cell As CellScan In all Select cell.moranI)
        Call tbl.add("p-value", From cell As CellScan In all Select cell.pvalue)

        If GetType(T) Is GetType(IHCCellScan) Then
            Dim ihcCells As IHCCellScan() = all _
                .Select(Function(cell) DirectCast(cell, IHCCellScan)) _
                .ToArray
            Dim antibodyList As String() = ihcCells.AntibodyNameList

            For Each name As String In antibodyList
                Call tbl.add(name, From cell As IHCCellScan
                                   In ihcCells
                                   Select If(cell.antibody Is Nothing, 0.0, cell.antibody.TryGetValue(name)))
            Next
        End If

        Return tbl
    End Function

    <Extension>
    Public Function CellGuid(cell As CellScan) As String
        Return New String() {cell.tile_id, CInt(cell.physical_x), CInt(cell.physical_y)}.JoinBy("-").MD5
    End Function

    Public Iterator Function TableReader(s As Stream) As IEnumerable(Of CellScan)
        Dim df As DataFrameResolver = DataFrameResolver.Load(s)
        Dim ordinal As Index(Of String) = df.HeadTitles.Indexing
        Dim tile_id As Integer = ordinal("tile_id")
        Dim label As Integer = ordinal("label")
        Dim x As Integer = ordinal("x")
        Dim y As Integer = ordinal("y")
        Dim physical_x As Integer = ordinal("physical_x")
        Dim physical_y As Integer = ordinal("physical_y")
        Dim area As Integer = ordinal("area")
        Dim ratio As Integer = ordinal("ratio")
        Dim size As Integer = ordinal("size")
        Dim r1 As Integer = ordinal("r1")
        Dim r2 As Integer = ordinal("r2")
        Dim theta As Integer = ordinal("theta")
        Dim weight As Integer = ordinal("weight")
        Dim density As Integer = ordinal("density")
        Dim mean_distance As Integer = ordinal("mean_distance")
        Dim moran_I As Integer = ordinal("moran-I")
        Dim p_value As Integer = ordinal("p-value")

        Call ordinal.Delete("x", "y", "physical_x", "physical_y",
                            "area", "ratio", "size",
                            "r1", "r2", "theta",
                            "weight", "density", "mean_distance",
                            "moran-I", "p-value",
                            "tile_id", "label")

        Dim antibodies As SeqValue(Of String)() = ordinal.Where(Function(a) Not a.value.StringEmpty).ToArray
        Dim isIHCCells As Boolean = Not antibodies.IsNullOrEmpty

        Do While df.Read
            Dim cell As CellScan = If(isIHCCells,
                New IHCCellScan With {.antibody = New Dictionary(Of String, Double)},
                New CellScan
            )

            For Each antibody As SeqValue(Of String) In antibodies
                DirectCast(cell, IHCCellScan).antibody(CStr(antibody)) = df.GetDouble(CInt(antibody))
            Next

            cell.x = df.GetDouble(x)
            cell.y = df.GetDouble(y)
            cell.physical_x = df.GetDouble(physical_x)
            cell.physical_y = df.GetDouble(physical_y)
            cell.area = df.GetDouble(area)
            cell.ratio = df.GetDouble(ratio)
            cell.points = df.GetInt32(size)
            cell.r1 = df.GetDouble(r1)
            cell.r2 = df.GetDouble(r2)
            cell.weight = df.GetDouble(weight)
            cell.density = df.GetDouble(density)
            cell.average_dist = df.GetDouble(mean_distance)
            cell.moranI = df.GetDouble(moran_I)
            cell.pvalue = df.GetDouble(p_value)
            cell.tile_id = df.GetString(tile_id)
            cell.theta = df.GetDouble(theta)
            cell.label = df.GetString(label)

            Yield cell
        Loop
    End Function
End Module
