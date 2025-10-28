Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Bootstrapping
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.DelaunayVoronoi
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports std = System.Math

Public Module SimpleCellMorphology

    <Extension>
    Public Function Measure(shape As IEnumerable(Of PointF)) As EllipseFitResult
        With shape.ToArray
            ' 1. 准备数据进行线性拟合
            Dim size As Integer = .Length
            Dim xValues As Double() = .Select(Function(p) CDbl(p.X)).ToArray()
            Dim yValues As Double() = .Select(Function(p) CDbl(p.Y)).ToArray()
            ' 2. 调用现有的线性拟合函数
            Dim fitResult As FitResult = LinearFit(xValues, yValues, size)
            Dim r1 = .CalculateLineLength(fitResult)
            Dim r2 = .CalculatePerpendicularLength(fitResult)
            ' 3. 计算各项指标
            Dim ellipse As New EllipseFitResult With {
                .RotationAngle = CalculateAngleWithXAxis(fitResult.Slope),
                .SemiMajorAxis = r1,
                .SemiMinorAxis = r2
            }

            Return ellipse
        End With
    End Function

    ''' <summary>
    ''' 计算直线与X轴的夹角（度数）
    ''' </summary>
    Private Function CalculateAngleWithXAxis(slope As Double) As Double
        ' 使用Atan计算弧度，然后转换为角度
        Dim angleRad As Double = std.Atan(slope)
        Dim angleDeg As Double = angleRad * (180.0 / std.PI)

        ' 确保角度在0-180度范围内
        If angleDeg < 0 Then
            angleDeg += 180
        ElseIf angleDeg >= 180 Then
            angleDeg -= 180
        End If

        Return angleDeg.ToRadians
    End Function

    ''' <summary>
    ''' 计算在输入数据范围内的线段长度
    ''' </summary>
    ''' 
    <Extension>
    Private Function CalculateLineLength(points As PointF(), fitResult As FitResult) As Double
        ' 找到数据在X轴上的范围
        Dim minX As Single = points.Min(Function(p) p.X)
        Dim maxX As Single = points.Max(Function(p) p.X)

        ' 计算范围内直线段的两个端点
        Dim startPoint As New PointF(minX, CSng(fitResult.Slope * minX + fitResult.Intercept))
        Dim endPoint As New PointF(maxX, CSng(fitResult.Slope * maxX + fitResult.Intercept))

        ' 计算两点间距离
        Return startPoint.Distance(endPoint)
    End Function

    ''' <summary>
    ''' 计算垂直于方程式线段的垂直线段在输入数据范围内的长度
    ''' </summary>
    <Extension>
    Private Function CalculatePerpendicularLength(points As PointF(), fitResult As FitResult) As Double
        ' 计算法向量的斜率（垂直线的斜率）
        Dim perpendicularSlope As Double = -1.0 / fitResult.Slope

        ' 找到数据的中心点
        Dim centerX As Single = points.Average(Function(p) p.X)
        Dim centerY As Single = points.Average(Function(p) p.Y)

        ' 计算通过中心点的垂直线与数据边界相交的两点
        ' 这里需要计算垂直线与数据边界框的交点
        Dim perpendicularLine As New LineWithSlope(perpendicularSlope, centerY - perpendicularSlope * centerX)
        Dim boundingBox As RectangleF = GetBoundingBox(points)

        Dim intersections As List(Of PointF) = GetLineBoundingBoxIntersections(perpendicularLine, boundingBox)

        If intersections.Count >= 2 Then
            Return intersections(0).Distance(intersections(1))
        End If

        Return 0
    End Function

    ''' <summary>
    ''' 获取点的边界框
    ''' </summary>
    Private Function GetBoundingBox(points As PointF()) As RectangleF
        Dim minX As Single = points.Min(Function(p) p.X)
        Dim maxX As Single = points.Max(Function(p) p.X)
        Dim minY As Single = points.Min(Function(p) p.Y)
        Dim maxY As Single = points.Max(Function(p) p.Y)

        Return New RectangleF(minX, minY, maxX - minX, maxY - minY)
    End Function

    ''' <summary>
    ''' 获取直线与边界框的交点
    ''' </summary>
    Private Function GetLineBoundingBoxIntersections(line As LineWithSlope, box As RectangleF) As List(Of PointF)
        Dim intersections As New List(Of PointF)()

        ' 检查与四条边的交点
        Dim edges As New List(Of LineSegment) From {
            New LineSegment(New PointF(box.Left, box.Top), New PointF(box.Right, box.Top)),    ' 上边
            New LineSegment(New PointF(box.Right, box.Top), New PointF(box.Right, box.Bottom)), ' 右边
            New LineSegment(New PointF(box.Right, box.Bottom), New PointF(box.Left, box.Bottom)), ' 下边
            New LineSegment(New PointF(box.Left, box.Bottom), New PointF(box.Left, box.Top))    ' 左边
        }

        For Each edge In edges
            Dim intersection As PointF? = GetLineSegmentIntersection(line, edge)
            If intersection.HasValue Then
                intersections.Add(intersection.Value)
            End If
        Next

        Return intersections
    End Function

    ''' <summary>
    ''' 计算直线与线段的交点
    ''' </summary>
    Private Function GetLineSegmentIntersection(line As LineWithSlope, segment As LineSegment) As PointF?
        ' 实现直线与线段求交算法
        ' 这里需要解线性方程组
        Dim x As Double
        Dim y As Double

        If Double.IsInfinity(line.Slope) Then
            ' 处理垂直线情况
            x = line.Intercept
            Dim segMinY As Single = std.Min(segment.StartPoint.Y, segment.EndPoint.Y)
            Dim segMaxY As Single = std.Max(segment.StartPoint.Y, segment.EndPoint.Y)
            y = line.Slope * x + line.Intercept ' 这里需要调整

            If y >= segMinY AndAlso y <= segMaxY Then
                Return New PointF(CSng(x), CSng(y))
            End If
        Else
            ' 一般情况下的交点计算
            ' 简化实现，实际使用时需要更严谨的数学计算
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' 辅助类：表示带斜率的直线
    ''' </summary>
    Public Class LineWithSlope
        Public Property Slope As Double
        Public Property Intercept As Double

        Public Sub New(slope As Double, intercept As Double)
            Me.Slope = slope
            Me.Intercept = intercept
        End Sub
    End Class

    ''' <summary>
    ''' 辅助类：表示线段
    ''' </summary>
    Public Class LineSegment
        Public Property StartPoint As PointF
        Public Property EndPoint As PointF

        Public Sub New(startPoint As PointF, endPoint As PointF)
            Me.StartPoint = startPoint
            Me.EndPoint = endPoint
        End Sub
    End Class
End Module
