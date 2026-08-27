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

        Dim edges As New List(Of EdgeData)
        ' 邻接集合，用于去重（无向边只保留一次）
        Dim seen As New HashSet(Of String)

        For i As Integer = 0 To n - 1
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
            For Each j In order
                Dim a = std.Min(i, j)
                Dim b = std.Max(i, j)
                Dim id = $"{a}-{b}"
                If seen.Add(id) Then
                    edges.Add(New EdgeData With {
                        .u = a,
                        .v = b,
                        .weight = 1.0 / (1.0 + dist(j))
                    })
                End If
            Next
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

