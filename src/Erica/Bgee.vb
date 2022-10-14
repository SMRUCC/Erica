Imports bgee
Imports Microsoft.VisualBasic.CommandLine.Reflection
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

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="bgee"></param>
    ''' <param name="geneSet"></param>
    ''' <param name="development_stage">
    ''' this function returns the enrichment result for all existed development stages
    ''' </param>
    ''' <returns>
    ''' the return data value is depends on the parameter 
    ''' value of <paramref name="development_stage"/>.
    ''' 
    ''' + for a specific development_stage id, this function 
    '''   just returns a enrichment result dataframe
    ''' + for default value, a list that contains enrichment 
    '''   result for each development_stage will be returned
    '''   
    ''' </returns>
    <ExportAPI("bgee_calls")>
    <RApiReturn(GetType(EnrichmentResult))>
    Public Function bgeeCalls(bgee As BgeeDiskReader, geneSet As String(), Optional development_stage As String = "*") As Object
        If development_stage = "*" Then
            Dim enrichSet As New list With {.slots = New Dictionary(Of String, Object)}

            For Each id As String In bgee.developmentalIDs
                enrichSet.add(id, bgee.Enrichment(geneSet, development_stage:=id))
            Next

            Return enrichSet
        Else
            Return bgee.Enrichment(geneSet, development_stage)
        End If
    End Function

    <ExportAPI("development_stage")>
    Public Function development_stage(bgee As BgeeDiskReader) As dataframe
        Dim id As String() = bgee.developmentalIDs
        Dim clusters = id.Select(Function(d) bgee.DevelopmentalModel(d)).ToArray
        Dim name = clusters.Select(Function(c) c.names).ToArray
        Dim size = clusters.Select(Function(c) c.size).ToArray

        Return New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"stage_id", id},
                {"development_stage", name},
                {"cluster_size", size}
            },
            .rownames = id
        }
    End Function

    <ExportAPI("anatomicalIDs")>
    Public Function anatomicalIDs(bgee As BgeeDiskReader) As dataframe
        Dim id As String() = bgee.anatomicalIDs
        Dim clusters = id.Select(Function(d) bgee.AnatomicalModel(d)).ToArray
        Dim name = clusters.Select(Function(c) c.names).ToArray
        Dim size = clusters.Select(Function(c) c.size).ToArray

        Return New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"anatomicalID", id},
                {"anatomical_name", name},
                {"cluster_size", size}
            },
            .rownames = id
        }
    End Function

    <ExportAPI("geneIDs")>
    Public Function geneIDList(bgee As BgeeDiskReader) As dataframe
        Dim geneIDs = bgee.GetAllgeneIDs
        Dim geneID As String() = geneIDs.Keys.ToArray
        Dim geneName As String() = geneID _
            .Select(Function(id) geneIDs(id)) _
            .ToArray
        Dim maps As New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"geneID", geneID},
                {"name", geneName}
            },
            .rownames = geneID
        }

        Return maps
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
