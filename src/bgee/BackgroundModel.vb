Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Text.Xml.Models
Imports SMRUCC.genomics.Analysis.HTS.GSEA

Public Module BackgroundModel

    <Extension>
    Public Function CreateTissueBackground(bgee As IEnumerable(Of AdvancedCalls)) As Background
        Dim tissues = bgee.GroupBy(Function(gene) gene.anatomicalID).ToArray
        Dim background As New Background With {
            .build = Now,
            .clusters = tissues _
                .Select(Function(genes) genes.TissueCluster) _
                .ToArray
        }

        Return background
    End Function

    <Extension>
    Private Function TissueCluster(genes As IEnumerable(Of AdvancedCalls)) As Cluster
        Dim geneList = genes.GroupBy(Function(gene) gene.geneID).ToArray
        Dim tissue = geneList.First.First
        Dim memberGenes As BackgroundGene() = geneList _
            .Select(Function(gene)
                        Return New BackgroundGene With {
                            .accessionID = gene.Key,
                            .[alias] = {gene.First.gene_name},
                            .locus_tag = New NamedValue With {.name = gene.Key, .text = gene.First.gene_name},
                            .name = gene.First.gene_name,
                            .term_id = {}
                        }
                    End Function) _
            .ToArray

        Return New Cluster With {
            .ID = tissue.anatomicalID,
            .names = tissue.anatomicalName,
            .description = .names,
            .members = memberGenes
        }
    End Function
End Module
