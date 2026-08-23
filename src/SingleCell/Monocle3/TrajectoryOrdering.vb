Imports Microsoft.VisualBasic.Data.GraphTheory.Network
Imports Microsoft.VisualBasic.Data.GraphTheory.MinimumSpanningTree
Imports Microsoft.VisualBasic.Data.GraphTheory.Analysis.Dijkstra
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports stdNum = System.Math

Namespace SMRUCC.genomics.SingleCell.Monocle3

    ''' <summary>
    ''' 学习轨迹顺序（ordering）：以团簇为节点构建最小生成树（Kruskal / MST）
    ''' 合并分群，得到轨迹拓扑主图；再以根为起点用 Dijkstra 最短路径距离赋予
    ''' 每个样本伪时间（pseudotime）。MST 主图缓存为 06_mst_graph.json，
    ''' 伪时间缓存为 07_pseudotime.csv。
    ''' </summary>
    Public Class TrajectoryOrdering

        ''' <summary>
        ''' cluster 质心在 UMAP 空间的坐标。
        ''' </summary>
        Public centroid As Double(,)

        ''' <summary>
        ''' cluster 数量（重新编号后的连续 id：0..k-1）。
        ''' </summary>
        Public k As Integer

        ''' <summary>
        ''' 执行轨迹学习：返回 (mstGraph, pseudotime)。
        ''' </summary>
        Public Shared Function Learn(umap3d As Double(,), clusters As Integer(), opts As Monocle3Options, cache As CacheStore) As (mst As GraphData, pseudotime As Double())
            Dim mstKey = "06_mst_graph.json"
            Dim ptKey = "07_pseudotime.csv"

            If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(mstKey) AndAlso cache.Hit(ptKey) Then
                Call Console.WriteLine($"[cache] load MST graph + pseudotime from cache")
                Dim g = cache.LoadGraph(mstKey)
                Dim pt = cache.LoadVector(ptKey)
                Return (g, pt)
            End If

            ' 1. 计算 cluster 质心（UMAP 3D 空间）
            Dim n = clusters.Length
            Dim ndim = umap3d.GetLength(1)
            Dim clusterIds = clusters.Distinct.ToArray
            Dim k = clusterIds.Length
            Dim idMap As New Dictionary(Of Integer, Integer)
            For i As Integer = 0 To k - 1
                idMap(clusterIds(i)) = i
            Next

            Dim centroid(k - 1, ndim - 1) As Double
            Dim counts(k - 1) As Integer
            For s As Integer = 0 To n - 1
                Dim ci = idMap(clusters(s))
                counts(ci) += 1
                For d As Integer = 0 To ndim - 1
                    centroid(ci, d) += umap3d(s, d)
                Next
            Next
            For ci As Integer = 0 To k - 1
                If counts(ci) > 0 Then
                    For d As Integer = 0 To ndim - 1
                        centroid(ci, d) /= counts(ci)
                    Next
                End If
            Next

            ' 2. 构建 cluster 全连接图（权重=质心欧氏距离）
            Dim allEdges As New List(Of VertexEdge)
            For a As Integer = 0 To k - 1
                For b As Integer = a + 1 To k - 1
                    Dim d = 0.0
                    For dd As Integer = 0 To ndim - 1
                        Dim diff = centroid(a, dd) - centroid(b, dd)
                        d += diff * diff
                    Next
                    d = stdNum.Sqrt(d)
                    allEdges.Add(New VertexEdge(New Vertex With {.ID = a}, New Vertex With {.ID = b}, d))
                Next
            Next

            ' 3. Kruskal 最小生成树
            Call Console.WriteLine($"[mst] computing minimum spanning tree over {k} clusters ...")
            Dim kruskal = New Kruskal(allEdges)
            Dim mstEdges = kruskal.findMinTree.ToArray

            Dim nodes(k - 1) As String
            For i As Integer = 0 To k - 1
                nodes(i) = i.ToString
            Next
            Dim edges(mstEdges.Length - 1) As EdgeData
            For i As Integer = 0 To mstEdges.Length - 1
                edges(i) = New EdgeData With {
                    .u = mstEdges(i).U.ID,
                    .v = mstEdges(i).V.ID,
                    .weight = mstEdges(i).weight
                }
            Next
            Dim mst = GraphData.Build(nodes, edges)
            Call cache.SaveGraph(mstKey, mst)
            Call Console.WriteLine($"[mst] done: {edges.Length} edges -> cached {cache.Path(mstKey)}")

            ' 4. 选根 cluster（默认度最低的外围 cluster，或由用户指定）
            Dim rootIdx = SelectRoot(mst, opts)
            Call Console.WriteLine($"[pseudotime] root cluster = {rootIdx}")

            ' 5. 构建 cluster 级邻接矩阵并运行 Dijkstra
            Dim adj As New SparseMatrix(k, k)
            For Each e In edges
                Call adj.Set(e.u, e.v, e.weight)
                Call adj.Set(e.v, e.u, e.weight)
            Next

            Dim dijkstra = New DijkstraAlgoritm(adj, k)
            Dim distNodes = dijkstra.DistanceFinder(rootIdx)
            Dim clusterPseudotime(k - 1) As Double
            For ci As Integer = 0 To k - 1
                clusterPseudotime(ci) = distNodes(ci).TotalDistance
            Next

            ' 6. 样本伪时间 = 其所属 cluster 的伪时间
            Dim pseudotime(n - 1) As Double
            For s As Integer = 0 To n - 1
                pseudotime(s) = clusterPseudotime(idMap(clusters(s)))
            Next

            Call cache.SaveVector(ptKey, pseudotime)
            Call Console.WriteLine($"[pseudotime] done -> cached {cache.Path(ptKey)}")

            Return (mst, pseudotime)
        End Function

        ''' <summary>
        ''' 选择根 cluster：优先使用用户指定；否则取 MST 中度最低（最外围）的 cluster。
        ''' </summary>
        Private Shared Function SelectRoot(mst As GraphData, opts As Monocle3Options) As Integer
            If opts.rootCluster.HasValue Then
                Return opts.rootCluster.Value
            End If

            Dim degree(mst.nodes.Length - 1) As Integer
            For Each e In mst.edges
                degree(e.u) += 1
                degree(e.v) += 1
            Next

            Dim minDeg = Integer.MaxValue
            Dim root = 0
            For i As Integer = 0 To degree.Length - 1
                If degree(i) < minDeg Then
                    minDeg = degree(i)
                    root = i
                End If
            Next
            Return root
        End Function
    End Class
End Namespace
