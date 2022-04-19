Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Text.Xml.Models
Imports SMRUCC.genomics.Analysis.HTS.GSEA
Imports SMRUCC.genomics.Assembly.Uniprot.XML

Public Module MetabolomicsMapping

    ''' <summary>
    ''' convert from gene names to metabolite id
    ''' </summary>
    ''' <param name="geneBackground"></param>
    ''' <returns></returns>
    <Extension>
    Public Function BackgroundConversion(geneBackground As Background, maps As Dictionary(Of String, String())) As Background
        Dim tissueList As Cluster() = geneBackground _
            .clusters _
            .Select(Function(c)
                        Return createTissueMapping(tissue:=c, maps:=maps)
                    End Function) _
            .Where(Function(tissue) Not tissue.members.IsNullOrEmpty) _
            .ToArray

        Return New Background With {
            .build = Now,
            .id = "",
            .comments = "",
            .name = "",
            .clusters = tissueList
        }
    End Function

    Private Function createTissueMapping(tissue As Cluster, maps As Dictionary(Of String, String())) As Cluster
        Return New Cluster With {
            .ID = tissue.ID,
            .description = tissue.description,
            .names = tissue.names,
            .members = tissue.members _
                .Where(Function(gene) maps.ContainsKey(gene.name)) _
                .Select(Function(gene) maps(gene.name)) _
                .IteratesALL _
                .Distinct _
                .Select(Function(id)
                            Return New BackgroundGene With {
                                .accessionID = id,
                                .[alias] = {id},
                                .locus_tag = New NamedValue With {.name = id, .text = id},
                                .name = id,
                                .term_id = {id}
                            }
                        End Function) _
                .ToArray
        }
    End Function

    ''' <summary>
    ''' create mapping of gene name to chebi id
    ''' </summary>
    ''' <param name="uniprot"></param>
    ''' <returns></returns>
    ''' 
    <Extension>
    Public Function CatalystMapping(uniprot As IEnumerable(Of entry)) As Dictionary(Of String, String())
        Dim maps As New Dictionary(Of String, List(Of String))

        uniprot = From protein As entry
                  In uniprot
                  Where Not protein.comments Is Nothing
                  Where protein.gene IsNot Nothing AndAlso protein.gene.names IsNot Nothing
                  Where protein.comments _
                      .Where(Function(c)
                                 Return c.type = "catalytic activity"
                             End Function) _
                      .Any

        For Each protein As entry In uniprot
            Dim chebiId As String() = protein.comments _
                .Where(Function(c)
                           Return c.type = "catalytic activity" AndAlso Not c.reaction Is Nothing
                       End Function) _
                .Select(Function(c) c.reaction.dbReferences) _
                .IteratesALL _
                .Where(Function(ref) ref.type = "ChEBI") _
                .Select(Function(ref) ref.id) _
                .Distinct _
                .ToArray

            For Each name As value In protein.gene.names
                If Not maps.ContainsKey(name.value) Then
                    Call maps.Add(name.value, New List(Of String))
                End If

                Call maps(name.value).AddRange(chebiId)
            Next
        Next

        Return maps _
            .ToDictionary(Function(gene) gene.Key,
                          Function(gene)
                              Return gene _
                                  .Value _
                                  .Distinct _
                                  .ToArray
                          End Function)
    End Function

End Module
