Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Bootstrapping
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports std = System.Math

Public Module SimpleCellMorphology

    ''' <summary>
    ''' 计算拟合直线的相关指标
    ''' </summary>
    ''' <param name="points">输入数据点集合（PointF数组）</param>
    ''' <returns>包含弧度夹角、线段长度和平均距离的CalculationResult对象</returns>
    ''' 
    <Extension>
    Public Function CalculateMetrics(points As PointF()) As EllipseFitResult
        ' 1. 提取x和y坐标数组，用于线性拟合
        Dim length As Integer = points.Length
        Dim x(length - 1) As Double
        Dim y(length - 1) As Double
        For i As Integer = 0 To length - 1
            x(i) = points(i).X
            y(i) = points(i).Y
        Next

        ' 调用已实现的线性拟合函数
        Dim fitResult As FitResult = LinearFit(x, y, length)
        Dim slope As Double = fitResult.Slope
        Dim intercept As Double = fitResult.Intercept

        ' 2. 计算拟合直线与X轴的弧度夹角
        Dim thetaRad As Double = std.Atan(slope) ' 初始弧度值（范围在 -π/2 到 π/2 之间）
        ' 调整角度范围到 [0, π) 弧度：当斜率为负时，夹角在第二象限
        If slope < 0 Then
            thetaRad = std.PI + thetaRad ' 使结果在 π/2 到 π 之间
        End If
        ' 注意：当斜率无穷大时（垂直直线），Math.Atan 可能返回 π/2，但线性拟合通常不会产生无穷大斜率

        ' 3. 计算拟合线段在数据范围内的长度
        ' 找到输入点的x坐标范围
        Dim xMin As Double = points.Min(Function(p) p.X)
        Dim xMax As Double = points.Max(Function(p) p.X)
        ' 计算线段端点坐标
        Dim yMin As Double = slope * xMin + intercept
        Dim yMax As Double = slope * xMax + intercept
        ' 计算线段长度（欧几里得距离）
        Dim segmentLength As Double = std.Sqrt((xMax - xMin) ^ 2 + (yMax - yMin) ^ 2)

        ' 4. 计算每个数据点到拟合线段的距离的平均值
        Dim totalDistance As Double() = New Double(points.Length - 1) {}
        Dim p1 As New PointF(CSng(xMin), CSng(yMin)) ' 线段起点
        Dim p2 As New PointF(CSng(xMax), CSng(yMax)) ' 线段终点
        Dim pt As PointF

        For i As Integer = 0 To points.Length - 1
            pt = points(i)
            totalDistance(i) = PointToSegmentDistance(pt, p1, p2)
        Next
        Dim r2 As Double = totalDistance.Max

        ' 返回结果
        Return New EllipseFitResult With {
            .RotationAngle = thetaRad,
            .SemiMajorAxis = segmentLength,
            .SemiMinorAxis = r2
        }
    End Function

    ''' <summary>
    ''' 计算点到一个线段的距离（非无限直线）
    ''' </summary>
    ''' <param name="p">目标点</param>
    ''' <param name="p1">线段起点</param>
    ''' <param name="p2">线段终点</param>
    ''' <returns>点到线段的距离</returns>
    Private Function PointToSegmentDistance(p As PointF, p1 As PointF, p2 As PointF) As Double
        ' 计算线段向量
        Dim dx As Double = p2.X - p1.X
        Dim dy As Double = p2.Y - p1.Y
        Dim lenSq As Double = dx * dx + dy * dy ' 线段长度的平方

        ' 如果线段长度为0（即p1和p2重合），直接返回点到点的距离
        If lenSq = 0 Then
            Return std.Sqrt((p.X - p1.X) ^ 2 + (p.Y - p1.Y) ^ 2)
        End If

        ' 计算向量p1->p与线段向量p1->p2的点积，并归一化得到参数t
        Dim t As Double = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / lenSq

        If t < 0 Then
            ' 投影点在线段起点外侧，最近点为p1
            Return std.Sqrt((p.X - p1.X) ^ 2 + (p.Y - p1.Y) ^ 2)
        ElseIf t > 1 Then
            ' 投影点在线段终点外侧，最近点为p2
            Return std.Sqrt((p.X - p2.X) ^ 2 + (p.Y - p2.Y) ^ 2)
        Else
            ' 投影点在线段上，计算点到直线的距离
            ' 直线方程的一般式：A = dy, B = -dx, C = dx*p1.Y - dy*p1.X
            Dim A As Double = dy
            Dim B As Double = -dx
            Dim C As Double = dx * p1.Y - dy * p1.X
            Dim distance As Double = std.Abs(A * p.X + B * p.Y + C) / std.Sqrt(A * A + B * B)
            Return distance
        End If
    End Function
End Module
