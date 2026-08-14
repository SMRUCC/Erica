Imports Erica.Analysis.SpatialTissue.Imaging.SpatialOmics.Math
Imports std = System.Math

' ============================================================================
' SpatialStats.vb — 空间自相关统计量
' ----------------------------------------------------------------------------
' 实现 spatialGE 的核心空间统计量（Ospina et al., Bioinformatics 2022）：
'
'   1. Moran's I — 全局空间自相关
'      I = (N / S₀) · Σᵢ Σⱼ wᵢⱼ(xᵢ - x̄)(xⱼ - x̄) / Σᵢ(xᵢ - x̄)²
'      期望 E[I] = -1/(N-1)，I > E[I] 表示正自相关（聚集）
'
'   2. Geary's C — 局部空间自相关
'      C = ((N-1) / (2·S₀)) · Σᵢ Σⱼ wᵢⱼ(xᵢ - xⱼ)² / Σᵢ(xᵢ - x̄)²
'      C ∈ [0,2]，C<1 正自相关，C>1 负自相关
'
'   3. Getis-Ord Gi* — 热点分析
'      Gi* = (Σⱼ wᵢⱼ xⱼ - x̄ Σⱼ wᵢⱼ) /
'             (S · √((N·Σⱼ wᵢⱼ² - (Σⱼ wᵢⱼ)²) / (N-1)))
'      Gi* > 0 热点（高值聚集），Gi* < 0 冷点（低值聚集）
'
' 参考：
'   Moran (1950), Geary (1954), Getis & Ord (1992)
'   Ospina et al. (2022) Bioinformatics 38(9):2645-2647
' ============================================================================

Namespace SpatialOmics.SpatialGE

    ''' <summary>空间自相关统计结果</summary>
    Public Class SpatialStatsResult

        ''' <summary>基因名</summary>
        Public Property GeneName As String

        ''' <summary>Moran's I 统计量</summary>
        Public Property MoransI As Double

        ''' <summary>Moran's I 期望值 E[I] = -1/(N-1)</summary>
        Public Property MoransIExpected As Double

        ''' <summary>Moran's I 标准化 z-score</summary>
        Public Property MoransIZScore As Double

        ''' <summary>Moran's I p 值（双侧）</summary>
        Public Property MoransIPValue As Double

        ''' <summary>Geary's C 统计量</summary>
        Public Property GearysC As Double

        ''' <summary>Geary's C 期望值 E[C] = 1</summary>
        Public Property GearysCExpected As Double

        ''' <summary>Geary's C 标准化 z-score</summary>
        Public Property GearysCZScore As Double

        ''' <summary>Geary's C p 值（双侧）</summary>
        Public Property GearysCPValue As Double

        ''' <summary>Getis-Ord Gi* 统计量数组（每个位点一个值）</summary>
        Public Property GetisOrdGiStar As Double()

        ''' <summary>Gi* z-score 数组</summary>
        Public Property GetisOrdGiZScore As Double()

        ''' <summary>Gi* p 值数组</summary>
        Public Property GetisOrdGiPValue As Double()

        Public Overrides Function ToString() As String
            Return $"Moran's I = {MoransI:F4} (z={MoransIZScore:F4}, p={MoransIPValue:E4}); " &
                   $"Geary's C = {GearysC:F4} (z={GearysCZScore:F4}, p={GearysCPValue:E4}); " &
                   $"Gi* spots = {GetisOrdGiStar.Length}"
        End Function

    End Class

    ''' <summary>
    ''' 空间自相关统计量计算器
    ''' </summary>
    Public Class SpatialStatistics

        Private _coords As Matrix
        Private _n As Integer

        ''' <summary>构造：传入空间坐标</summary>
        Public Sub New(coordinates As Matrix)
            _coords = coordinates
            _n = coordinates.Rows
        End Sub

        ''' <summary>
        ''' 构建 K 近邻空间权重矩阵
        ''' </summary>
        ''' <param name="kNeighbors">近邻数（默认 4）</param>
        ''' <returns>N×N 0/1 行标准化的权重矩阵 W</returns>
        Public Function BuildKNNWeights(Optional kNeighbors As Integer = 4) As Matrix
            Dim W As New Matrix(_n, _n)

            ' 计算所有点对距离
            For I As Integer = 0 To _n - 1
                ' 找到 i 的 K 个最近邻
                Dim dists(_n - 1) As (idx As Integer, dist As Double)
                For j = 0 To _n - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _coords.Cols - 1
                        Dim diff = _coords(I, d) - _coords(j, d)
                        d2 += diff * diff
                    Next
                    dists(j) = (j, std.Sqrt(d2))
                Next

                ' 按距离排序，取前 K 个（排除自身）
                Dim offset As Integer = I
                Dim neighbors = dists.Where(Function(x) x.idx <> offset).
                                      OrderBy(Function(x) x.dist).
                                      Take(kNeighbors).ToArray()

                ' 行标准化权重
                Dim wi As Double = 1.0 / neighbors.Length
                For Each nb In neighbors
                    W(I, nb.idx) = wi
                Next
            Next

            Return W
        End Function

        ''' <summary>
        ''' 构建距离阈值权重矩阵（距离小于 threshold 的为邻居）
        ''' </summary>
        Public Function BuildDistanceWeights(distanceThreshold As Double) As Matrix
            Dim W As New Matrix(_n, _n)
            For I As Integer = 0 To _n - 1
                Dim neighbors As New List(Of Integer)
                For j = 0 To _n - 1
                    If I = j Then Continue For
                    Dim d2 As Double = 0.0
                    For d = 0 To _coords.Cols - 1
                        Dim diff = _coords(I, d) - _coords(j, d)
                        d2 += diff * diff
                    Next
                    If std.Sqrt(d2) <= distanceThreshold Then
                        neighbors.Add(j)
                    End If
                Next
                ' 行标准化
                If neighbors.Count > 0 Then
                    Dim wi As Double = 1.0 / neighbors.Count
                    For Each nb In neighbors
                        W(I, nb) = wi
                    Next
                End If
            Next
            Return W
        End Function

        ''' <summary>
        ''' 计算空间自相关统计量（Moran's I, Geary's C, Getis-Ord Gi*）
        ''' </summary>
        ''' <param name="values">长度为 N 的数值向量（基因表达）</param>
        ''' <param name="weights">N×N 行标准化权重矩阵</param>
        Public Function ComputeAll(values As Double(), weights As Matrix) As SpatialStatsResult

            If values.Length <> _n Then
                Throw New ArgumentException($"Values length ({values.Length}) must match sample count ({_n}).")
            End If

            Dim result As New SpatialStatsResult()

            ' ---- Moran's I ----
            Dim moranI = ComputeMoransI(values, weights)
            result.MoransI = moranI.statistic
            result.MoransIExpected = moranI.expectedValue
            result.MoransIZScore = moranI.zScore
            result.MoransIPValue = moranI.pValue

            ' ---- Geary's C ----
            Dim gearyC = ComputeGearysC(values, weights)
            result.GearysC = gearyC.statistic
            result.GearysCExpected = gearyC.expectedValue
            result.GearysCZScore = gearyC.zScore
            result.GearysCPValue = gearyC.pValue

            ' ---- Getis-Ord Gi* ----
            Dim gi = ComputeGetisOrdGiStar(values, weights)
            result.GetisOrdGiStar = gi.statistics
            result.GetisOrdGiZScore = gi.zScores
            result.GetisOrdGiPValue = gi.pValues

            Return result
        End Function

        ''' <summary>
        ''' Moran's I 计算（含 z-score 和 p 值）
        ''' I = (N / S₀) · Σᵢ Σⱼ wᵢⱼ(xᵢ-x̄)(xⱼ-x̄) / Σᵢ(xᵢ-x̄)²
        ''' S₀ = Σᵢ Σⱼ wᵢⱼ
        ''' E[I] = -1/(N-1)
        ''' Var[I] under randomization → z = (I - E[I]) / √Var[I]
        ''' </summary>
        Private Function ComputeMoransI(values As Double(), W As Matrix) _
            As (statistic As Double, expectedValue As Double,
                zScore As Double, pValue As Double)

            Dim n = values.Length
            Dim xBar = Statistics.Mean(values)
            Dim centered = values.Select(Function(v) v - xBar).ToArray()
            Dim ss As Double = 0.0 ' Σ(xᵢ-x̄)²
            For Each c In centered
                ss += c * c
            Next
            If ss < 1.0E-20 Then
                Return (0, -1.0 / (n - 1), 0, 1.0)
            End If

            ' S₀ = Σᵢ Σⱼ wᵢⱼ
            Dim S0 As Double = 0.0
            For I As Integer = 0 To n - 1
                For j = 0 To n - 1
                    S0 += W(I, j)
                Next
            Next

            ' Σᵢ Σⱼ wᵢⱼ(xᵢ-x̄)(xⱼ-x̄)
            Dim numerator As Double = 0.0
            For I As Integer = 0 To n - 1
                For j = 0 To n - 1
                    numerator += W(I, j) * centered(I) * centered(j)
                Next
            Next

            Dim dI = (n / S0) * numerator / ss
            Dim expected = -1.0 / (n - 1)

            ' 方差（随机化假设下的近似）
            Dim S1 As Double = 0.0
            For I As Integer = 0 To n - 1
                Dim rowSum As Double = 0.0
                Dim colSum As Double = 0.0
                For j = 0 To n - 1
                    rowSum += W(I, j)
                    colSum += W(j, I)
                Next
                S1 += (rowSum + colSum) ^ 2
            Next
            S1 /= 2.0

            Dim S2 As Double = 0.0
            For I As Integer = 0 To n - 1
                Dim rowSum As Double = 0.0
                Dim colSum As Double = 0.0
                For j = 0 To n - 1
                    rowSum += W(I, j)
                    colSum += W(j, I)
                Next
                S2 += (rowSum - colSum) ^ 2
            Next

            Dim n2 = n * n
            Dim varI = (n2 * n - 3 * n2 + 3 * n - n * S1 + 3 * S0 * S0 - n * S2) /
                       ((n - 1) * (n - 2) * (n - 3) * S0 * S0) -
                       expected * expected

            If varI < 0 Then varI = std.Abs(varI)
            Dim z = If(varI > 0, (dI - expected) / std.Sqrt(varI), 0.0)
            Dim p = 2.0 * (1.0 - NormalCDF(std.Abs(z)))

            Return (dI, expected, z, p)
        End Function

        ''' <summary>
        ''' Geary's C 计算
        ''' C = ((N-1) / (2·S₀)) · Σᵢ Σⱼ wᵢⱼ(xᵢ-xⱼ)² / Σᵢ(xᵢ-x̄)²
        ''' E[C] = 1
        ''' </summary>
        Private Function ComputeGearysC(values As Double(), W As Matrix) _
            As (statistic As Double, expectedValue As Double,
                zScore As Double, pValue As Double)

            Dim n = values.Length
            Dim xBar = Statistics.Mean(values)
            Dim ss As Double = 0.0
            For I As Integer = 0 To n - 1
                ss += (values(I) - xBar) ^ 2
            Next
            If ss < 1.0E-20 Then
                Return (1.0, 1.0, 0.0, 1.0)
            End If

            Dim S0 As Double = 0.0
            Dim numerator As Double = 0.0
            For I As Integer = 0 To n - 1
                For j = 0 To n - 1
                    S0 += W(I, j)
                    numerator += W(I, j) * (values(I) - values(j)) ^ 2
                Next
            Next

            Dim C = ((n - 1) / (2.0 * S0)) * numerator / ss
            Dim expected = 1.0

            ' 近似方差
            Dim varC = (2.0 * S0 * S0 + n * (n - 1) * S0 - n * S1) /
                       (S0 * S0 * (n - 1) * (n - 2)) - 1.0
            If varC < 1.0E-20 Then varC = 1.0
            Dim z = (C - expected) / std.Sqrt(varC)
            Dim p = 2.0 * (1.0 - NormalCDF(std.Abs(z)))

            Return (C, expected, z, p)
        End Function

        ''' <summary>
        ''' Getis-Ord Gi* 计算
        ''' Gi* = (Σⱼ wᵢⱼ xⱼ - x̄ Σⱼ wᵢⱼ) /
        '''       (S · √((N·Σⱼ wᵢⱼ² - (Σⱼ wᵢⱼ)²) / (N-1)))
        ''' 其中 S = √(Σ(xⱼ - x̄)² / N)
        ''' 注意：Gi* 包含自身（wᵢᵢ ≠ 0）
        ''' </summary>
        Private Function ComputeGetisOrdGiStar(values As Double(), W As Matrix) _
            As (statistics As Double(), zScores As Double(), pValues As Double())

            Dim n = values.Length
            Dim xBar = Statistics.Mean(values)
            Dim S As Double = 0.0
            For I As Integer = 0 To n - 1
                S += (values(I) - xBar) ^ 2
            Next
            S = std.Sqrt(S / n)

            Dim gi(n - 1) As Double
            Dim zi(n - 1) As Double
            Dim pi(n - 1) As Double

            For I As Integer = 0 To n - 1
                ' Σⱼ wᵢⱼ xⱼ
                Dim sumWX As Double = 0.0
                ' Σⱼ wᵢⱼ
                Dim sumW As Double = 0.0
                ' Σⱼ wᵢⱼ²
                Dim sumW2 As Double = 0.0

                For j = 0 To n - 1
                    sumWX += W(I, j) * values(j)
                    sumW += W(I, j)
                    sumW2 += W(I, j) * W(I, j)
                Next

                ' 分子
                Dim numer = sumWX - xBar * sumW
                ' 分母
                Dim denom = S * std.Sqrt((n * sumW2 - sumW * sumW) / (n - 1))

                If std.Abs(denom) < 1.0E-20 Then
                    zi(I) = 0.0
                Else
                    zi(I) = numer / denom
                End If

                pi(I) = 2.0 * (1.0 - NormalCDF(std.Abs(zi(I))))
            Next

            Return (gi, zi, pi)
        End Function

        ' ---- 正态分布 CDF（近似） ----
        Private Function NormalCDF(z As Double) As Double
            ' Abramowitz & Stegun 近似
            Dim sign = If(z >= 0, 1, -1)
            z = std.Abs(z) / std.Sqrt(2.0)
            Dim t = 1.0 / (1.0 + 0.3275911 * z)
            Dim poly = t * (0.254829592 +
                     t * (-0.284496736 +
                     t * (1.421413741 +
                     t * (-1.453152027 +
                     t * 1.061405429))))
            Dim erf = 1.0 - poly * std.Exp(-z * z)
            Return 0.5 * (1.0 + sign * erf)
        End Function

    End Class

End Namespace
