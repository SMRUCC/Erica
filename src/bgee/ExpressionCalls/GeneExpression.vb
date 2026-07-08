
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Class GeneExpression

    Public Property data As String
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_high_quality As Integer
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_low_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_high_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_low_quality As Integer
    Public Property observed_data As String

    Public Shared Function MakeMatrix(bgeeCalls As IEnumerable(Of AdvancedCalls)) As Matrix
        Dim expressions As New Dictionary(Of String, List(Of AdvancedCalls))

        For Each geneExpr As AdvancedCalls In bgeeCalls.SafeQuery
            If geneExpr.expression = "absent" Then
                Continue For
            End If

            Dim sample_id As String = $"{geneExpr.anatomicalName}-{geneExpr.developmental_stage}"

            If Not expressions.ContainsKey(sample_id) Then
                expressions.Add(sample_id, New List(Of AdvancedCalls) From {geneExpr})
            Else
                expressions(sample_id).Add(geneExpr)
            End If
        Next

        Dim bgeeSamples As NamedValue(Of Dictionary(Of String, Double))() = expressions _
            .Select(Function(sample)
                        Return New NamedValue(Of Dictionary(Of String, Double))(sample.Key, sample.Value.ToDictionary(Function(a) a.geneID, Function(a) Val(a.expression_rank)))
                    End Function) _
            .ToArray

        Return MatrixBuilder.BuildAndNormalizeAbundanceMatrix(bgeeSamples)
    End Function

End Class