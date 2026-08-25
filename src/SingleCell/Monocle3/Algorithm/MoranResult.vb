
''' <summary>
''' Moran's I 评估结果缓存对象。
''' </summary>
Public Class MoranResult
    Public Property globalPseudotimeI As Double
    Public Property topVariableGenes As (gene As String, moranI As Double)()
End Class
