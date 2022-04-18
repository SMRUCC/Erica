Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Text.Xml.Models
Imports SMRUCC.genomics.Analysis.HTS.GSEA

Public Module BackgroundModel

    <Extension>
    Public Function CreateTissueBackground(bgee As IEnumerable(Of AdvancedCalls)) As Background
        Dim tissues = bgee _
            .Where(Function(gene) gene.expression = "present") _
            .GroupBy(Function(gene)
                         Return gene.anatomicalID
                     End Function) _
            .ToArray
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
                        Dim symbol = gene.First
                        Dim geneName = symbol.gene_name.Trim(""""c)

                        Return New BackgroundGene With {
                            .accessionID = gene.Key,
                            .[alias] = {geneName},
                            .locus_tag = New NamedValue With {.name = gene.Key, .text = geneName},
                            .name = geneName,
                            .term_id = {}
                        }
                    End Function) _
            .ToArray

        Return New Cluster With {
            .ID = tissue.anatomicalID.Trim(""""c),
            .names = tissue.anatomicalName.Trim(""""c),
            .description = .names,
            .members = memberGenes
        }
    End Function
End Module
