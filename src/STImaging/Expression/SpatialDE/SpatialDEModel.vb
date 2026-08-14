' ============================================================================
' SpatialDEModel.vb — SpatialDE 核心算法
' ----------------------------------------------------------------------------
' 基于 Gaussian Process 回归的空间变异基因鉴定（Svensson et al., 2018）。
'
' 算法概述：
'   1. 对每个基因，建立 GP 模型：y ~ N(μ·1, σ_s²·(Σ + δ·I))
'      其中 Σ 为空间协方差核矩阵，δ 为非空间噪声比，FSV = 1/(1+δ)
'   2. 优化超参数 (μ, σ_s², δ, l) 使边际似然最大化
'   3. 零假设模型：y ~ N(μ·1, σ²·I)（无空间分量）
'   4. 似然比检验 → χ²(df=1) → p 值 → BH-FDR 校正
'   5. BIC 模型选择：比较 SE / Linear / Periodic 核
'
' 关键公式：
'   LL = -N/2·log(2π) - 1/2·log|σ_s²·(Σ+δI)|
'        - 1/2·(y-μ1)ᵀ(σ_s²(Σ+δI))⁻¹(y-μ1)     (Eq. 3)
' ============================================================================

Imports SpatialOmics.Math
Imports System
Imports System.Linq

Namespace SpatialOmics.SpatialDE

    ''' <summary>
    ''' 单个基因的 SpatialDE 分析结果
    ''' </summary>
    Public Class SpatialDEResult

        ''' <summary>基因名</summary>
        Public Property GeneName As String

        ''' <summary>P 值（似然比检验，χ² df=1）</summary>
        Public Property PValue As Double

        ''' <summary>Q 值（BH-FDR 校正后）</summary>
        Public Property QValue As Double

        ''' <summary>长度尺度参数 l（优化值）</summary>
        Public Property LengthScale As Double

        ''' <summary>非空间噪声比 δ</summary>
        Public Property Delta As Double

        ''' <summary>空间方差比例 FSV = 1/(1+δ)</summary>
        Public ReadOnly Property FSV As Double
            Get
                Return 1.0 / (1.0 + Delta)
            End Get
        End Property

        ''' <summary>空间均值 μ</summary>
        Public Property Mu As Double

        ''' <summary>空间方差 σ_s²</summary>
        Public Property SigmaSq As Double

        ''' <summary>全模型对数边际似然</summary>
        Public Property LogLikFull As Double

        ''' <summary>零假设模型对数似然</summary>
        Public Property LogLikNull As Double

        ''' <summary>BIC 值（最优核）</summary>
        Public Property BIC As Double

        ''' <summary>最优核类型</summary>
        Public Property BestKernel As KernelType

        ''' <summary>似然比统计量</summary>
        Public ReadOnly Property LRStat As Double
            Get
                Return 2.0 * (LogLikFull - LogLikNull)
            End Get
        End Property

        ''' <summary>是否通过显著性检验（q < 0.05）</summary>
        Public ReadOnly Property IsSignificant As Boolean
            Get
                Return QValue < 0.05
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{GeneName,-20} l={LengthScale:F4} δ={Delta:F4} FSV={FSV:F4} " &
                   $"p={PValue:E4} q={QValue:E4} kernel={BestKernel}"
        End Function

    End Class

    ''' <summary>
    ''' SpatialDE 分析器：鉴定空间变异基因
    ''' </summary>
    Public Class SpatialDEModel

        Private _coords As Matrix      ' N×D 空间坐标
        Private _n As Integer         ' 样本数（坐标数）
        Private _maxDist As Double    ' 坐标最大距离
        Private _minDist As Double    ' 坐标最小非零距离

        ''' <summary>长度尺度网格搜索的候选值</summary>
        Private _lengthScales As Double()

        ''' <summary>
        ''' 构造 SpatialDE 模型
        ''' </summary>
        ''' <param name="coordinates">N×D 空间坐标矩阵（D 通常为 2）</param>
        ''' <param name="nGridPoints">长度尺度网格搜索点数（默认 10）</param>
        Public Sub New(coordinates As Matrix, Optional nGridPoints As Integer = 10)
            _coords = coordinates
            _n = coordinates.Rows

            ' 计算坐标间距离范围
            ComputeDistanceRange()

            ' 生成对数均匀分布的长度尺度网格
            _lengthScales = GenerateLengthScaleGrid(nGridPoints)
        End Sub

        Private Sub ComputeDistanceRange()
            _maxDist = 0.0
            _minDist = Double.MaxValue
            For i = 0 To _n - 2
                For j = i + 1 To _n - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _coords.Cols - 1
                        Dim diff = _coords(i, d) - _coords(j, d)
                        d2 += diff * diff
                    Next
                    Dim dist = Math.Sqrt(d2)
                    If dist > 0 AndAlso dist < _minDist Then
                        _minDist = dist
                    End If
                    If dist > _maxDist Then
                        _maxDist = dist
                    End If
                Next
            Next
            If _minDist = Double.MaxValue Then _minDist = 0.01
            If _maxDist = 0 Then _maxDist = 1.0
        End Sub

        Private Function GenerateLengthScaleGrid(nPoints As Integer) As Double()
            Dim grid(nPoints - 1) As Double
            Dim logMin = Math.Log(_minDist * 0.5)
            Dim logMax = Math.Log(_maxDist * 2.0)
            For i = 0 To nPoints - 1
                grid(i) = Math.Exp(logMin + (logMax - logMin) * i / (nPoints - 1))
            Next
            Return grid
        End Function

        ''' <summary>
        ''' 对一组基因表达数据执行 SpatialDE 分析
        ''' </summary>
        ''' <param name="expression">G×N 基因表达矩阵（行=基因，列=样本/坐标）</param>
        ''' <param name="geneNames">基因名数组（长度 = G）</param>
        ''' <param name="kernelType">协方差核类型（默认 SE）</param>
        ''' <param name="doModelSelection">是否执行 BIC 模型选择</param>
        Public Function Analyze(
                expression As Matrix,
                geneNames As String(),
                Optional kernelType As KernelType = KernelType.SquaredExponential,
                Optional doModelSelection As Boolean = False) As List(Of SpatialDEResult)

            If expression.Cols <> _n Then
                Throw New ArgumentException(
                    $"Expression columns ({expression.Cols}) must match coordinate count ({_n}).")
            End If

            Dim nGenes = expression.Rows
            Dim results As New List(Of SpatialDEResult)(nGenes)

            ' 预计算各长度尺度下的核矩阵 K = Σ + δI 的基（不含 δ）
            ' 注意：δ 是每次优化的，但 K 的 Cholesky 可增量更新

            ' 逐基因分析
            For g = 0 To nGenes - 1
                Dim y = expression.GetRow(g)
                Dim result = AnalyzeGene(y, geneNames(g), kernelType, doModelSelection)
                results.Add(result)

                If g Mod 100 = 0 AndAlso g > 0 Then
                    Console.WriteLine($"  [SpatialDE] 已处理 {g}/{nGenes} 基因...")
                End If
            Next

            ' BH-FDR 校正
            Dim pVals = results.Select(Function(r) r.PValue).ToArray()
            Dim qVals = Statistics.BenjaminiHochberg(pVals)
            For i = 0 To results.Count - 1
                results(i).QValue = qVals(i)
            Next

            Return results
        End Function

        ''' <summary>
        ''' 分析单个基因
        ''' </summary>
        Private Function AnalyzeGene(y As Double(), geneName As String,
                                    defaultKernel As KernelType,
                                    doModelSelection As Boolean) As SpatialDEResult

            Dim bestResult As SpatialDEResult = Nothing

            If doModelSelection Then
                ' 对三种核都做优化，选 BIC 最小者
                Dim kernels = {KernelType.SquaredExponential,
                               KernelType.Linear,
                               KernelType.Periodic}
                For Each k In kernels
                    Dim r = OptimizeGene(y, geneName, k)
                    If bestResult Is Nothing OrElse r.BIC < bestResult.BIC Then
                        bestResult = r
                    End If
                Next
            Else
                bestResult = OptimizeGene(y, geneName, defaultKernel)
            End If

            ' 零假设模型的对数似然
            Dim llNull = ComputeNullLogLikelihood(y)
            bestResult.LogLikNull = llNull

            ' 似然比统计量 → p 值
            Dim LR = bestResult.LRStat
            If LR < 0 Then LR = 0
            bestResult.PValue = Statistics.ChiSquaredSF(LR, 1)

            Return bestResult
        End Function

        ''' <summary>
        ''' 优化单个基因的 GP 超参数
        ''' </summary>
        Private Function OptimizeGene(y As Double(), geneName As String,
                                     kernelType As KernelType) As SpatialDEResult

            Dim n = y.Length
            Dim yMean = Statistics.Mean(y)
            Dim yCentered = y.Select(Function(v) v - yMean).ToArray()

            ' 对长度尺度做网格搜索，对 δ 做 Brent 优化
            Dim bestLL As Double = Double.MinValue
            Dim bestL As Double = _lengthScales(0)
            Dim bestDelta As Double = 0.01
            Dim bestMu As Double = yMean
            Dim bestSigmaSq As Double = 1.0

            For Each l In _lengthScales
                ' 计算核矩阵 K(l)
                Dim K = CovarianceKernels.ComputeKernel(_coords, l, kernelType)

                ' 对 δ 做一维优化（Brent）
                ' 目标：最大化对数边际似然
                Dim negLogLik As Func(Of Double, Double) = Function(delta)
                                                                Try
                                                                    Return -ComputeLogMarginalLikelihood(
                                                                        yCentered, K, delta, n)
                                                                Catch
                                                                    Return 1.0E+20
                                                                End Try
                                                            End Function

                Dim deltaMin = 0.0001
                Dim deltaMax = 10.0
                Dim (optDelta, optNegLL) = Optimization.BrentMinimize(
                    negLogLik, deltaMin, deltaMax, 0.001, 100)

                Dim ll = -optNegLL
                If ll > bestLL Then
                    bestLL = ll
                    bestL = l
                    bestDelta = optDelta
                    ' 重算 μ 和 σ_s²
                    Dim (_, _, sigmaSq) = ComputeMuSigma(yCentered, K, optDelta)
                    bestSigmaSq = sigmaSq
                End If
            Next

            ' 计算 BIC = log(N)·M - 2·LL（M = 参数数 = 3: μ, σ_s², δ；l 从网格选取）
            Dim bic = Math.Log(n) * 3 - 2.0 * bestLL

            Return New SpatialDEResult With {
                .GeneName = geneName,
                .LengthScale = bestL,
                .Delta = bestDelta,
                .Mu = bestMu,
                .SigmaSq = bestSigmaSq,
                .LogLikFull = bestLL,
                .BIC = bic,
                .BestKernel = kernelType
            }
        End Function

        ''' <summary>
        ''' 计算给定 (K, δ) 下的对数边际似然
        ''' LL = -N/2·log(2π) - 1/2·log|σ_s²·(K+δI)| - 1/2·(y-μ1)ᵀ(σ_s²(K+δI))⁻¹(y-μ1)
        ''' 其中 μ, σ_s² 有闭式解：
        '''   μ = (1ᵀ C⁻¹ 1)⁻¹ · (1ᵀ C⁻¹ y)
        '''   σ_s² = (y-μ1)ᵀ C⁻¹ (y-μ1) / N
        '''   C = K + δ·I
        ''' </summary>
        Private Function ComputeLogMarginalLikelihood(
                yCentered As Double(), K As Matrix,
                delta As Double, n As Integer) As Double

            Dim (mu, sigmaSq, Cinv_y) = ComputeMuSigma(yCentered, K, delta)

            ' log|C| = log|σ_s²·C| = N·log(σ_s²) + log|C|
            ' 使用 Cholesky 的对角乘积求 log|C|
            Dim C = K.AddScalar(delta) ' C = K + δ·I
            Dim logDetC = C.LogDetPosDef()

            ' 残差二次型 = yᵀ C⁻¹ y - N·μ²  (已中心化，μ_c=0 for centered y)
            ' 对于中心化的 y，μ=0，二次型 = y_cᵀ C⁻¹ y_c
            Dim quad As Double = 0.0
            For i = 0 To n - 1
                quad += yCentered(i) * Cinv_y(i)
            Next

            Dim sigmaSqVal = Math.Max(quad / n, 1.0E-20)

            ' LL = -N/2·log(2π) - 1/2·(N·log(σ_s²) + log|C|) - 1/2·N
            ' (二次型 / σ_s² = N，因 σ_s² = quad/N)
            Dim ll = -0.5 * n * Math.Log(2.0 * Math.PI) -
                     0.5 * (n * Math.Log(sigmaSqVal) + logDetC) -
                     0.5 * n

            Return ll
        End Function

        ''' <summary>
        ''' 计算闭式最优 μ, σ_s² 和 C⁻¹·y
        ''' </summary>
        Private Function ComputeMuSigma(yCentered As Double(), K As Matrix, delta As Double) _
            As (mu As Double, sigmaSq As Double, CinvY As Double())

            Dim n = yCentered.Length
            Dim C = K.AddScalar(delta) ' C = K + δ·I

            ' C⁻¹·y
            Dim CinvY = C.SolveCholesky(yCentered)

            ' C⁻¹·1
            Dim ones(n - 1) As Double
            For i = 0 To n - 1
                ones(i) = 1.0
            Next
            Dim Cinv1 = C.SolveCholesky(ones)

            ' μ = (1ᵀ C⁻¹ 1)⁻¹ · (1ᵀ C⁻¹ y)
            Dim denom As Double = 0.0  ' 1ᵀ C⁻¹ 1
            Dim numer As Double = 0.0  ' 1ᵀ C⁻¹ y
            For i = 0 To n - 1
                denom += Cinv1(i)
                numer += CinvY(i)
            Next

            Dim mu = If(Math.Abs(denom) > 1.0E-20, numer / denom, 0.0)

            ' σ_s² = (y - μ·1)ᵀ C⁻¹ (y - μ·1) / N
            Dim resid(n - 1) As Double
            For i = 0 To n - 1
                resid(i) = yCentered(i) - mu
            Next
            Dim CinvResid = C.SolveCholesky(resid)
            Dim quad As Double = 0.0
            For i = 0 To n - 1
                quad += resid(i) * CinvResid(i)
            Next
            Dim sigmaSq = Math.Max(quad / n, 1.0E-20)

            Return (mu, sigmaSq, CinvY)
        End Function

        ''' <summary>
        ''' 零假设模型对数似然：y ~ N(μ, σ²·I)
        ''' LL_null = -N/2·log(2π) - N/2·log(σ²) - N/2
        ''' 其中 σ² = var(y)
        ''' </summary>
        Private Function ComputeNullLogLikelihood(y As Double()) As Double
            Dim n = y.Length
            Dim mu = Statistics.Mean(y)
            Dim sigmaSq = Statistics.Variance(y)
            If sigmaSq < 1.0E-20 Then sigmaSq = 1.0E-20

            ' LL = -N/2·log(2π) - N/2·log(σ²) - N/2
            ' (因 Σ(y-μ)²/(σ²) = (N-1)·var(y)/σ² = N-1 ≈ N)
            Return -0.5 * n * Math.Log(2.0 * Math.PI) -
                   0.5 * n * Math.Log(sigmaSq) -
                   0.5 * (n - 1)
        End Function

    End Class

End Namespace
