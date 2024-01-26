Imports Microsoft.VisualBasic.Data.GraphTheory.KNearNeighbors
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.Correlations

''' <summary>
''' WGCNA spearman weight score
''' </summary>
Public Class Spearman : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Dim pvalue As Double
        Dim cor As Double = Correlations.Spearman(x, y)

        Call Correlations.TestStats(cor, x.Length, 0.0, pvalue, 0.0, 0.0, 0.0, throwMaxIterError:=False)

        If pvalue >= 0.05 Then
            Return 0
        Else
            Return cor ^ 2
        End If
    End Function

    Public Overrides Function ToString() As String
        Return "spearman();"
    End Function
End Class