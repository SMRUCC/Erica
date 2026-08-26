
''' <summary>
''' 单条加权边（u、v 为端点节点在 GraphData.nodes 中的下标）。
''' </summary>
Public Class EdgeData

    Public Property u As Integer
    Public Property v As Integer
    Public Property weight As Double

    Public Overrides Function ToString() As String
        Return $"[{u}->{v}, w={weight}]"
    End Function
End Class
