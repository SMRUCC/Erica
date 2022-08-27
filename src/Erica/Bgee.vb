
Imports System.IO
Imports bgee
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.IO.MessagePack
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.GSEA
Imports SMRUCC.genomics.Assembly.Uniprot.XML
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop

''' <summary>
''' the bgee database toolkit
''' </summary>
<Package("Bgee")>
Public Module Bgee

    <ExportAPI("bgee_calls")>
    Public Function bgeeCalls(bgee As BgeeDiskReader, geneSet As String(), Optional development_stage As String = "*") As EnrichmentResult()
        Dim result As New List(Of EnrichmentResult)
        Dim clusterIDs = bgee.anatomicalIDs.ToArray

        For Each id As String In clusterIDs
            Dim size As Integer = -1
            Dim hits = bgee.Anatomical(id, geneSet, development_stage, size:=size).ToArray
            Dim enrich = bgee.AnatomicalModel(id, size).calcResult(hits, geneSet.Length, bgee.backgroundSize, outputAll:=False)

            If Not enrich Is Nothing Then
                result.Add(enrich)
            End If
        Next

        Return result.ToArray
    End Function

    <ExportAPI("parseTsv")>
    <RApiReturn(GetType(AdvancedCalls))>
    Public Function parseTsv(file As String,
                             Optional advance As Boolean = False,
                             Optional quality As String = "*",
                             Optional pip_stream As Boolean = False,
                             Optional env As Environment = Nothing) As Object

        Dim println = env.WriteLineHandler
        Dim stream As IEnumerable(Of AdvancedCalls)

        If quality.StringEmpty OrElse quality = "*" Then
            println("load bgee expression calls with no quality condition...")
        Else
            println($"load bgee expression calls under '{quality}' quality condition...")
        End If

        If advance Then
            stream = AdvancedCalls.ParseTable(file)
        Else
            stream = AdvancedCalls.ParseSimpleTable(file, quality)
        End If

        If pip_stream Then
            Return pipeline.CreateFromPopulator(stream)
        Else
            Return stream.ToArray
        End If
    End Function

    ''' <summary>
    ''' create tissue and cell background based on bgee database
    ''' </summary>
    ''' <param name="bgee"></param>
    ''' <returns></returns>
    <ExportAPI("tissue_background")>
    Public Function TissueBackground(bgee As AdvancedCalls(), Optional env As Environment = Nothing) As Background
        Dim println = env.WriteLineHandler
        Call println($"create tissue enrichment background model based on {bgee.Length} bgee gene expression calls!")
        Return bgee.CreateTissueBackground
    End Function

    <ExportAPI("write.backgroundPack")>
    <RApiReturn(GetType(Boolean))>
    Public Function saveBackgroundPack(<RRawVectorArgument>
                                       background As Object,
                                       file As String,
                                       Optional env As Environment = Nothing) As Object

        Dim data As pipeline = pipeline.TryCreatePipeline(Of AdvancedCalls)(background, env)

        If data.isError Then
            Return data.getError
        End If

        Using disk As New BgeeDiskWriter(file)
            Call disk.Push(data.populates(Of AdvancedCalls)(env))
        End Using

        Return True
    End Function

    <ExportAPI("read.backgroundPack")>
    Public Function readBackgroundPack(file As String) As BgeeDiskReader
        Return New BgeeDiskReader(file)
    End Function

    <ExportAPI("metabolomicsMapping")>
    Public Function metabolomicsMapping(uniprot As pipeline, geneExpressions As Background, Optional env As Environment = Nothing) As Background
        Dim maps = uniprot.populates(Of entry)(env).CatalystMapping
        Dim metabolites = geneExpressions.BackgroundConversion(maps)

        Return metabolites
    End Function

End Module
