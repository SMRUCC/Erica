Imports Microsoft.VisualBasic.Data.GraphTheory
Imports Microsoft.VisualBasic.Data.GraphTheory.Analysis.Dijkstra
Imports Microsoft.VisualBasic.Data.GraphTheory.MinimumSpanningTree
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports std = System.Math
Imports System.Threading.Tasks

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

        ' 按 cluster 分组样本索引（保证每个 cluster 内样本顺序固定，归并结果确定）
        Dim cellsInCluster(k - 1) As List(Of Integer)
        For c As Integer = 0 To k - 1
            cellsInCluster(c) = New List(Of Integer)
        Next
        For s As Integer = 0 To n - 1
            cellsInCluster(idMap(clusters(s))).Add(s)
        Next

        ' 各 cluster 的累加相互独立（写出不同行/不同计数索引），可并行；
        ' 同一 cluster 内按 cellsInCluster(c) 顺序累加，浮点顺序固定。
        Dim accumulateCluster = Sub(c As Integer)
            Dim sum(ndim - 1) As Double
            Dim cnt = 0
            For Each s In cellsInCluster(c)
                cnt += 1
                For d As Integer = 0 To ndim - 1
                    sum(d) += umap3d(s, d)
                Next
            Next
            counts(c) = cnt
            For d As Integer = 0 To ndim - 1
                centroid(c, d) = sum(d)
            Next
        End Sub
        If opts.parallelEnabled Then
            Parallel.For(0, k, accumulateCluster)
        Else
            For c As Integer = 0 To k - 1
                accumulateCluster(c)
            Next
        End If
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
                d = std.Sqrt(d)
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
        ' 防御校验：连通的 MST 必须恰有 k-1 条边；否则图不连通（或库实现异常），
        ' 会出现大量节点到根不可达（TotalDistance = Integer.MaxValue）。
        If edges.Length <> k - 1 Then
            Call Console.WriteLine($"[warn] MST edges={edges.Length}, nodes={k} (expect {k - 1}): 图可能不连通!")
        End If

        Dim adj As New SparseMatrix(k, k)
        For Each e In edges
            ' sciBASIC# 的 SparseMatrix.Set 签名是 Set(xij, i, j)：值在前，行/列在后。
            ' 原写法 adj.Set(e.u, e.v, e.weight) 把 (cluster索引, cluster索引, 距离)
            ' 误传成了 (权重, 行, 列)，导致邻接矩阵几乎为空、Dijkstra 无路可走。
            ' 此外 Dijkstra 把权重 0 当作“无边”，故对重合质心加一个极小 epsilon。
            Dim w = std.Max(e.weight, 0.000001)
            Call adj.Set(w, e.u, e.v)
            Call adj.Set(w, e.v, e.u)
        Next

        Dim dijkstra = New DijkstraAlgoritm(adj, k)
        Dim distNodes = dijkstra.DistanceFinder(rootIdx)
        Dim clusterPseudotime(k - 1) As Double
        For ci As Integer = 0 To k - 1
            clusterPseudotime(ci) = distNodes(ci).TotalDistance
        Next

        ' 6. 不可达簇兜底：Dijkstra 对不可达节点返回 Integer.MaxValue（不是 NaN）。
        '    将每个不可达簇接回最近的可达簇（其伪时间 + 质心距离），等价于把森林补成连通树。
        For ci As Integer = 0 To k - 1
            If clusterPseudotime(ci) >= Integer.MaxValue Then
                Dim bestJ = -1
                Dim bestD = Double.MaxValue
                For j As Integer = 0 To k - 1
                    If clusterPseudotime(j) < Integer.MaxValue Then
                        Dim d = 0.0
                        For dd As Integer = 0 To ndim - 1
                            Dim diff = centroid(ci, dd) - centroid(j, dd)
                            d += diff * diff
                        Next
                        d = std.Sqrt(d)
                        If d < bestD Then
                            bestD = d
                            bestJ = j
                        End If
                    End If
                Next
                If bestJ >= 0 Then
                    clusterPseudotime(ci) = clusterPseudotime(bestJ) + bestD
                    Call Console.WriteLine($"[warn] cluster {ci} 到根不可达, 已接回最近可达簇 {bestJ}")
                Else
                    clusterPseudotime(ci) = 0.0
                    Call Console.WriteLine($"[warn] cluster {ci} 无任何可达簇, 伪时间置 0")
                End If
            End If
        Next

        ' 7. 与 Monocle 原版一致：把 cluster 伪时间缩放到 0-100
        Dim maxPt = clusterPseudotime.Max()
        If maxPt > 0 Then
            For ci As Integer = 0 To k - 1
                clusterPseudotime(ci) = clusterPseudotime(ci) / maxPt * 100.0
            Next
        End If

        ' 8. 样本伪时间 = 其所属 cluster 的伪时间
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

