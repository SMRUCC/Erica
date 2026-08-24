Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner

''' <summary>
''' Monocle3 分析管线配置项。
''' </summary>
Public Class Monocle3Options
    ''' <summary>PCA 主成分数量，默认 50。</summary>
    Public Property numPCA As Integer = 50
    ''' <summary>用于 PCA 的高变基因（highly variable genes）数量，默认 2000。
    ''' 在 PCA 前按表达方差筛选 top N 基因，避免在全基因（数万维）上做 PCA 导致极慢。</summary>
    Public Property numHVGenes As Integer = 2000
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

    ''' <summary>
    ''' 把样本级分析结果回写到 GCModeller 实验设计体系的 <see cref="SampleInfo"/> 集合中。
    ''' 每个样本对应一个 <see cref="SampleInfo"/>，其 <see cref="SampleInfo.metadata"/> 字典
    ''' 携带该样本的全部样本级结论（统一前缀 <c>mon_</c>，避免与实验设计自有字段冲突）：
    ''' <c>mon_pseudotime</c>、<c>mon_cluster</c>、<c>mon_umap3d_x/y/z</c>、
    ''' <c>mon_umap2d_x/y</c>、<c>mon_pca_1..N</c>、<c>mon_moran_global</c>。
    ''' cluster 级（clusterGraph/pagaGraph）与基因级（topVariableGenes）结果不属于样本级，不写入。
    ''' </summary>
    ''' <param name="sampleNames">样本名数组，顺序必须与 pcaScore/umap*/clusters/pseudotime 的行一致。</param>
    ''' <returns>与 sampleNames 一一对应的 SampleInfo 集合。</returns>
    Public Function ToSampleInfo(sampleNames As String()) As SampleInfo()
        Dim n = sampleNames.Length

        If pseudotime Is Nothing OrElse pseudotime.Length <> n Then
            Throw New ArgumentException($"Monocle3Result.pseudotime 长度({pseudotime?.Length})与样本数({n})不一致！")
        End If
        If clusters Is Nothing OrElse clusters.Length <> n Then
            Throw New ArgumentException($"Monocle3Result.clusters 长度({clusters?.Length})与样本数({n})不一致！")
        End If
        If umap3d Is Nothing OrElse umap3d.GetLength(0) <> n Then
            Throw New ArgumentException($"Monocle3Result.umap3d 行数({umap3d?.GetLength(0)})与样本数({n})不一致！")
        End If
        If umap2d Is Nothing OrElse umap2d.GetLength(0) <> n Then
            Throw New ArgumentException($"Monocle3Result.umap2d 行数({umap2d?.GetLength(0)})与样本数({n})不一致！")
        End If
        If pcaScore Is Nothing OrElse pcaScore.GetLength(0) <> n Then
            Throw New ArgumentException($"Monocle3Result.pcaScore 行数({pcaScore?.GetLength(0)})与样本数({n})不一致！")
        End If

        Dim nPCA = pcaScore.GetLength(1)
        Dim samples(n - 1) As SampleInfo

        For i As Integer = 0 To n - 1
            Dim sampleId = sampleNames(i)
            Dim meta As New Dictionary(Of String, String) From {
                {"mon_pseudotime", pseudotime(i).ToString("G17")},
                {"mon_cluster", clusters(i).ToString},
                {"mon_umap3d_x", umap3d(i, 0).ToString("G17")},
                {"mon_umap3d_y", umap3d(i, 1).ToString("G17")},
                {"mon_umap3d_z", umap3d(i, 2).ToString("G17")},
                {"mon_umap2d_x", umap2d(i, 0).ToString("G17")},
                {"mon_umap2d_y", umap2d(i, 1).ToString("G17")},
                {"mon_moran_global", moranGlobal.ToString("G17")}
            }

            For c As Integer = 0 To nPCA - 1
                meta($"mon_pca_{c + 1}") = pcaScore(i, c).ToString("G17")
            Next

            samples(i) = New SampleInfo With {
                .ID = sampleId,
                .sample_name = sampleId,
                .metadata = meta
            }
        Next

        Return samples
    End Function
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
        Dim expr = LoadExpression(matrix, opts, cache)
        Return RunCore(expr.sampleByGene, expr.geneNames, expr.sampleNames, opts, cache)
    End Function

    ''' <summary>
    ''' 缓存感知的"加载 + 转置 + 低表达过滤 + log 归一化 + 高变基因选择"步骤。
    ''' 命中缓存时直接读取预处理后的 [样本 × 高变基因] 矩阵，跳过 Matrix.LoadData 与全矩阵遍历。
    ''' </summary>
    Private Shared Function LoadExpression(matrix As Matrix, opts As Monocle3Options, cache As CacheStore) As (sampleByGene As Double(,), geneNames As String(), sampleNames As String())
        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit("01_expr_hv.csv") Then
            Call Console.WriteLine("[cache] hit 01_expr_hv.csv (skip load + preprocess + HVG)")
            Return (cache.LoadMatrix("01_expr_hv.csv"), cache.LoadLabels("01_genes_hv.txt"), cache.LoadLabels("01_samples.txt"))
        End If

        Dim sw = System.Diagnostics.Stopwatch.StartNew()
        Dim full = MatrixExtensions.ToSampleByGeneMatrix(matrix, minSamples:=1, logNormalize:=True)
        Dim fullGenes = MatrixExtensions.KeptGeneNames(matrix, minSamples:=1)
        Dim hv = MatrixExtensions.SelectHighlyVariableGenes(full, fullGenes, opts.numHVGenes)
        sw.Stop()
        Call Console.WriteLine($"[01] preprocess + HVG done: {hv.matrix.GetLength(0)} samples x {hv.matrix.GetLength(1)} HV genes ({sw.Elapsed.TotalSeconds:F1}s)")

        Call cache.SaveMatrix("01_expr_hv.csv", hv.matrix)
        Call cache.SaveLabels("01_genes_hv.txt", hv.names)
        Call cache.SaveLabels("01_samples.txt", matrix.sampleID)
        Return (hv.matrix, hv.names, matrix.sampleID)
    End Function

    ''' <summary>
    ''' 直接以预处理后的 [样本 × 基因] 矩阵作为输入运行管线（可绕过 Matrix.LoadData，配合外部缓存使用）。
    ''' </summary>
    Public Shared Function Run(sampleByGene As Double(,), geneNames As String(), sampleNames As String(), Optional opts As Monocle3Options = Nothing) As Monocle3Result
        If opts Is Nothing Then opts = New Monocle3Options
        Dim cache = New CacheStore(opts.cacheDir)
        Return RunCore(sampleByGene, geneNames, sampleNames, opts, cache)
    End Function

    Private Shared Function RunCore(sampleByGene As Double(,), geneNames As String(), sampleNames As String(), opts As Monocle3Options, cache As CacheStore) As Monocle3Result
        Call Console.WriteLine($"=== Monocle3 pipeline start (cacheDir={opts.cacheDir}, useCache={opts.useCache}) ===")

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

