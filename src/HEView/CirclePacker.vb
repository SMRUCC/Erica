Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports std = System.Math

Public Class CirclePacker

    ReadOnly radius As Double
    ReadOnly polygon As PointF() ' 多边形顶点

    Sub New(polygon As IEnumerable(Of PointF), r As Double)
        Me.polygon = polygon.ToArray
        Me.radius = r
    End Sub

    Public Function PackCircles() As List(Of PointF)
        ' 1. 计算多边形边界框
        Dim minX = polygon.Min(Function(p) p.X)
        Dim maxX = polygon.Max(Function(p) p.X)
        Dim minY = polygon.Min(Function(p) p.Y)
        Dim maxY = polygon.Max(Function(p) p.Y)

        ' 2. 初始化网格参数
        Dim dx As Double = std.Sqrt(3) * radius ' 水平间距
        Dim dy As Double = 1.5 * radius          ' 垂直间距
        Dim circles As New List(Of PointF)()     ' 存储有效圆心

        ' 3. 遍历网格点
        Dim row As Integer = 0
        For y As Double = minY To maxY Step dy
            ' 奇数行偏移 dx/2
            Dim offsetX = If(row Mod 2 = 1, dx / 2, 0)

            For x As Double = minX + offsetX To maxX Step dx
                Dim candidate As New PointF(CSng(x), CSng(y))

                ' 4. 检查候选点有效性
                If IsPointInPolygon(candidate) AndAlso
                   DistanceToEdges(candidate) >= radius AndAlso
                   Not OverlapsExisting(candidate, circles, radius) Then
                    circles.Add(candidate)
                End If
            Next
            row += 1
        Next
        Return circles
    End Function

    '--- 辅助函数 ---
    ' 射线法判断点是否在多边形内
    Private Function IsPointInPolygon(point As PointF) As Boolean
        Dim inside As Boolean = False
        For i As Integer = 0 To polygon.Count - 1
            Dim j As Integer = (i + 1) Mod polygon.Count
            If (polygon(i).Y > point.Y) <> (polygon(j).Y > point.Y) AndAlso
               point.X < (polygon(j).X - polygon(i).X) * (point.Y - polygon(i).Y) /
                         (polygon(j).Y - polygon(i).Y) + polygon(i).X Then
                inside = Not inside
            End If
        Next
        Return inside
    End Function

    ' 计算点到多边形边的最短距离
    Private Function DistanceToEdges(point As PointF) As Double
        Dim minDist As Double = Double.MaxValue
        For i As Integer = 0 To polygon.Count - 1
            Dim j As Integer = (i + 1) Mod polygon.Count
            Dim dist = PointToLineDistance(point, polygon(i), polygon(j))
            If dist < minDist Then minDist = dist
        Next
        Return minDist
    End Function

    ' 点到线段的距离
    Private Function PointToLineDistance(p As PointF, a As PointF, b As PointF) As Double
        Dim lengthSquared As Double = (b.X - a.X) ^ 2 + (b.Y - a.Y) ^ 2
        If lengthSquared = 0 Then Return Distance(p, a) ' 线段退化为点

        ' 计算投影比例 t
        Dim t As Double = ((p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y)) / lengthSquared
        t = std.Max(0, std.Min(1, t)) ' 限制 t 在 [0,1] 内

        ' 计算投影点
        Dim projection As New PointF(
            CSng(a.X + t * (b.X - a.X)),
            CSng(a.Y + t * (b.Y - a.Y)))

        Return p.Distance(projection)
    End Function

    ' 两圆是否重叠（圆心距离 < 2r）
    Private Function OverlapsExisting(point As PointF, circles As List(Of PointF), r As Double) As Boolean
        Return circles.Any(Function(c) Distance(c, point) < 2 * r)
    End Function

End Class