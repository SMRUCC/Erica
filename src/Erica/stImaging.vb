
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.MIME.Html
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Interop
Imports STImaging
Imports STRaid

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

    <ExportAPI("plot_spots")>
    Public Function SpotPlot(spots As SpaceSpot(), matrix As MatrixViewer, geneId As String,
                             <RRawVectorArgument>
                             Optional size As Object = "3000,3000",
                             Optional env As Environment = Nothing) As Object

        Dim sizeVal = InteropArgumentHelper.getSize(size, env, [default]:="3000,3000")
        Dim render As New Render(matrix, spots)
        Dim layer As PixelData() = render.GetLayer(geneId).ToArray
        Dim theme As New Theme
        Dim app As New SpotPlot(layer, render.dimension, theme)

        Return app.Plot(sizeVal)
    End Function
End Module
