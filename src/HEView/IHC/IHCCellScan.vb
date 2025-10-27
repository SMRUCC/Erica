Public Class IHCCellScan : Inherits CellScan

    ''' <summary>
    ''' the antibody name used in IHC staining
    ''' </summary>
    ''' <returns></returns>
    Public Property antibody As String

    Protected Overrides Function Clone() As CellScan
        Return New IHCCellScan With {
            .area = area,
            .average_dist = average_dist,
            .density = density,
            .r2 = r2,
            .moranI = moranI,
            .physical_x = physical_x,
            .physical_y = physical_y,
            .points = points,
            .pvalue = pvalue,
            .ratio = ratio,
            .scan_x = scan_x,
            .scan_y = scan_y,
            .tile_id = tile_id,
            .weight = weight,
            .r1 = r1,
            .x = x,
            .y = y,
            .antibody = antibody
        }
    End Function

End Class
