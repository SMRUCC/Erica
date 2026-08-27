Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.GraphTheory.Network
Imports std = System.Math
Imports System.Threading.Tasks

''' <summary>
''' 基于 PCA 主成分空间的距离构建 K 近邻（KNN）图。
''' 节点=样本索引；边权重=1/(1+欧氏距离)（距离越近权重越大）。
''' 结果以轻量 <see cref="GraphData"/> 形式缓存为 04_knn_graph.json。
''' </summary>
Public Class NearestNeighborGraph

    ''' <summary>
    ''' 在 PCA 空间构建 KNN 图（无向，保留两端互为近邻的边）。
    ''' </summary>
    Public Shared Function BuildKNN(score As Double(,), opts As Monocle3Options, cache As CacheStore) As GraphData
        Dim key = "04_knn_graph.json"

        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
            Call Console.WriteLine($"[cache] load KNN graph from {cache.Path(key)}")
            Return cache.LoadGraph(key)
        End If

        Dim n = score.GetLength(0)
        Dim ndim = score.GetLength(1)
        Call Console.WriteLine($"[knn] building KNN graph (k={opts.knnK}) on {n} samples ...")

        Dim nodes(n - 1) As String
        For i As Integer = 0 To n - 1
            nodes(i) = i.ToString
        Next

        ' 候选边集合：并行生成时各线程产出局部边，循环结束后统一去重（无向边只保留一次）。
        ' 避免在 Parallel.For 内共享 HashSet 造成竞争与不确定性。
        Dim candidateEdges As New List(Of EdgeData)

        Dim buildOne = Sub(i As Integer)
            ' 计算到所有其他样本的距离
            Dim dist(n - 1) As Double
            For j As Integer = 0 To n - 1
                If j = i Then
                    dist(j) = Double.PositiveInfinity
                    Continue For
                End If
                Dim d = 0.0
                For k As Integer = 0 To ndim - 1
                    Dim diff = score(i, k) - score(j, k)
                    d += diff * diff
                Next
                dist(j) = std.Sqrt(d)
            Next

            ' 取前 k 近邻
            Dim order = Enumerable.Range(0, n).OrderBy(Function(j) dist(j)).Take(opts.knnK).ToArray
            Dim local As New List(Of EdgeData)
            For Each j In order
                Dim a = std.Min(i, j)
                Dim b = std.Max(i, j)
                local.Add(New EdgeData With {
                    .u = a,
                    .v = b,
                    .weight = 1.0 / (1.0 + dist(j))
                })
            Next
            SyncLock candidateEdges
                candidateEdges.AddRange(local)
            End SyncLock
        End Sub

        If opts.parallelEnabled Then
            Parallel.For(0, n, buildOne)
        Else
            For i As Integer = 0 To n - 1
                buildOne(i)
            Next
        End If

        ' 统一去重：按 min-max 键保留首次出现的边
        Dim seen As New HashSet(Of String)
        Dim edges As New List(Of EdgeData)
        For Each e In candidateEdges
            Dim id = $"{std.Min(e.u, e.v)}-{std.Max(e.u, e.v)}"
            If seen.Add(id) Then
                edges.Add(e)
            End If
        Next

        Dim graph = GraphData.Build(nodes, edges.ToArray)
        Call cache.SaveGraph(key, graph)
        Call Console.WriteLine($"[knn] done: {edges.Count} edges -> cached {cache.Path(key)}")

        Return graph
    End Function

    ''' <summary>
    ''' 将轻量图转换为 Louvain 所需的 <see cref="NetworkGraph(Of Node, Edge(Of Node))"/>。
    ''' </summary>
    ''' 
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function ToNetworkGraph(g As GraphData) As NetworkGraph(Of Node, Edge(Of Node))
        Return g.ToNetworkGraph()
    End Function
End Class

