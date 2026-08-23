Imports SMRUCC.genomics.Analysis.HTS_matrix
Imports stdNum = System.Math

Namespace SMRUCC.genomics.SingleCell.Monocle3

    ''' <summary>
    ''' Monocle3 分析管线配置项。
    ''' </summary>
    Public Class Monocle3Options
        ''' <summary>PCA 主成分数量，默认 50。</summary>
        Public Property numPCA As Integer = 50
        ''' <summary>UMAP 嵌入维度，默认 3。</summary>
        Public Property umapDim As Integer = 3
        ''' <summary>KNN 近邻数，默认 15。</summary>
        Public Property knnK As Integer = 15
        ''' <summary>Louvain 分辨率（传给 Builder.Load 的 eps），默认 1.0。</summary>
        Public Property resolution As Double = 1.0
        ''' <summary>是否使用 Leiden 算法（否则 Louvain）。</summary>
        Public Property useLeiden As Boolean = False
        ''' <summary>指定的根 cluster（用于伪时间计算）；为 Nothing 时自动选择最外围 cluster。</summary>
        Public Property rootCluster As Integer? = Nothing

        ' ===== 缓存控制 =====
        ''' <summary>缓存目录，默认 ./monocle3_cache。</summary>
        Public Property cacheDir As String = "./monocle3_cache"
        ''' <summary>是否启用缓存（命中则跳过已计算步骤）。默认 True。</summary>
        Public Property useCache As Boolean = True
        ''' <summary>是否强制覆盖缓存（重算所有步骤）。默认 False。</summary>
        Public Property overwriteCache As Boolean = False
    End Class

    ''' <summary>
    ''' Monocle3 分析结果聚合。
    ''' </summary>
    Public Class Monocle3Result
        Public Property cacheDir As String
        ''' <summary>PCA 50 维 score 矩阵（样本 × 50）。</summary>
        Public Property pcaScore As Double(,)
        ''' <summary>UMAP 3 维嵌入（样本 × 3）。</summary>
        Public Property umap3d As Double(,)
        ''' <summary>UMAP 2 维嵌入（样本 × 2）。</summary>
        Public Property umap2d As Double(,)
        ''' <summary>每个样本的 cluster 标签。</summary>
        Public Property clusters As Integer()
        ''' <summary>轨迹主图（cluster 级最小生成树）。</summary>
        Public Property clusterGraph As GraphData
        ''' <summary>每个样本的伪时间（pseudotime）。</summary>
        Public Property pseudotime As Double()
        ''' <summary>PAGA 团簇连接图。</summary>
        Public Property pagaGraph As GraphData
        ''' <summary>伪时间向量的全局 Moran's I。</summary>
        Public Property moranGlobal As Double
        ''' <summary>按 |Moran I| 排序的 top 变化基因。</summary>
        Public Property topVariableGenes As (gene As String, moranI As Double)()
    End Class

    ''' <summary>
    ''' Monocle3 轨迹推断主入口。
    ''' 
    ''' 流程（缓存感知）：
    '''   1.  转置表达矩阵为 [样本 × 基因]
    '''   2.  PCA 降维至 50 维
    '''   3.  UMAP 降维至 3 维（及 2 维用于可视化）
    '''   4.  基于 PCA 距离构建 KNN 图
    '''   5.  Louvain/Leiden 分群
    '''   6.  以团簇为节点构建 MST 主图并算伪时间
    '''   7.  PAGA 团簇连接图
    '''   8.  Moran's I 质量评估
    ''' </summary>
    Public Class Monocle3

        Public Shared Function Run(matrix As Matrix, Optional opts As Monocle3Options = Nothing) As Monocle3Result
            If opts Is Nothing Then opts = New Monocle3Options
            Dim cache = New CacheStore(opts.cacheDir)

            Call Console.WriteLine($"=== Monocle3 pipeline start (cacheDir={opts.cacheDir}, useCache={opts.useCache}) ===")

            ' 步骤 1：转置 + 低表达过滤 + log 归一化
            Dim sampleByGene = MatrixExtensions.ToSampleByGeneMatrix(matrix, minSamples:=1, logNormalize:=True)
            Dim geneNames = MatrixExtensions.KeptGeneNames(matrix, minSamples:=1)
            Dim sampleNames = matrix.sampleID

            ' 步骤 2：PCA
            Dim pcaScore = PCAProjection.Project(sampleByGene, opts, cache)

            ' 步骤 3：UMAP（3D + 2D）
            Dim umap3d = UMAPEmbedding.Embed(pcaScore, opts, cache, dim:=opts.umapDim)
            Dim umap2d = UMAPEmbedding.Embed(pcaScore, opts, cache, dim:=2)

            ' 步骤 4：KNN 图
            Dim knn = NearestNeighborGraph.BuildKNN(pcaScore, opts, cache)

            ' 步骤 5：分群
            Dim clusters = Clustering.Cluster(knn, sampleNames, opts, cache)

            ' 步骤 6：轨迹学习（MST 主图 + 伪时间）
            Dim traj = TrajectoryOrdering.Learn(umap3d, clusters, opts, cache)

            ' 步骤 7：PAGA 团簇连接图
            Dim paga = PAGAGraph.Abstract(knn, clusters, opts, cache)

            ' 步骤 8：Moran's I 质量评估
            Dim moran = SpatialAutocorrelation.Evaluate(traj.pseudotime, umap2d, sampleByGene, geneNames, opts, cache, topN:=50)

            Dim result = New Monocle3Result With {
                .cacheDir = opts.cacheDir,
                .pcaScore = pcaScore,
                .umap3d = umap3d,
                .umap2d = umap2d,
                .clusters = clusters,
                .clusterGraph = traj.mst,
                .pseudotime = traj.pseudotime,
                .pagaGraph = paga,
                .moranGlobal = moran.globalPseudotimeI,
                .topVariableGenes = moran.topVariableGenes
            }

            Call Console.WriteLine($"=== Monocle3 pipeline done (global Moran I={result.moranGlobal:0.000}) ===")
            Return result
        End Function
    End Class
End Namespace
