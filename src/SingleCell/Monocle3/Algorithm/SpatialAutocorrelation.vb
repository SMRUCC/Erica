Imports Microsoft.VisualBasic.Imaging.Math2D
Imports std = System.Math

''' <summary>
''' 基于 Moran's I 的空间自相关评估轨迹/排序质量。
''' - 全局：对 pseudotime 向量在 UMAP 空间计算全局 Moran's I（衡量伪时间的空间平滑/单调程度）。
''' - 基因级：对每个基因的表达在 UMAP 空间计算 Moran's I，取 |I| 最大的 top 变化基因。
''' 结果缓存为 09_moran.json。
''' </summary>
Public Class SpatialAutocorrelation

    Public Shared Function Evaluate(pseudotime As Double(),
                                     umap2d As Double(,),
                                     geneExpr As Double(,),
                                     geneNames As String(),
                                     opts As Monocle3Options,
                                     cache As CacheStore,
                                     Optional topN As Integer = 50) As MoranResult
        Dim key = "09_moran.json"

        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
            Call Console.WriteLine($"[cache] load Moran result from {cache.Path(key)}")
            Return cache.LoadJson(Of MoranResult)(key)
        End If

        Dim n = pseudotime.Length
        Dim c1(n - 1) As Double
        Dim c2(n - 1) As Double
        For i As Integer = 0 To n - 1
            c1(i) = umap2d(i, 0)
            c2(i) = umap2d(i, 1)
        Next

        Call Console.WriteLine($"[moran] computing global pseudotime autocorrelation ...")
        Dim globalI = Moran.calc_moran(pseudotime, c1, c2).observed

        Call Console.WriteLine($"[moran] computing gene-level autocorrelation over {geneNames.Length} genes ...")
        Dim moranOfGene(geneNames.Length - 1) As (gene As String, moranI As Double)
        For g As Integer = 0 To geneNames.Length - 1
            Dim expr(n - 1) As Double
            For i As Integer = 0 To n - 1
                expr(i) = geneExpr(i, g)
            Next
            Dim mi = Moran.calc_moran(expr, c1, c2).observed
            moranOfGene(g) = (geneNames(g), mi)
        Next

        ' 按 |Moran I| 降序取 top N 变化基因
        Dim top = moranOfGene _
            .OrderByDescending(Function(t) std.Abs(t.moranI)) _
            .Take(topN) _
            .ToArray

        Dim result = New MoranResult With {
            .globalPseudotimeI = globalI,
            .topVariableGenes = top
        }
        Call cache.SaveJson(key, result)
        Call Console.WriteLine($"[moran] done: global I={globalI:0.000} -> cached {cache.Path(key)}")

        Return result
    End Function
End Class

''' <summary>
''' Moran's I 评估结果缓存对象。
''' </summary>
Public Class MoranResult
    Public Property globalPseudotimeI As Double
    Public Property topVariableGenes As (gene As String, moranI As Double)()
End Class

