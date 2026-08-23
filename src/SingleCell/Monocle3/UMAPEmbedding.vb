Imports Microsoft.VisualBasic.DataMining.UMAP

''' <summary>
''' 封装 UMAP 非线性降维。
''' 输入：行=样本、列=特征的 Double(,) 矩阵（通常取 PCA 50 维结果）。
''' 输出：3 维（默认）嵌入用于轨迹学习，以及 2 维嵌入用于可视化，
''' 分别缓存为 03_umap3d.csv / 03b_umap2d.csv。
''' </summary>
Public Class UMAPEmbedding

    Public Shared Function Embed(score As Double(,), opts As Monocle3Options, cache As CacheStore, Optional [dim] As Integer = -1) As Double(,)
        If [dim] <= 0 Then [dim] = opts.umapDim
        Dim key = If([dim] = 2, "03b_umap2d.csv", "03_umap3d.csv")

        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
            Call Console.WriteLine($"[cache] load UMAP({[dim]}d) from {cache.Path(key)}")
            Return cache.LoadMatrix(key)
        End If

        Dim n = score.GetLength(0)
        Call Console.WriteLine($"[umap] computing {[dim]}d embedding on {n} samples ...")

        Dim rows As Double()() = MatrixExtensions.ToRowVectors(score)
        Dim umap As New Umap(dimensions:=[dim])
        Call umap.InitializeFit(rows)

        ' 默认迭代轮次，足以在中大规模数据上收敛
        For i As Integer = 1 To 500
            Call umap.Step(50)
        Next

        Dim embedding = umap.GetEmbedding()
        Dim out(n - 1, [dim] - 1) As Double
        For i As Integer = 0 To n - 1
            For j As Integer = 0 To [dim] - 1
                out(i, j) = embedding(i)(j)
            Next
        Next

        Call cache.SaveMatrix(key, out)
        Call Console.WriteLine($"[umap] done ({[dim]}d) -> cached {cache.Path(key)}")

        Return out
    End Function
End Class

