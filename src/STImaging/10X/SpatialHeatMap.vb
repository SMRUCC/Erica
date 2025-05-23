﻿Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.DataStorage.netCDF
Imports Microsoft.VisualBasic.DataStorage.netCDF.Components
Imports Microsoft.VisualBasic.DataStorage.netCDF.Data
Imports Microsoft.VisualBasic.DataStorage.netCDF.DataVector
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports STData = SMRUCC.genomics.Analysis.Spatial.RAID.STRaid

Public Class SpatialHeatMap

    Public Property spots As SpotCell()
    Public Property dimension_size As Size
    Public Property offset As Point

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function Max(ST As STData) As SpatialHeatMap
        Return Aggregate(ST, f:=Function(x) x.Max)
    End Function

    Public Shared Function Aggregate(ST As STData, f As Func(Of DataFrameRow, Double)) As SpatialHeatMap
        Dim shape As New Polygon2D(ST.spots)
        Dim cells As New List(Of SpotCell)

        For i As Integer = 0 To ST.spots.Length - 1
            Dim x As DataFrameRow = ST.matrix.expression(i)
            Dim d As Double = f(x)
            Dim xy = ST.spots(i)
            Dim spot As New SpotCell With {
                .Scale = d,
                .X = xy.X,
                .Y = xy.Y,
                .Barcode = x.geneID
            }

            Call cells.Add(spot)
        Next

        Return New SpatialHeatMap With {
            .spots = cells.ToArray,
            .dimension_size = shape.GetDimension,
            .offset = New Point(shape.xpoints.Min, shape.ypoints.Min)
        }
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function TotalSum(ST As STData) As SpatialHeatMap
        Return Aggregate(ST, f:=Function(x) x.Sum)
    End Function

    Public Shared Sub WriteCDF(layer As SpatialHeatMap, file As Stream)
        Dim x As Integer() = layer.spots.Select(Function(i) i.X).ToArray
        Dim y As Integer() = layer.spots.Select(Function(i) i.Y).ToArray
        Dim scale As Double() = layer.spots.Select(Function(i) i.Scale).ToArray
        Dim spot_size As New Dimension With {.name = "spot_size", .size = scale.Length}
        Dim barcode As String() = layer.spots.Select(Function(i) i.Barcode).ToArray

        Using buf As New CDFWriter(file)
            Call buf.GlobalAttributes(New attribute With {.name = "scan_x", .type = CDFDataTypes.NC_INT, .value = layer.dimension_size.Width})
            Call buf.GlobalAttributes(New attribute With {.name = "scan_y", .type = CDFDataTypes.NC_INT, .value = layer.dimension_size.Height})
            Call buf.GlobalAttributes(New attribute With {.name = "offset_x", .type = CDFDataTypes.NC_INT, .value = layer.offset.X})
            Call buf.GlobalAttributes(New attribute With {.name = "offset_y", .type = CDFDataTypes.NC_INT, .value = layer.offset.Y})
            Call buf.GlobalAttributes(New attribute With {.name = "spots", .type = CDFDataTypes.NC_INT, .value = layer.spots.Length})

            Call buf.AddVariable("x", New integers(x), spot_size)
            Call buf.AddVariable("y", New integers(y), spot_size)
            Call buf.AddVector("heatmap", scale, spot_size)
            Call buf.AddVariable("barcode", New chars(barcode), spot_size)
        End Using
    End Sub

    Public Shared Function LoadCDF(file As Stream) As SpatialHeatMap
        Using read As New netCDFReader(file)
            Dim nspots As Integer = read.getAttribute("spots")
            Dim scan_x As Integer = read.getAttribute("scan_x")
            Dim scan_y As Integer = read.getAttribute("scan_y")
            Dim offset_x As Integer = read.getAttribute("offset_x")
            Dim offset_y As Integer = read.getAttribute("offset_y")
            Dim x As Integer() = DirectCast(read.getDataVariable("x"), integers)
            Dim y As Integer() = DirectCast(read.getDataVariable("y"), integers)
            Dim scale As Double() = DirectCast(read.getDataVariable("heatmap"), doubles)
            Dim barcode As String() = DirectCast(read.getDataVariable("barcode"), chars).LoadJSON(Of String())

            Return New SpatialHeatMap With {
                .dimension_size = New Size(scan_x, scan_y),
                .spots = x _
                    .Select(Function(xi, i)
                                Return New SpotCell With {
                                    .Scale = scale(i),
                                    .X = xi,
                                    .Y = y(i),
                                    .Barcode = barcode(i)
                                }
                            End Function) _
                    .ToArray,
                .offset = New Point(offset_x, offset_y)
            }
        End Using
    End Function

End Class
