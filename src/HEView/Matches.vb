Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.GraphTheory.KdTree
Imports Microsoft.VisualBasic.Math.Correlations
Imports std = System.Math

''' <summary>
''' 细胞匹配结果类
''' </summary>
Public Class CellMatchResult

    Public Property CellA As CellScan
    Public Property CellB As CellScan
    Public Property Distance As Double
    Public Property Morphology As Double

    ''' <summary>
    ''' the total match score
    ''' </summary>
    ''' <returns></returns>
    Public Property MatchScore As Double

    Public Overrides Function ToString() As String
        Return $"Match: A({CellA.physical_x:F2}, {CellA.physical_y:F2}) <-> B({CellB.physical_x:F2}, {CellB.physical_y:F2}) | Dist: {Distance:F2} | Score: {MatchScore:F4}"
    End Function
End Class

''' <summary>
''' 细胞匹配器 - 使用贪心算法进行细胞一一对应
''' </summary>
Public Class CellMatcher

    ReadOnly _maxDistanceThreshold As Double = 100.0 ' 最大距离阈值
    ReadOnly _morphologyWeight As Double = 0.3 ' 形态学特征权重
    ReadOnly _distanceWeight As Double = 0.7 ' 距离权重

    ''' <summary>
    ''' 细胞访问器 - 用于KD树操作
    ''' </summary>
    Private Class CellScanAccessor : Inherits KdNodeAccessor(Of CellScan)

        Public Overrides Function GetDimensions() As String()
            Return {"physical_x", "physical_y"}
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overrides Function metric(a As CellScan, b As CellScan) As Double
            Return a.SquareDistance(b)
        End Function

        Public Overrides Function getByDimension(x As CellScan, dimName As String) As Double
            Select Case dimName
                Case "physical_x" : Return x.physical_x
                Case "physical_y" : Return x.physical_y
                Case Else
                    Return 0.0
            End Select
        End Function

        Public Overrides Sub setByDimension(x As CellScan, dimName As String, value As Double)
            Select Case dimName
                Case "physical_x" : x.physical_x = value
                Case "physical_y" : x.physical_y = value
            End Select
        End Sub

        Public Overrides Function nodeIs(a As CellScan, b As CellScan) As Boolean
            ' 基于空间位置判断细胞是否相同
            Return a.physical_x = b.physical_x AndAlso a.physical_y = b.physical_y
        End Function

        Public Overrides Function activate() As CellScan
            Return New CellScan()
        End Function
    End Class

    ''' <summary>
    ''' 设置匹配参数
    ''' </summary>
    Public Sub New(maxDistance As Double, distanceWeight As Double, morphologyWeight As Double)
        _maxDistanceThreshold = maxDistance
        _distanceWeight = distanceWeight
        _morphologyWeight = morphologyWeight
    End Sub

    ''' <summary>
    ''' 计算两个细胞的形态学相似度
    ''' </summary>
    Private Function CalculateMorphologySimilarity(cellA As CellScan, cellB As CellScan) As Double
        ' 归一化处理各个形态学参数
        Dim areaSim As Double = 1.0 - std.Abs(cellA.area - cellB.area) / (std.Max(cellA.area, cellB.area) + 0.001)
        Dim ratioSim As Double = 1.0 - std.Abs(cellA.ratio - cellB.ratio) / (std.Max(cellA.ratio, cellB.ratio) + 0.001)
        Dim densitySim As Double = 1.0 - std.Abs(cellA.density - cellB.density) / (std.Max(cellA.density, cellB.density) + 0.001)
        Dim weightSim As Double = 1.0 - std.Abs(cellA.weight - cellB.weight) / (std.Max(cellA.weight, cellB.weight) + 0.001)

        ' 计算综合相似度（加权平均）
        Return (areaSim * 0.3 + ratioSim * 0.3 + densitySim * 0.2 + weightSim * 0.2)
    End Function

    ''' <summary>
    ''' 计算匹配得分（综合考虑距离和形态学特征）
    ''' </summary>
    Private Function CalculateMatchScore(cellA As CellScan, cellB As CellScan, distance As Double, ByRef morphologyScore As Double) As Double
        ' 距离得分（距离越小得分越高）
        Dim distanceScore As Double = std.Max(0, 1.0 - distance / _maxDistanceThreshold)
        ' 形态学相似度得分
        morphologyScore = CalculateMorphologySimilarity(cellA, cellB)
        ' 综合得分
        Return distanceScore * _distanceWeight + morphologyScore * _morphologyWeight
    End Function

    ''' <summary>
    ''' 使用贪心算法进行细胞匹配
    ''' </summary>
    ''' <param name="cellsA">第一张切片的细胞列表</param>
    ''' <param name="cellsB">第二张切片的细胞列表</param>
    ''' <returns>匹配结果列表</returns>
    Public Function GreedyMatchCells(cellsA As CellScan(), cellsB As CellScan()) As IEnumerable(Of CellMatchResult)
        ' 参数验证
        If cellsA.IsNullOrEmpty OrElse cellsB.IsNullOrEmpty Then
            Return New CellMatchResult() {}
        Else
            Return GreedyMatchCellsInternal(cellsA, cellsB)
        End If
    End Function

    ''' <summary>
    ''' 使用贪心算法进行细胞匹配
    ''' </summary>
    ''' <param name="cellsA">第一张切片的细胞列表</param>
    ''' <param name="cellsB">第二张切片的细胞列表</param>
    ''' <returns>匹配结果列表</returns>
    Public Iterator Function GreedyMatchCellsInternal(cellsA As CellScan(), cellsB As CellScan()) As IEnumerable(Of CellMatchResult)
        ' 创建KD树访问器
        Dim accessor As New CellScanAccessor()
        ' 使用第二张切片的细胞构建KD树
        Dim kdTree As New KdTree(Of CellScan)(cellsB, accessor)
        ' 记录已匹配的细胞
        Dim matchedCellsA As New HashSet(Of CellScan)()
        Dim matchedCellsB As New HashSet(Of CellScan)()

        ' 为第一张切片的每个细胞寻找最佳匹配
        For Each cellA As CellScan In cellsA
            If matchedCellsA.Contains(cellA) Then Continue For

            ' 在KD树中搜索最近的几个细胞
            Dim nearestNeighbors = kdTree.nearest(cellA, 5, _maxDistanceThreshold).ToList()

            Dim bestMatch As CellMatchResult = Nothing
            Dim bestScore As Double = -1.0

            ' 在最近的邻居中寻找最佳匹配
            For Each neighbor In nearestNeighbors
                Dim cellB = neighbor.node.data

                ' 如果细胞B已被匹配，跳过
                If matchedCellsB.Contains(cellB) Then Continue For

                ' 计算实际距离（开方得到欧几里得距离）
                Dim actualDistance As Double = std.Sqrt(neighbor.distance)
                Dim morphologyScore As Double = 0
                ' 计算匹配得分
                Dim score As Double = CalculateMatchScore(cellA, cellB, actualDistance, morphologyScore)

                ' 更新最佳匹配
                If score > bestScore Then
                    bestScore = score
                    bestMatch = New CellMatchResult With {
                        .CellA = cellA,
                        .CellB = cellB,
                        .Distance = actualDistance,
                        .MatchScore = score,
                        .Morphology = morphologyScore
                    }
                End If
            Next

            ' 如果找到有效匹配且得分足够高，则记录匹配
            If bestMatch IsNot Nothing AndAlso bestScore > 0.3 Then
                matchedCellsA.Add(cellA)
                matchedCellsB.Add(bestMatch.CellB)

                Yield bestMatch
            End If
        Next

        ' 处理剩余未匹配的细胞（反向匹配）
        Dim unmatchedCellsB = cellsB.Where(Function(c) Not matchedCellsB.Contains(c)).ToList()

        If unmatchedCellsB.Count > 0 Then
            ' 为第一张切片构建KD树进行反向匹配
            Dim kdTreeA As New KdTree(Of CellScan)(From c As CellScan In cellsA Where Not matchedCellsA.Contains(c), accessor)

            For Each cellB As CellScan In unmatchedCellsB
                Dim nearestNeighbors = kdTreeA.nearest(cellB, 3, _maxDistanceThreshold).ToList()

                Dim bestMatch As CellMatchResult = Nothing
                Dim bestScore As Double = -1.0

                For Each neighbor In nearestNeighbors
                    Dim cellA = neighbor.node.data

                    If matchedCellsA.Contains(cellA) Then
                        Continue For
                    End If

                    Dim morphologyScore As Double = 0
                    Dim actualDistance As Double = std.Sqrt(neighbor.distance)
                    Dim score As Double = CalculateMatchScore(cellA, cellB, actualDistance, morphologyScore)

                    If score > bestScore Then
                        bestScore = score
                        bestMatch = New CellMatchResult With {
                            .CellA = cellA,
                            .CellB = cellB,
                            .Distance = actualDistance,
                            .MatchScore = score,
                            .Morphology = morphologyScore
                        }
                    End If
                Next

                If bestMatch IsNot Nothing AndAlso bestScore > 0.3 Then
                    matchedCellsA.Add(bestMatch.CellA)
                    matchedCellsB.Add(cellB)

                    Yield bestMatch
                End If
            Next
        End If
    End Function

    ''' <summary>
    ''' 获取匹配统计信息
    ''' </summary>
    Public Function GetMatchStatistics(matches As CellMatchResult(), totalCellsA As Integer, totalCellsB As Integer) As String
        Dim matchedA = matches.Select(Function(m) m.CellA).Distinct().Count()
        Dim matchedB = matches.Select(Function(m) m.CellB).Distinct().Count()

        Return $"匹配统计: " & vbCrLf &
               $"切片A: {matchedA}/{totalCellsA} 细胞已匹配 ({matchedA / totalCellsA * 100:F1}%)" & vbCrLf &
               $"切片B: {matchedB}/{totalCellsB} 细胞已匹配 ({matchedB / totalCellsB * 100:F1}%)" & vbCrLf &
               $"成功匹配对数: {matches.Length}" & vbCrLf &
               $"平均匹配距离: {matches.Average(Function(m) m.Distance):F2}" & vbCrLf &
               $"平均匹配得分: {matches.Average(Function(m) m.MatchScore):F4}"
    End Function
End Class

