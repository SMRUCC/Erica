''' <summary>
''' PAGA（partition-based graph abstraction）风格的团簇连接图抽象。
''' 
''' 说明：原 sciBASIC# 的 <see cref="Microsoft.VisualBasic.Data.visualize.Network.Analysis.PAGA"/>
''' 接收的是 Datavisualization.Network 的非泛型 NetworkGraph，并要求节点 metadata 中携带
''' cluster 类型信息，与本项目使用的 NetworkGraph(Of Integer) 类型不兼容。为避免类型耦合
''' 与不可控的序列化问题，这里实现其数学本质：基于 KNN 图与 cluster 标签，统计每对团簇
''' 之间的连接强度（共享边数），构建团簇级连接图。结果缓存为 08_paga_graph.json。
''' </summary>
Public Class PAGAGraph

    ''' <summary>
    ''' 基于 KNN 图与 cluster 标签抽象出团簇级连接图。
    ''' 连接权重 = 两个 cluster 之间在 KNN 图中的共享边数量（归一化到 [0,1]）。
    ''' </summary>
    Public Shared Function Abstract(knn As GraphData, clusters As Integer(), opts As Monocle3Options, cache As CacheStore) As GraphData
        Dim key = "08_paga_graph.json"

        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
            Call Console.WriteLine($"[cache] load PAGA graph from {cache.Path(key)}")
            Return cache.LoadGraph(key)
        End If

        Dim n = clusters.Length
        ' 重新编号 cluster 为连续 0..k-1
        Dim distinct = clusters.Distinct.OrderBy(Function(c) c).ToArray
        Dim k = distinct.Length
        Dim idMap As New Dictionary(Of Integer, Integer)
        For i As Integer = 0 To k - 1
            idMap(distinct(i)) = i
        Next

        ' 统计 cluster 对之间的共享边数
        Dim conn(k - 1, k - 1) As Integer
        For Each e In knn.edges
            Dim cu = idMap(clusters(e.u))
            Dim cv = idMap(clusters(e.v))
            If cu <> cv Then
                conn(cu, cv) += 1
                conn(cv, cu) += 1
            End If
        Next

        ' 归一化
        Dim maxConn = 0
        For a As Integer = 0 To k - 1
            For b As Integer = 0 To k - 1
                If conn(a, b) > maxConn Then maxConn = conn(a, b)
            Next
        Next

        Dim nodes(k - 1) As String
        For i As Integer = 0 To k - 1
            nodes(i) = distinct(i).ToString
        Next
        Dim edges As New List(Of EdgeData)
        For a As Integer = 0 To k - 1
            For b As Integer = a + 1 To k - 1
                If conn(a, b) > 0 Then
                    Dim w = If(maxConn > 0, conn(a, b) / CDbl(maxConn), 0.0)
                    edges.Add(New EdgeData With {
                        .u = a,
                        .v = b,
                        .weight = w
                    })
                End If
            Next
        Next

        Dim graph = GraphData.Build(nodes, edges.ToArray)
        Call cache.SaveGraph(key, graph)
        Call Console.WriteLine($"[paga] done: {edges.Count} cluster connections -> cached {cache.Path(key)}")

        Return graph
    End Function
End Class

