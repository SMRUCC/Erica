' ============================================================================
' SpatialGEModel.vb — spatialGE 主分析模型
' ----------------------------------------------------------------------------
' 整合空间统计量、克里金插值和 STclust 聚类，提供 spatialGE 的完整
' 分析流程接口（Ospina et al., Bioinformatics 2022）。
'
' 流程：
'   1. 数据标准化（library size + log2 变换）
'   2. 逐基因计算 STHet 统计量（Moran's I / Geary's C / Getis-Ord Gi*）
'   3. 克里金空间插值（构建转录组表面）
'   4. STclust 空间感知聚类
'   5. 汇总结果
' ============================================================================

Imports SpatialOmics.Math
Imports System
Imports System.Linq

Namespace SpatialOmics.SpatialGE

    ''' <summary>spatialGE 分析结果</summary>
    Public Class SpatialGEAnalysisResult

        ''' <summary>空间自相关统计结果（每基因一行）</summary>
        Public Property SpatialStats As List(Of SpatialStatsResult)

        ''' <summary>克里金插值结果（每基因一个）</summary>
        Public Property KrigingSurfaces As Dictionary(Of String, KrigingResult)

        ''' <summary>STclust 聚类结果</summary>
        Public Property Clustering As STclustResult

        ''' <summary>标准化方法描述</summary>
        Public Property NormalizationMethod As String

    End Class

    ''' <summary>
    ''' spatialGE 主分析器
    ''' </summary>
    Public Class SpatialGEModel

        Private _coords As Matrix  ' N×D 空间坐标
        Private _n As Integer

        ''' <summary>构造：传入空间坐标矩阵（N×D，D 通常为 2）</summary>
        Public Sub New(coordinates As Matrix)
            _coords = coordinates
            _n = coordinates.Rows
        End Sub

        ''' <summary>
        ''' 库大小标准化 + log2 变换
        ''' </summary>
        ''' <param name="counts">G×N 计数矩阵</param>
        ''' <param name="pseudo">伪计数（默认 1）</param>
        Public Function NormalizeLog2(counts As Matrix, Optional pseudo As Double = 1.0) As Matrix
            Dim nGenes = counts.Rows
            Dim result As New Matrix(nGenes, _n)

            For j = 0 To _n - 1
                ' 计算库大小
                Dim libSize As Double = 0.0
                For g = 0 To nGenes - 1
                    libSize += counts(g, j)
                Next
                If libSize < 1.0 Then libSize = 1.0

                ' 标准化 + log2
                For g = 0 To nGenes - 1
                    ' CPM (counts per million) + log2
                    Dim cpm = counts(g, j) / libSize * 1.0E6
                    result(g, j) = Math.Log2(cpm + pseudo)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 逐基因计算空间自相关统计量
        ''' </summary>
        ''' <param name="expression">G×N 表达矩阵</param>
        ''' <param name="geneNames">基因名</param>
        ''' <param name="maxDist">空间权重最大距离（默认 = 中位距离）</param>
        Public Function ComputeSpatialStats(expression As Matrix,
                                           geneNames As String(),
                                           Optional maxDist As Double = -1) _
            As List(Of SpatialStatsResult)

            ' 自动估计 maxDist
            If maxDist <= 0 Then
                maxDist = EstimateMedianDistance()
            End If

            ' 构建空间权重矩阵
            Dim calc As New SpatialStatistics(_coords)
            Dim W = calc.BuildDistanceWeights(maxDist)

            Dim results As New List(Of SpatialStatsResult)
            For g = 0 To expression.Rows - 1
                Dim y = expression.GetRow(g)
                Dim res = calc.ComputeAll(y, W)
                res.GeneName = geneNames(g)
                results.Add(res)
            Next

            ' 按 Moran's I 降序排序
            Return results.OrderByDescending(Function(r) r.MoransI).ToList()
        End Function

        ''' <summary>
        ''' 构建转录组表面（克里金插值）
        ''' </summary>
        ''' <param name="expression">G×N 表达矩阵</param>
        ''' <param name="geneNames">基因名</param>
        ''' <param name="targetCoords">目标网格坐标</param>
        Public Function BuildTranscriptomicSurfaces(
                expression As Matrix, geneNames As String(),
                targetCoords As Matrix) _
            As Dictionary(Of String, KrigingResult)

            Dim surfaces As New Dictionary(Of String, KrigingResult)
            Dim krig As New OrdinaryKriging(_coords)

            For g = 0 To expression.Rows - 1
                Dim y = expression.GetRow(g)

                ' 拟合变异函数
                Dim (dists, gammas, counts) = krig.ComputeEmpiricalVariogram(y, 10)
                Dim vario = krig.FitVariogram(dists, gammas, VariogramModel.Spherical)

                ' 插值
                Dim res = krig.Interpolate(y, targetCoords, vario)
                surfaces(geneNames(g)) = res
            Next

            Return surfaces
        End Function

        ''' <summary>
        ''' STclust: 空间感知聚类
        ''' </summary>
        ''' <param name="expression">G×N 表达矩阵</param>
        ''' <param name="geneNames">基因名</param>
        ''' <param name="nClusters">目标聚类数</param>
        ''' <param name="nTopGenes">使用的高变异基因数</param>
        ''' <param name="spatialWeight">空间权重 w</param>
        Public Function RunSTclust(expression As Matrix, geneNames As String(),
                                   nClusters As Integer,
                                   Optional nTopGenes As Integer = 1000,
                                   Optional spatialWeight As Double = 0.5) _
            As STclustResult

            Dim stc As New STclust(_coords)
            Return stc.Run(expression, geneNames, nClusters, nTopGenes, spatialWeight)
        End Function

        ''' <summary>
        ''' 完整分析流程
        ''' </summary>
        Public Function RunFull(expression As Matrix, geneNames As String(),
                               Optional nClusters As Integer = 5,
                               Optional doKriging As Boolean = False,
                               Optional krigingGrid As Matrix = Nothing,
                               Optional spatialWeight As Double = 0.5) _
            As SpatialGEAnalysisResult

            ' 1. 空间统计
            Dim stats = ComputeSpatialStats(expression, geneNames)

            ' 2. 克里金（可选）
            Dim surfaces As Dictionary(Of String, KrigingResult) = Nothing
            If doKriging AndAlso krigingGrid IsNot Nothing Then
                surfaces = BuildTranscriptomicSurfaces(expression, geneNames, krigingGrid)
            End If

            ' 3. STclust
            Dim clust = RunSTclust(expression, geneNames, nClusters,
                                   spatialWeight:=spatialWeight)

            Return New SpatialGEAnalysisResult With {
                .SpatialStats = stats,
                .KrigingSurfaces = surfaces,
                .Clustering = clust,
                .NormalizationMethod = "log2-CPM"
            }
        End Function

        ''' <summary>估计中位样本间距</summary>
        Private Function EstimateMedianDistance() As Double
            Dim dists As New List(Of Double)
            For i = 0 To _n - 2
                For j = i + 1 To _n - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To _coords.Cols - 1
                        Dim diff = _coords(i, d) - _coords(j, d)
                        d2 += diff * diff
                    Next
                    dists.Add(Math.Sqrt(d2))
                Next
            Next
            dists.Sort()
            Return dists(dists.Count \ 2)
        End Function

    End Class

End Namespace
