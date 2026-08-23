Imports Microsoft.VisualBasic.Data.GraphTheory
Imports Microsoft.VisualBasic.Data.GraphTheory.Analysis.Louvain
Imports Microsoft.VisualBasic.Data.GraphTheory.Network
Imports stdNum = System.Math

Namespace SMRUCC.genomics.SingleCell.Monocle3

    ''' <summary>
    ''' 基于 KNN 图进行 Louvain / Leiden 社区划分，得到每个样本的团簇标签。
    ''' 结果缓存为 05_clusters.json（含样本名与 cluster id 映射）。
    ''' </summary>
    Public Class Clustering

        Public Shared Function Cluster(knn As GraphData, sampleNames As String(), opts As Monocle3Options, cache As CacheStore) As Integer()
            Dim key = "05_clusters.json"

            If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
                Call Console.WriteLine($"[cache] load clusters from {cache.Path(key)}")
                Dim cached = cache.LoadJson(Of ClusterResult)(key)
                Return cached.labels
            End If

            Call Console.WriteLine($"[cluster] running {(If(opts.useLeiden, "Leiden", "Louvain"))} community detection (resolution={opts.resolution}) ...")

            Dim g As NetworkGraph(Of Node, VertexEdge) = knn.ToNetworkGraph()
            Dim louvain As LouvainCommunity = Builder.Load(Of Node, VertexEdge)(g, eps:=opts.resolution, leiden:=opts.useLeiden)

            Call louvain.SolveClustersParallel()
            Dim labelsStr = louvain.GetCommunity()

            ' labelsStr(i) 对应节点 i（=样本 i）的社区 id（字符串），转换为整数
            Dim n = labelsStr.Length
            Dim labels(n - 1) As Integer
            Dim remap As New Dictionary(Of String, Integer)
            Dim nextId = 0

            For i As Integer = 0 To n - 1
                Dim s = labelsStr(i)
                If Not remap.ContainsKey(s) Then
                    remap(s) = nextId
                    nextId += 1
                End If
                labels(i) = remap(s)
            Next

            Dim result = New ClusterResult With {
                .sampleNames = sampleNames,
                .labels = labels
            }
            Call cache.SaveJson(key, result)
            Call Console.WriteLine($"[cluster] done: {nextId} clusters -> cached {cache.Path(key)}")

            Return labels
        End Function
    End Class

    ''' <summary>
    ''' 分群结果缓存对象（可 JSON 序列化）。
    ''' </summary>
    Public Class ClusterResult
        Public Property sampleNames As String()
        Public Property labels As Integer()
    End Class
End Namespace
