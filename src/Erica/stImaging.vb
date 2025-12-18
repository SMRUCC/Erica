
Imports System.Drawing
Imports Erica.Analysis.SpatialTissue.Imaging
Imports Erica.Analysis.SpatialTissue.RaidData
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Interop
Imports Render = Erica.Analysis.SpatialTissue.Imaging.Render

''' <summary>
''' do heatmap imaging of the STdata spots
''' </summary>
<Package("st-imaging")>
Module stImaging

    <ExportAPI("new_render")>
    Public Function createRender(h5ad As AnnData) As Render
        Return New Render(h5ad)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="raw">
    ''' the sample id should be the barcodes, and the row id is the gene id
    ''' </param>
    ''' <param name="spots"></param>
    ''' <returns></returns>
    <ExportAPI("as.STrender")>
    Public Function createRender(raw As Matrix, spots As SpatialSpot()) As Render
        Return New Render(New HTSMatrixViewer(raw), spots)
    End Function

    ''' <summary>
    ''' get gene layer raw data
    ''' </summary>
    ''' <param name="imaging"></param>
    ''' <param name="geneId"></param>
    ''' <returns></returns>
    <ExportAPI("gene_layer")>
    Public Function geneLayer(imaging As Render, geneId As String) As PixelData()
        Return imaging.GetLayer(geneId).ToArray
    End Function

    ''' <summary>
    ''' do imaging render of a specific gene expression layer
    ''' </summary>
    ''' <param name="render"></param>
    ''' <param name="geneId"></param>
    ''' <returns></returns>
    <ExportAPI("imaging")>
    Public Function imaging(render As Render, geneId As String) As Object
        Return render.Imaging(geneId)
    End Function

    <ExportAPI("spot_heatmap")>
    Public Function SpotHeatmap(spots As PixelData(),
                                <RRawVectorArgument>
                                Optional size As Object = "3000,3000",
                                Optional colorMaps As ScalerPalette = ScalerPalette.turbo,
                                Optional env As Environment = Nothing) As Object

        Dim sizeVal = InteropArgumentHelper.getSize(size, env, [default]:="3000,3000")
        Dim poly As New Polygon2D(spots.Select(Function(a) New PointF(a.X, a.Y)).ToArray)
        Dim theme As New Theme With {
            .colorSet = colorMaps.Description
        }
        Dim dimSize As New Size(poly.xpoints.Max, poly.ypoints.Max)
        Dim app As New SpotHeatMap(spots, dimSize, theme) With {
            .mapLevels = 120
        }

        Return app.Plot(sizeVal)
    End Function

    <ExportAPI("plot_spots")>
    Public Function SpotPlot(spots As SpatialSpot(), matrix As Object, geneId As String,
                             <RRawVectorArgument>
                             Optional size As Object = "3000,3000",
                             Optional spot_radius As Double = 13,
                             Optional colorMaps As ScalerPalette = ScalerPalette.turbo,
                             Optional env As Environment = Nothing) As Object

        Dim sizeVal = InteropArgumentHelper.getSize(size, env, [default]:="3000,3000")

        If TypeOf matrix Is Matrix Then
            matrix = New HTSMatrixViewer(DirectCast(matrix, Matrix))
        End If

        Dim render As New Render(DirectCast(matrix, MatrixViewer), spots)
        Dim layer As PixelData() = render.GetLayer(geneId).ToArray
        Dim theme As New Theme With {
            .pointSize = spot_radius,
            .colorSet = colorMaps.Description
        }
        Dim app As New SpotPlot(layer, render.dimension, theme)

        Return app.Plot(sizeVal)
    End Function
End Module
