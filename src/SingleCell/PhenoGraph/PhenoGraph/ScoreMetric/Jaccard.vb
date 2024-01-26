Imports Microsoft.VisualBasic.Data.GraphTheory.KNearNeighbors

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
