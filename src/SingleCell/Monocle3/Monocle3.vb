Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

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
        Dim result = RunCore(expr.sampleByGene, expr.geneNames, expr.sampleNames, opts, cache)
        result.featureIds = matrix.expression.Keys
        result.sampleNames = matrix.sampleID.ToArray
        Return result
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

        ' 步骤 9：PseudoVelo 伪 RNA 速率（基于伪时间对表达曲线求导）
        If opts.pseudoVeloEnabled Then
            result.pseudoVelocity = PseudoVelo.Compute(result, sampleByGene, geneNames, sampleNames, opts, cache)
            Call Console.WriteLine($"=== PseudoVelo done: {result.pseudoVelocity} ===")
        End If

        Call Console.WriteLine($"=== Monocle3 pipeline done (global Moran I={result.moranGlobal:0.000}) ===")
        Return result
    End Function
End Class

