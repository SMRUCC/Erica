
Imports System.IO
Imports bgee
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.IO.MessagePack
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.GSEA
Imports SMRUCC.genomics.Assembly.Uniprot.XML
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object

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
        Return bgee.CreateTissueBackground
    End Function

    <ExportAPI("write.backgroundPack")>
    Public Function saveBackgroundPack(background As Background, file As String) As Boolean
        Using buffer As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)
            Call MsgPackSerializer.SerializeObject(background, buffer)
            Call buffer.Flush()
        End Using

        Return True
    End Function

    <ExportAPI("read.backgroundPack")>
    Public Function readBackgroundPack(file As String) As Background
        Using buffer As Stream = file.Open(FileMode.Open, doClear:=False, [readOnly]:=True)
            Return MsgPackSerializer.Deserialize(Of Background)(buffer)
        End Using
    End Function

    <ExportAPI("metabolomicsMapping")>
    Public Function metabolomicsMapping(uniprot As pipeline, geneExpressions As Background, Optional env As Environment = Nothing) As Background
        Dim maps = uniprot.populates(Of entry)(env).CatalystMapping
        Dim metabolites = geneExpressions.BackgroundConversion(maps)

        Return metabolites
    End Function

End Module
