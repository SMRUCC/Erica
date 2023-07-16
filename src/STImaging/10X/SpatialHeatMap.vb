Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.DataStorage.netCDF
Imports Microsoft.VisualBasic.DataStorage.netCDF.Components
Imports Microsoft.VisualBasic.DataStorage.netCDF.Data
Imports Microsoft.VisualBasic.DataStorage.netCDF.DataVector
Imports STData = STRaid.STRaid

Public Class SpatialHeatMap

    Public Property spots As SpotCell()
    Public Property dimension_size As Size

    Public Shared Function TotalSum(ST As STData) As SpatialHeatMap

    End Function

    Public Shared Sub WriteCDF(layer As SpatialHeatMap, file As Stream)
        Dim x As Integer() = layer.spots.Select(Function(i) i.X).ToArray
        Dim y As Integer() = layer.spots.Select(Function(i) i.Y).ToArray
        Dim scale As Double() = layer.spots.Select(Function(i) i.Scale).ToArray
        Dim spot_size As New Dimension With {.name = "spot_size", .size = scale.Length}

        Using buf As New CDFWriter(file)
            Call buf.GlobalAttributes(New attribute With {.name = "scan_x", .type = CDFDataTypes.INT, .value = layer.dimension_size.Width})
            Call buf.GlobalAttributes(New attribute With {.name = "scan_y", .type = CDFDataTypes.INT, .value = layer.dimension_size.Height})
            Call buf.GlobalAttributes(New attribute With {.name = "spots", .type = CDFDataTypes.INT, .value = layer.spots.Length})

            Call buf.AddVariable("x", New integers(x), spot_size)
            Call buf.AddVariable("y", New integers(y), spot_size)
            Call buf.AddVector("heatmap", scale, spot_size)
        End Using
    End Sub

    Public Shared Function LoadCDF(file As Stream) As SpatialHeatMap
        Using read As New netCDFReader(file)
            Dim nspots As Integer = read.getAttribute("spots")
            Dim scan_x As Integer = read.getAttribute("scan_x")
            Dim scan_y As Integer = read.getAttribute("scan_y")
            Dim x As Integer() = DirectCast(read.getDataVariable("x"), integers)
            Dim y As Integer() = DirectCast(read.getDataVariable("y"), integers)
            Dim scale As Double() = DirectCast(read.getDataVariable("heatmap"), doubles)

            Return New SpatialHeatMap With {
                .dimension_size = New Size(scan_x, scan_y),
                .spots = x _
                    .Select(Function(xi, i)
                                Return New SpotCell With {
                                    .Scale = scale(i),
                                    .X = xi,
                                    .Y = y(i)
                                }
                            End Function) _
                    .ToArray
            }
        End Using
    End Function

End Class
