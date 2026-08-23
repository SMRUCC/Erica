Imports Microsoft.VisualBasic.Data.GraphTheory.Network



''' <summary>
''' 轻量级、可 JSON 序列化的无向加权图结构，用于在 Monocle3 各步骤之间
''' 缓存图数据（KNN 图、MST 主图、PAGA 图等）。运行时再按需转换为
''' <see cref="NetworkGraph(Of Integer)"/> 供 Louvain / PAGA / Dijkstra 使用。
''' </summary>
Public Class GraphData

    ''' <summary>
    ''' 节点标签（字符串 id）。索引隐含节点编号。
    ''' </summary>
    Public Property nodes As String()

    ''' <summary>
    ''' 边表：u、v 为节点索引（对应 <see cref="nodes"/> 下标），weight 为权重。
    ''' </summary>
    Public Property edges As EdgeData()

    Public Shared Function Build(nodes As String(), edges As EdgeData()) As GraphData
        Return New GraphData With {
            .nodes = nodes,
            .edges = edges
        }
    End Function

    ''' <summary>
    ''' 转换为 sciBASIC# 的 <see cref="NetworkGraph(Of Node, Edge(Of ?))"/>。
    ''' 节点 ID 在 NetworkGraph 构造时自动赋为顺序索引（从 0/1 起），
    ''' 因此样本 i 对应节点 ID = i + 1，可据此回溯。
    ''' </summary>
    Public Function ToNetworkGraph() As NetworkGraph(Of Node, Edge(Of Node))
        Dim nodeList(nodes.Length - 1) As Node
        Dim edgeList(edges.Length - 1) As Edge(Of Node)

        For i As Integer = 0 To nodes.Length - 1
            nodeList(i) = New Node() With {.label = nodes(i)}
        Next
        For i As Integer = 0 To edges.Length - 1
            Dim e = edges(i)
            edgeList(i) = New Edge(Of Node) With {
                .U = nodeList(e.u),
                .V = nodeList(e.v),
                .weight = e.weight
            }
        Next

        Return New NetworkGraph(Of Node, Edge(Of Node))(nodeList, edgeList)
    End Function
End Class

''' <summary>
''' 单条加权边（u、v 为端点节点在 GraphData.nodes 中的下标）。
''' </summary>
Public Class EdgeData
    Public Property u As Integer
    Public Property v As Integer
    Public Property weight As Double
End Class

