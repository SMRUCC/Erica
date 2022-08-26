
Imports Microsoft.VisualBasic.CommandLine.Reflection
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
End Module
