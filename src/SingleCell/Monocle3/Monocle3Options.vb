
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

    ' ===== PseudoVelo（伪 RNA 速率）配置 =====
    ''' <summary>是否启用 PseudoVelo 伪速度计算。默认 True。</summary>
    Public Property pseudoVeloEnabled As Boolean = True
    ''' <summary>平滑窗口半宽（实际窗宽 = 2*window+1），默认 2（窗宽 5）。</summary>
    Public Property pseudoVeloWindow As Integer = 2
    ''' <summary>预留：若改用 LOESS 平滑时的 span 参数（0~1）。默认 0.3。</summary>
    Public Property pseudoVeloSpan As Double = 0.3
    ''' <summary>是否把细胞伪速度投影到 UMAP2D 坐标（生成 velocityUMAP）。默认 True。</summary>
    Public Property useVelocityProjection As Boolean = True
End Class