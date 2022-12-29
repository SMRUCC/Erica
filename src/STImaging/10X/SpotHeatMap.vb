Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Data.GraphTheory
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap

Public Class SpotHeatMap : Inherits HeatMapPlot

    ReadOnly spots As PixelData()
    ReadOnly dimension_size As Size
    ReadOnly spotSize As SizeF

    Public Sub New(layer As PixelData(), dimension_size As Size, theme As Theme)
        MyBase.New(theme)

        Me.spots = layer
        Me.dimension_size = dimension_size
        Me.spotSize = Render.SpotDiff(layer.Select(Function(p) New Point(p.X, p.Y)))
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim ncellsWidth As Integer = dimension_size.Width / spotSize.Width
        Dim ncellsHeight As Integer = dimension_size.Height / spotSize.Height
        Dim rect As Rectangle = canvas.PlotRegion
        Dim physicalCellWidth As Double = rect.Width / ncellsWidth
        Dim physicalCellHeight As Double = rect.Height / ncellsHeight
        Dim cells = LoadCells(rect.Size, physicalCellWidth, physicalCellHeight)

    End Sub

    Private Function LoadCells(rect As Size,
                               physicalCellWidth As Double,
                               physicalCellHeight As Double) As Grid(Of PixelData)

        Dim heatmap As Grid(Of PixelData) = Grid(Of PixelData).Blank(
            dims:=rect,
            blankSpot:=Function(x, y) New PixelData(x, y),
            steps:=New SizeF(physicalCellWidth, physicalCellHeight),
            getSpot:=Function(i) New Point(i.X, i.Y)
        )

        Return heatmap
    End Function
End Class
