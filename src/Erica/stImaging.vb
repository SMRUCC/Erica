
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.MIME.Html
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports STImaging
Imports STRaid

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
End Module
