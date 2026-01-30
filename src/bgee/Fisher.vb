Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Math
Imports SMRUCC.genomics.Analysis.HTS.GSEA

Public Module Fisher

    <Extension>
    Public Function Enrichment(bgee As BgeeDiskReader, geneSet As String(), development_stage As String) As EnrichmentResult()
        Dim result As New List(Of EnrichmentResult)
        Dim clusterIDs = bgee.anatomicalIDs

        For Each id As String In clusterIDs
            Dim size As Integer = -1
            Dim hits = bgee.Anatomical(id, geneSet, development_stage, size:=size).ToArray
            Dim enrich As EnrichmentResult = bgee _
                .AnatomicalModel(id, size) _
                .calcResult(
                    enriched:=hits,
                    inputSize:=geneSet.Length,
                    genes:=bgee.backgroundSize,
                    outputAll:=False
                )

            If Not enrich Is Nothing Then
                result.Add(enrich)
            End If
        Next

        Return result.FDR.ToArray
    End Function
End Module
