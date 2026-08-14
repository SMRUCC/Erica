' ============================================================================
' Kriging.vb — 普通克里金插值
' ----------------------------------------------------------------------------
' 实现 spatialGE 的转录组表面（transcriptomic surface）空间插值功能。
' 使用普通克里金（Ordinary Kriging）方法，基于变异函数（variogram）拟合
' 空间相关性模型，在未采样位置进行无偏最优估计。
'
' 算法概述（Diggle & Ribeiro, 2007; Cressie, 1993）：
'   1. 计算经验半变异函数 γ(h) = 1/(2N(h)) · Σ|z(xᵢ) - z(xⱼ)|²
'   2. 拟合理论变异函数模型（球状/Spherical 模型）
'   3. 在目标位置 x₀：求解克里金权重 λ：
'      Γ · λ = g  →  λ = Γ⁻¹ · g
'      其中 Γ 为样本间变异函数矩阵，g 为样本到目标点的变异函数向量
'   4. 估计值 ẑ(x₀) = Σ λᵢ · z(xᵢ)
'   5. 估计方差 σ²(x₀) = Σ λᵢ · γ(xᵢ, x₀) + μ（拉格朗日乘子）
'
' 参考：
'   Cressie, N. (1993) Statistics for Spatial Data. Wiley.
'   Diggle, P.J. & Ribeiro, P.J. (2007) Model-based Geostatistics. Springer.
'   Ospina et al. (2022) Bioinformatics 38(9):2645-2647
' ============================================================================

Imports SpatialOmics.Math
Imports System
Imports System.Linq

Namespace SpatialOmics.SpatialGE

    ''' <summary>变异函数模型类型</summary>
    Public Enum VariogramModel
        ''' <summary>球状模型（最常用）</summary>
        Spherical
        ''' <summary>指数模型</summary>
        Exponential
        ''' <summary>高斯模型</summary>
        Gaussian
    End Enum

    ''' <summary>变异函数参数</summary>
    Public Class VariogramParams
        ''' <summary>块金值 (nugget) — 微尺度随机变异</summary>
        Public Property Nugget As Double
        ''' <summary>基台值 (sill) = nugget + partial_sill</summary>
        Public Property Sill As Double
        ''' <summary>变程 (range) — 空间相关距离</summary>
        Public Property Range As Double
        ''' <summary>模型类型</summary>
        Public Property Model As VariogramModel = VariogramModel.Spherical

        ''' <summary>偏基台 = Sill - Nugget</summary>
        Public ReadOnly Property PartialSill As Double
            Get
                Return Sill - Nugget
            End Get
        End Property
    End Class

    ''' <summary>克里金插值结果</summary>
    Public Class KrigingResult

        ''' <summary>插值点数</summary>
        Public Property NPredictions As Integer

        ''' <summary>预测值数组</summary>
        Public Property Predictions As Double()

        ''' <summary>预测方差数组</summary>
        Public Property Variances As Double()

        ''' <summary>拟合的变异函数参数</summary>
        Public Property Variogram As VariogramParams

    End Class

    ''' <summary>
    ''' 普通克里金插值器
    ''' </summary>
    Public Class OrdinaryKriging

        Private _sampleCoords As Matrix ' M×D 已采样坐标
        Private _nSamples As Integer

        ''' <summary>构造：传入已采样坐标</summary>
        Public Sub New(sampleCoords As Matrix)
            _sampleCoords = sampleCoords
            _nSamples = sampleCoords.Rows
        End Sub

        ''' <summary>
        ''' 计算经验半变异函数
        ''' γ(h) = 1/(2·N(h)) · Σ_{(i,j)∈N(h)} |z(xᵢ) - z(xⱼ)|²
        ''' </summary>
        ''' <param name="values">采样值</param>
        ''' <param name="nLags">滞后阶数（默认 10）</param>
        Public Function ComputeEmpiricalVariogram(
                values As Double(), Optional nLags As Integer = 10) _
            As (distances As Double(), gamma As Double(), counts As Integer())

            ' 计算所有样本对距离和半方差
            Dim pairs As New List(Of (dist As Double, halfVar As Double))
            For i = 0 To _nSamples - 2
                For j = i + 1 To _nSamples - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _sampleCoords.Cols - 1
                        Dim diff = _sampleCoords(i, d) - _sampleCoords(j, d)
                        d2 += diff * diff
                    Next
                    Dim dist = Math.Sqrt(d2)
                    Dim halfVar = 0.5 * (values(i) - values(j)) ^ 2
                    pairs.Add((dist, halfVar))
                Next
            Next

            ' 最大距离
            Dim maxDist = pairs.Max(Function(p) p.dist)
            Dim lagSize = maxDist / nLags

            ' 分组统计
            Dim distances(nLags - 1) As Double
            Dim gamma(nLags - 1) As Double
            Dim counts(nLags - 1) As Integer

            For Each pair In pairs
                Dim lagIdx = CInt(Math.Floor(pair.dist / lagSize))
                If lagIdx >= nLags Then lagIdx = nLags - 1
                distances(lagIdx) += pair.dist
                gamma(lagIdx) += pair.halfVar
                counts(lagIdx) += 1
            Next

            For i = 0 To nLags - 1
                If counts(i) > 0 Then
                    distances(i) /= counts(i)
                    gamma(i) /= counts(i)
                End If
            Next

            ' 过滤空组
            Dim validIdx = Enumerable.Range(0, nLags).Where(Function(i) counts(i) > 0).ToArray()
            Dim validDist = validIdx.Select(Function(i) distances(i)).ToArray()
            Dim validGamma = validIdx.Select(Function(i) gamma(i)).ToArray()
            Dim validCounts = validIdx.Select(Function(i) counts(i)).ToArray()

            Return (validDist, validGamma, validCounts)
        End Function

        ''' <summary>
        ''' 拟合理论变异函数模型（最小二乘拟合球状模型）
        ''' Spherical: γ(h) = nugget + partial_sill · (3h/(2r) - h³/(2r³))  for h < r
        '''            γ(h) = sill  for h ≥ r
        ''' </summary>
        Public Function FitVariogram(distances As Double(), gamma As Double(),
                                     Optional modelType As VariogramModel = VariogramModel.Spherical) _
            As VariogramParams

            ' 初始猜测
            Dim nugget0 = If(gamma.Length > 0, gamma(0) * 0.5, 0.0)
            Dim sill0 = If(gamma.Length > 0, gamma.Max(), 1.0)
            Dim range0 = If(distances.Length > 0, distances.Max() * 0.5, 1.0)

            ' 简单的网格搜索 + 局部精调
            Dim bestSSD As Double = Double.MaxValue
            Dim bestParams As New VariogramParams With {
                .Nugget = nugget0,
                .Sill = sill0,
                .Range = range0,
                .Model = modelType
            }

            ' 在参数空间上做粗网格搜索
            Dim nuggetGrid = {nugget0 * 0.1, nugget0 * 0.3, nugget0 * 0.5, nugget0 * 0.8, nugget0}
            Dim sillGrid = {sill0 * 0.5, sill0 * 0.8, sill0, sill0 * 1.2, sill0 * 1.5}
            Dim rangeGrid = {range0 * 0.3, range0 * 0.5, range0 * 0.8, range0, range0 * 1.5}

            For Each nug In nuggetGrid
                For Each sil In sillGrid
                    For Each rng In rangeGrid
                        Dim ssd As Double = 0.0
                        For i = 0 To distances.Length - 1
                            Dim pred = EvaluateVariogram(distances(i), nug, sil, rng, modelType)
                            Dim resid = gamma(i) - pred
                            ssd += resid * resid
                        Next
                        If ssd < bestSSD Then
                            bestSSD = ssd
                            bestParams.Nugget = Math.Max(0, nug)
                            bestParams.Sill = Math.Max(nug + 0.0001, sil)
                            bestParams.Range = Math.Max(0.0001, rng)
                        End If
                    Next
                Next
            Next

            Return bestParams
        End Function

        ''' <summary>计算变异函数值</summary>
        Private Function EvaluateVariogram(h As Double, nugget As Double,
                                           sill As Double, range As Double,
                                           modelType As VariogramModel) As Double
            Dim partialSill = sill - nugget
            Select Case modelType
                Case VariogramModel.Spherical
                    If h >= range Then
                        Return sill
                    Else
                        Return nugget + partialSill *
                               (1.5 * h / range - 0.5 * (h / range) ^ 3)
                    End If
                Case VariogramModel.Exponential
                    Return nugget + partialSill * (1.0 - Math.Exp(-3.0 * h / range))
                Case VariogramModel.Gaussian
                    Return nugget + partialSill * (1.0 - Math.Exp(-3.0 * (h / range) ^ 2))
                Case Else
                    Return EvaluateVariogram(h, nugget, sill, range, VariogramModel.Spherical)
            End Select
        End Function

        ''' <summary>
        ''' 在目标位置执行普通克里金插值
        ''' </summary>
        ''' <param name="values">采样值</param>
        ''' <param name="targetCoords">目标位置坐标矩阵（P×D）</param>
        ''' <param name="varioParams">变异函数参数</param>
        Public Function Interpolate(values As Double(), targetCoords As Matrix,
                                    varioParams As VariogramParams) As KrigingResult

            Dim nTargets = targetCoords.Rows
            Dim predictions(nTargets - 1) As Double
            Dim variances(nTargets - 1) As Double

            ' 构建样本间变异函数矩阵 Γ (N+1 × N+1)
            ' 最后一行/列用于拉格朗日乘子 μ
            Dim dim = _nSamples + 1
            Dim Gamma As New Matrix(dim, dim)
            For i = 0 To _nSamples - 1
                For j = 0 To _nSamples - 1
                    If i = j Then
                        Gamma(i, j) = varioParams.Nugget ' 对角线 = 0 or nugget
                    Else
                        Dim d2 As Double = 0.0
                        For d = 0 To _sampleCoords.Cols - 1
                            Dim diff = _sampleCoords(i, d) - _sampleCoords(j, d)
                            d2 += diff * diff
                        Next
                        Gamma(i, j) = EvaluateVariogram(
                            Math.Sqrt(d2), varioParams.Nugget, varioParams.Sill,
                            varioParams.Range, varioParams.Model)
                    End If
                Next
                ' 最后一列/行 = 1（无偏约束）
                Gamma(i, _nSamples) = 1.0
                Gamma(_nSamples, i) = 1.0
            Next
            Gamma(_nSamples, _nSamples) = 0.0

            ' 预计算 Γ 的逆（所有目标点共用）
            Dim GammaInv = Gamma.Inverse()

            ' 逐目标点插值
            For p = 0 To nTargets - 1
                ' 构建变异函数向量 g (N+1)
                Dim g(dim - 1) As Double
                For i = 0 To _nSamples - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _sampleCoords.Cols - 1
                        Dim diff = _sampleCoords(i, d) - targetCoords(p, d)
                        d2 += diff * diff
                    Next
                    g(i) = EvaluateVariogram(
                        Math.Sqrt(d2), varioParams.Nugget, varioParams.Sill,
                        varioParams.Range, varioParams.Model)
                Next
                g(_nSamples) = 1.0

                ' λ = Γ⁻¹ · g
                Dim lambda(dim - 1) As Double
                For i = 0 To dim - 1
                    Dim s As Double = 0.0
                    For j = 0 To dim - 1
                        s += GammaInv(i, j) * g(j)
                    Next
                    lambda(i) = s
                Next

                ' ẑ(x₀) = Σ λᵢ · z(xᵢ)
                Dim pred As Double = 0.0
                For i = 0 To _nSamples - 1
                    pred += lambda(i) * values(i)
                Next
                predictions(p) = pred

                ' σ²(x₀) = Σ λᵢ · γ(xᵢ, x₀) + μ
                Dim varEst As Double = 0.0
                For i = 0 To _nSamples - 1
                    varEst += lambda(i) * g(i)
                Next
                varEst += lambda(_nSamples) ' μ
                variances(p) = Math.Max(0, varEst)
            Next

            Return New KrigingResult With {
                .NPredictions = nTargets,
                .Predictions = predictions,
                .Variances = variances,
                .Variogram = varioParams
            }
        End Function

    End Class

End Namespace
