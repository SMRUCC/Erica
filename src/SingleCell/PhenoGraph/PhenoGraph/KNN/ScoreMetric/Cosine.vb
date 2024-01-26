Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.LinearAlgebra

Public Class Cosine : Inherits ScoreMetric

    Public Overrides Function eval(x() As Double, y() As Double) As Double
        Return New Vector(x).SSM(New Vector(y))
    End Function

    Public Overrides Function ToString() As String
        Return "cosine();"
    End Function

End Class