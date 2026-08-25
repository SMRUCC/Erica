
''' <summary>
''' PseudoVelo 的计算结果。
''' </summary>
Public Class PseudoVelocityResult
    ''' <summary>伪速度矩阵，约定为 基因 × 细胞（行=基因，列=样本，与 result.pseudotime 顺序一致）。</summary>
    Public Property velocity As Double(,)
    ''' <summary>各细胞在 UMAP2D 空间的速度向量（样本×2）；若关闭 UMAP 投影则为 Nothing。</summary>
    Public Property velocityUMAP As Double(,)
    ''' <summary>基因名（与 velocity 行序一致）。</summary>
    Public Property geneNames As String()
    ''' <summary>样本名（与 velocity 列序、velocityUMAP 行序一致）。</summary>
    Public Property sampleNames As String()
    ''' <summary>按伪时间升序排好的样本下标（原始顺序 → 排序后位置）。</summary>
    Public Property orderIndex As Integer()
    ''' <summary>实际使用的平滑窗口半宽。</summary>
    Public Property window As Integer
    ''' <summary>是否计算了 UMAP 投影。</summary>
    Public Property useProjection As Boolean

    Public Overrides Function ToString() As String
        Dim nGenes = If(velocity Is Nothing, 0, velocity.GetLength(0))
        Dim nCells = If(velocity Is Nothing, 0, velocity.GetLength(1))

        Return $"PseudoVelocityResult(genes={nGenes}, cells={nCells}, umapProjection={useProjection})"
    End Function
End Class