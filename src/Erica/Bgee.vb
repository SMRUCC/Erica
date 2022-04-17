
Imports bgee
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.GSEA

''' <summary>
''' the bgee database toolkit
''' </summary>
<Package("Bgee")>
Public Module Bgee

    Public Function bgeeCalls(advCalls As String)

    End Function

    <ExportAPI("parseTsv")>
    Public Function parseTsv(file As String, Optional advance As Boolean = False) As AdvancedCalls()
        If advance Then
            Return AdvancedCalls.ParseTable(file).ToArray
        Else
            Return AdvancedCalls.ParseSimpleTable(file).ToArray
        End If
    End Function

    ''' <summary>
    ''' create tissue and cell background based on bgee database
    ''' </summary>
    ''' <param name="bgee"></param>
    ''' <returns></returns>
    <ExportAPI("tissue_background")>
    Public Function TissueBackground(bgee As AdvancedCalls()) As Background
        Dim tissues = bgee.GroupBy(Function(gene) gene.anatomicalID).ToArray

    End Function

End Module
