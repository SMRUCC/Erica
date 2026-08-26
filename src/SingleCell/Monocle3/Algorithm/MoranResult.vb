
''' <summary>
''' Moran's I 评估结果缓存对象。
''' </summary>
Public Class MoranResult
    Public Property globalPseudotimeI As Double
    Public Property topVariableGenes As VariantGene()
End Class

Public Class VariantGene

    Public Property gene As String
    Public Property moranI As Double

    Sub New()
    End Sub

    Sub New(gene As String, moranI As Double)
        _gene = gene
        _moranI = moranI
    End Sub

    Public Overrides Function ToString() As String
        Return $"{gene}, moran-I={moranI}"
    End Function

End Class