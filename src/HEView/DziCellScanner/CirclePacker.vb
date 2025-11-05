Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.DelaunayVoronoi
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Math.Distributions
Imports std = System.Math

Public Class CirclePacker

    ReadOnly radius As DoubleRange
    ReadOnly polygon As PointF() ' 多边形顶点
    ReadOnly shape As Polygon2D

    Sub New(polygon As IEnumerable(Of PointF), r As DoubleRange)
        Me.polygon = polygon.ToArray
        Me.radius = r
        Me.shape = New Polygon2D(Me.polygon)
    End Sub

    Public Iterator Function PackCircles() As IEnumerable(Of Polygon2D)
        Dim rect As RectangleF = shape.GetRectangle
        Dim maxSize = std.Max(rect.Width, rect.Height)

        ' too small for generates the sub polygons
        If radius.Max / maxSize > 0.4 Then
            Yield shape
            Return
        End If

        Dim dist = PoissonDiskGenerator.Generate(radius.Max, maxSize)
        Dim voronoi As New Voronoi(dist, New Rectf(0, 0, maxSize, maxSize))
        Dim cells As Polygon2D() = voronoi.Regions.ToArray
        Dim offsetX = rect.X
        Dim offsetY = rect.Y

        For Each cell As Polygon2D In cells
            Dim center As New PointF(cell.xpoints.Average + offsetX, cell.ypoints.Average + offsetY)

            If shape.inside(center) Then
                Yield cell + New PointF(offsetX, offsetY)
            End If
        Next
    End Function
End Class