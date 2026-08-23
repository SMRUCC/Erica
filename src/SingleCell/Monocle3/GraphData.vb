Namespace SMRUCC.genomics.SingleCell.Monocle3

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
        ''' 转换为 sciBASIC# 的 <see cref="NetworkGraph(Of Integer)"/>。
        ''' 节点 data 设为其在 nodes 数组中的下标，便于按样本索引回溯。
        ''' </summary>
        Public Function ToNetworkGraph() As NetworkGraph(Of Integer)
            Dim nodeList(nodes.Length - 1) As Node(Of Integer)
            Dim edgeList(edges.Length - 1) As Edge(Of Integer)

            For i As Integer = 0 To nodes.Length - 1
                nodeList(i) = New Node(Of Integer)(nodes(i), i)
            Next
            For i As Integer = 0 To edges.Length - 1
                Dim e = edges(i)
                edgeList(i) = New Edge(Of Integer)(nodeList(e.u), nodeList(e.v), e.weight)
            Next

            Return New NetworkGraph(Of Integer, Edge(Of Integer))(nodeList, edgeList)
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
End Namespace
