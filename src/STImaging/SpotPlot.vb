Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Imaging.Math2D

Public Class SpotPlot : Inherits Plot

    ReadOnly spots As PixelData()
    ReadOnly dimension_size As Size

    Public Property mapLevels As Integer = 16

    Public Sub New(layer As PixelData(), dimension_size As Size, theme As Theme)
        MyBase.New(theme)

        Me.spots = layer
        Me.dimension_size = dimension_size
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim size As New SizeF(theme.pointSize, theme.pointSize)
        Dim image As Graphics2D = dimension_size.Scale(theme.pointSize).CreateGDIDevice(filled:=Color.Transparent)
        Dim expr0 As New DoubleRange(From spot As PixelData In spots Select spot.Scale)
        Dim index As New DoubleRange(0, mapLevels - 1)
        Dim colors As SolidBrush() = Designer.GetBrushes(theme.colorSet, mapLevels)

        For Each spot As PixelData In spots
            Dim pos As New PointF(spot.X * theme.pointSize, spot.Y * theme.pointSize)
            Dim rect As New RectangleF(pos, size)
            Dim i As Integer = expr0.ScaleMapping(spot.Scale, index)
            Dim color As Brush = colors(i)

            Call image.FillEllipse(color, rect)
        Next

        Call image.Flush()
        Call g.DrawImage(image.ImageResource, canvas.PlotRegion)
    End Sub
End Class