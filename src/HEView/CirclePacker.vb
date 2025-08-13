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
        Dim dist = PoissonDiskGenerator.Generate((radius.Min + radius.Max) / 2, maxSize)
        Dim voronoi As New Voronoi(dist, New Rectf(0, 0, maxSize, maxSize))
        Dim cells = voronoi.Regions.ToArray

        For Each cell As Polygon2D In cells
            Dim center As New PointF(cell.xpoints.Average, cell.ypoints.Average)

            If shape.inside(center) Then
                Yield cell
            End If
        Next
    End Function
End Class