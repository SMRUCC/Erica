Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.Correlations
Imports Microsoft.VisualBasic.Math.LinearAlgebra

Public MustInherit Class ScoreMetric

    ''' <summary>
    ''' the score function should produce a positive score value,
    ''' higher score value is better
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <returns></returns>
    Public MustOverride Function eval(x As Double(), y As Double()) As Double

    Public Overrides Function ToString() As String
        Return "knn_score_metric();"
    End Function

End Class

Public Class Cosine : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Return New Vector(x).SSM(New Vector(y))
    End Function

    Public Overrides Function ToString() As String
        Return "cosine();"
    End Function

End Class

Public Class Jaccard : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Dim j As Integer
        Dim u As Integer = x.Length

        For i As Integer = 0 To x.Length - 1
            If x(i) > 0 AndAlso y(i) > 0 Then
                j += 1
            End If
        Next

        Return j / u
    End Function

    Public Overrides Function ToString() As String
        Return "jaccard();"
    End Function
End Class

''' <summary>
''' WGCNA pearson weight score
''' </summary>
Public Class Pearson : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Dim pvalue As Double
        Dim cor As Double = GetPearson(x, y, pvalue)

        If pvalue >= 0.05 Then
            Return 0
        Else
            Return cor ^ 2
        End If
    End Function

    Public Overrides Function ToString() As String
        Return "pearson();"
    End Function
End Class