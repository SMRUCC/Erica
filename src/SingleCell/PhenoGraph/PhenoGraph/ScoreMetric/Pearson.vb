Imports Microsoft.VisualBasic.Data.GraphTheory.KNearNeighbors
Imports Microsoft.VisualBasic.Math.Correlations

''' <summary>
''' WGCNA pearson weight score
''' </summary>
Public Class Pearson : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Dim pvalue As Double
        Dim cor As Double = GetPearson(x, y, pvalue, throwMaxIterError:=False)

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