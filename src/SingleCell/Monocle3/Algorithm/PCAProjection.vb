Imports Microsoft.VisualBasic.Math.Statistics.Hypothesis.ANOVA

''' <summary>
''' 封装 PCA 降维至 numPCA 个主成分。
''' 输入：行=样本、列=基因的 Double(,) 矩阵。
''' 输出：样本级 score 矩阵（行=样本，列=主成分），缓存为 02_pca50.csv。
''' </summary>
Public Class PCAProjection

    Public Shared Function Project(data As Double(,), opts As Monocle3Options, cache As CacheStore) As Double(,)
        Dim key = "02_pca50.csv"

        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit(key) Then
            Call Console.WriteLine($"[cache] load PCA score from {cache.Path(key)}")
            Return cache.LoadMatrix(key)
        End If

        Dim n = data.GetLength(0)
        Dim y(n - 1) As Double
        ' y 为占位标签向量，长度需等于样本数

        Call Console.WriteLine($"[pca] computing {opts.numPCA} principal components on {n} samples x {data.GetLength(1)} genes ...")

        ' PCA 接受 StatisticsObject：需将 [样本 × 基因] 的 Double(,) 转为 Double()()（每行一个样本）
        Dim rows = MatrixExtensions.ToRowVectors(data)
        Dim stat = New StatisticsObject(rows, y)
        Dim result = PCA.PrincipalComponentAnalysis(stat, opts.numPCA)
        Dim scores = result.TPreds
        ' scores(k) 为第 k 个主成分的得分向量，长度 = 样本数 n（即 [PC × 样本] 组织）
        Dim scoreMatrix(n - 1, opts.numPCA - 1) As Double

        For i As Integer = 0 To n - 1
            For j As Integer = 0 To opts.numPCA - 1
                scoreMatrix(i, j) = scores(j)(i)
            Next
        Next

        Call cache.SaveMatrix(key, scoreMatrix)
        Call Console.WriteLine($"[pca] done -> cached {cache.Path(key)}")

        Return scoreMatrix
    End Function
End Class

