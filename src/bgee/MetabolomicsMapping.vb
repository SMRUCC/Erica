Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.genomics.Analysis.HTS.GSEA
Imports SMRUCC.genomics.Assembly.Uniprot.XML

Public Module MetabolomicsMapping

    <Extension>
    Public Function BackgroundConversion(geneBackground As Background) As Background

    End Function

    ''' <summary>
    ''' create mapping of gene name to chebi id
    ''' </summary>
    ''' <param name="uniprot"></param>
    ''' <returns></returns>
    ''' 
    <Extension>
    Public Function CatalystMapping(uniprot As IEnumerable(Of entry)) As Dictionary(Of String, List(Of String))
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

        Return maps
    End Function

End Module
