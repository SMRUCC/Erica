' ============================================================================
' AEH.vb — Automatic Expression Histology（自动表达组织学）
' ----------------------------------------------------------------------------
' SpatialDE 的扩展模块：基于 GP 后验的空间模式聚类。
' 将空间变异基因按表达空间模式归入 K 个模式组（histological patterns），
' 使用变分推断估计后验概率。
'
' 算法概述（Svensson et al., 2018, Online Methods "AEH" 部分）：
'   1. 筛选显著空间变异基因（q < threshold）
'   2. 对每个基因计算 GP 后验均值（空间平滑后的表达值）
'   3. 对后验均值做 K-means 聚类，得到 K 个空间模式
'   4. 用变分 EM 迭代优化聚类中心和分配概率
' ============================================================================

Imports SpatialOmics.Math
Imports System
Imports System.Linq

Namespace SpatialOmics.SpatialDE

    ''' <summary>AEH 聚类结果</summary>
    Public Class AEHResult

        ''' <summary>每个基因的模式分配（0~K-1）</summary>
        Public Property Assignments As Integer()

        ''' <summary>每个基因属于各模式的后验概率矩阵 (G×K)</summary>
        Public Property Posterior As Double(,)

        ''' <summary>各模式中心（K×N 空间表达模式）</summary>
        Public Property PatternCenters As Double(,) ' K×N

        ''' <summary>模式数 K</summary>
        Public Property K As Integer

    End Class

    ''' <summary>
    ''' Automatic Expression Histology：空间模式聚类
    ''' </summary>
    Public Class AutomaticExpressionHistology

        Private _coords As Matrix
        Private _n As Integer

        ''' <summary>构造 AEH 分析器</summary>
        Public Sub New(coordinates As Matrix)
            _coords = coordinates
            _n = coordinates.Rows
        End Sub

        ''' <summary>
        ''' 对筛选后的基因执行 AEH 聚类
        ''' </summary>
        ''' <param name="expression">G×N 筛选后基因表达矩阵</param>
        ''' <param name="geneNames">基因名</param>
        ''' <param name="kPatterns">模式数 K</param>
        ''' <param name="maxIter">最大 EM 迭代次数</param>
        ''' <param name="tol">收敛容差</param>
        ''' <param name="seed">随机种子</param>
        Public Function Run(expression As Matrix, geneNames As String(),
                            kPatterns As Integer,
                            Optional maxIter As Integer = 100,
                            Optional tol As Double = 0.0001,
                            Optional seed As Integer = 42) As AEHResult

            Dim nGenes = expression.Rows
            Dim n = expression.Cols
            If n <> _n Then
                Throw New ArgumentException("Expression columns must match coordinate count.")
            End If

            ' 1. 对每个基因做空间平滑（GP 后验均值近似）
            Dim smoothed(nGenes - 1, n - 1) As Double
            Dim lengthScale = EstimateMedianLengthScale()
            Dim K = CovarianceKernels.SquaredExponential(_coords, lengthScale)
            ' 加小噪声保证正定
            Dim C = K.AddScalar(0.01)

            For g = 0 To nGenes - 1
                Dim y = expression.GetRow(g)
                Dim yMean = Statistics.Mean(y)
                Dim yCentered = y.Select(Function(v) v - yMean).ToArray()
                ' GP 后验均值 ≈ K · (K+σ²I)⁻¹ · y
                ' 即 C⁻¹·y 后乘 K
                Dim CinvY = C.SolveCholesky(yCentered)
                For i = 0 To n - 1
                    Dim smoothVal As Double = 0.0
                    For j = 0 To n - 1
                        smoothVal += K(i, j) * CinvY(j)
                    Next
                    smoothed(g, i) = smoothVal + yMean
                Next
            Next

            ' 2. K-means++ 初始化
            Dim rng As New Random(seed)
            Dim centers(kPatterns - 1, n - 1) As Double
            ' 选第一个中心：随机选一个基因
            Dim firstIdx = rng.Next(nGenes)
            For j = 0 To n - 1
                centers(0, j) = smoothed(firstIdx, j)
            Next

            For k = 1 To kPatterns - 1
                ' 计算各点到已有中心的最小距离
                Dim dists(nGenes - 1) As Double
                Dim distSum As Double = 0.0
                For g = 0 To nGenes - 1
                    Dim minDist As Double = Double.MaxValue
                    For kk = 0 To k - 1
                        Dim d2 As Double = 0.0
                        For j = 0 To n - 1
                            d2 += (smoothed(g, j) - centers(kk, j)) ^ 2
                        Next
                        If d2 < minDist Then minDist = d2
                    Next
                    dists(g) = minDist
                    distSum += minDist
                Next
                ' 按距离平方概率选下一个中心
                Dim r = rng.NextDouble() * distSum
                Dim cumSum As Double = 0.0
                Dim nextIdx = 0
                For g = 0 To nGenes - 1
                    cumSum += dists(g)
                    If cumSum >= r Then
                        nextIdx = g
                        Exit For
                    End If
                Next
                For j = 0 To n - 1
                    centers(k, j) = smoothed(nextIdx, j)
                Next
            Next

            ' 3. 变分 EM 迭代
            Dim assignments(nGenes - 1) As Integer
            Dim posterior(nGenes - 1, kPatterns - 1) As Double
            Dim prevLL As Double = Double.MinValue

            For iter = 1 To maxIter
                ' E 步：计算各基因属于各模式的概率（softmax on -d²/2σ²）
                For g = 0 To nGenes - 1
                    Dim logProb(kPatterns - 1) As Double
                    For k = 0 To kPatterns - 1
                        Dim d2 As Double = 0.0
                        For j = 0 To n - 1
                            Dim diff = smoothed(g, j) - centers(k, j)
                            d2 += diff * diff
                        Next
                        logProb(k) = -0.5 * d2 / (1.0 + 0.01) ' σ²=1 + jitter
                    Next
                    ' softmax
                    Dim maxLP = logProb.Max()
                    Dim sumExp As Double = 0.0
                    For k = 0 To kPatterns - 1
                        posterior(g, k) = Math.Exp(logProb(k) - maxLP)
                        sumExp += posterior(g, k)
                    Next
                    For k = 0 To kPatterns - 1
                        posterior(g, k) /= sumExp
                    Next
                    ' 硬分配
                    Dim maxP = posterior(g, 0)
                    Dim maxK = 0
                    For k = 1 To kPatterns - 1
                        If posterior(g, k) > maxP Then
                            maxP = posterior(g, k)
                            maxK = k
                        End If
                    Next
                    assignments(g) = maxK
                Next

                ' M 步：更新中心
                For k = 0 To kPatterns - 1
                    Dim count As Double = 0.0
                    Dim sum(n - 1) As Double
                    For g = 0 To nGenes - 1
                        Dim p = posterior(g, k)
                        count += p
                        For j = 0 To n - 1
                            sum(j) += p * smoothed(g, j)
                        Next
                    Next
                    If count > 0 Then
                        For j = 0 To n - 1
                            centers(k, j) = sum(j) / count
                        Next
                    End If
                Next

                ' 计算对数似然
                Dim ll As Double = 0.0
                For g = 0 To nGenes - 1
                    Dim maxLP = Double.MinValue
                    For k = 0 To kPatterns - 1
                        If posterior(g, k) > 0 Then
                            Dim lp = Math.Log(posterior(g, k))
                            If lp > maxLP Then maxLP = lp
                        End If
                    Next
                    If maxLP > Double.MinValue Then ll += maxLP
                Next

                If Math.Abs(ll - prevLL) < tol Then Exit For
                prevLL = ll
            Next

            Return New AEHResult With {
                .Assignments = assignments,
                .Posterior = posterior,
                .PatternCenters = centers,
                .K = kPatterns
            }
        End Function

        ''' <summary>估计中位数长度尺度（用于 GP 平滑）</summary>
        Private Function EstimateMedianLengthScale() As Double
            Dim distances As New List(Of Double)
            For i = 0 To _n - 2
                For j = i + 1 To _n - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _coords.Cols - 1
                        Dim diff = _coords(i, d) - _coords(j, d)
                        d2 += diff * diff
                    Next
                    distances.Add(Math.Sqrt(d2))
                Next
            Next
            distances.Sort()
            Return distances(distances.Count \ 2)
        End Function

    End Class

End Namespace
