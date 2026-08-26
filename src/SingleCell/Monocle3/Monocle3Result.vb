Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner
Imports std = System.Math

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
    Public Property topVariableGenes As VariantGene()
    Public Property featureIds As String()
    Public Property sampleNames As String()
    ''' <summary>PseudoVelo 伪 RNA 速率结果（基因×细胞 伪速度矩阵 + 可选 UMAP 速度向量）；未计算时为 Nothing。</summary>
    Public Property pseudoVelocity As PseudoVelocityResult

    ''' <summary>
    ''' 把样本级分析结果回写到 GCModeller 实验设计体系的 <see cref="SampleInfo"/> 集合中。
    ''' 每个样本对应一个 <see cref="SampleInfo"/>，其 <see cref="SampleInfo.metadata"/> 字典
    ''' 携带该样本的全部样本级结论（统一前缀 <c>mon_</c>，避免与实验设计自有字段冲突）：
    ''' <c>mon_pseudotime</c>、<c>mon_cluster</c>、<c>mon_umap3d_x/y/z</c>、
    ''' <c>mon_umap2d_x/y</c>、<c>mon_pca_1..N</c>、<c>mon_moran_global</c>。
    ''' cluster 级（clusterGraph/pagaGraph）与基因级（topVariableGenes）结果不属于样本级，不写入。
    ''' </summary>
    ''' <returns>与 sampleNames 一一对应的 SampleInfo 集合。</returns>
    Public Function ToSampleInfo() As SampleInfo()
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

        nPCA = std.Min(3, nPCA)

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