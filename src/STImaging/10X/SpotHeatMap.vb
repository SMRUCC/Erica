Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Data.GraphTheory
Imports Microsoft.VisualBasic.Data.GraphTheory.GridGraph
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap

Public Class SpotHeatMap : Inherits HeatMapPlot

    ReadOnly spots As PixelData()
    ReadOnly dimension_size As Size
    ReadOnly spotSize As SizeF

    Private Class SpotCell : Implements Pixel

        Public Property X As Integer Implements Pixel.X
        Public Property Y As Integer Implements Pixel.Y
        Public Property Scale As Double Implements Pixel.Scale

    End Class

    Public Sub New(layer As PixelData(), dimension_size As Size, theme As Theme)
        MyBase.New(theme)

        Me.dimension_size = dimension_size
        Me.spots = layer
        Me.spotSize = Render.SpotDiff(layer.Select(Function(p) New Point(p.X, p.Y)).ToArray)
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim ncellsWidth As Integer = dimension_size.Width / spotSize.Width
        Dim ncellsHeight As Integer = dimension_size.Height / spotSize.Height
        Dim rect As Rectangle = canvas.PlotRegion
        Dim physicalCellWidth As Double = rect.Width / ncellsWidth
        Dim physicalCellHeight As Double = rect.Height / ncellsHeight
        Dim physicalCell As New Size(physicalCellWidth, physicalCellHeight)
        Dim cells = LoadCells(rect, physicalCellWidth, physicalCellHeight)
        Dim raster = cells.EnumerateData.ToArray

        ' rendering the heatmap cells
        ' Call g.DrawRectangle(Pens.Red, canvas.PlotRegion)
        Call PixelRender.FillRectangles(
            g:=g,
            raster:=raster,
            colors:=GetColors,
            defaultColor:=theme.gridFill.TranslateColor,
            cw:=physicalCellWidth,
            ch:=physicalCellHeight
        )
    End Sub

    Private Function LoadCells(rect As Rectangle,
                               physicalCellWidth As Double,
                               physicalCellHeight As Double) As Grid(Of SpotCell)

        Dim physicalCell As New Size(physicalCellWidth, physicalCellHeight)
        Dim heatmap As Grid(Of SpotCell) = Grid(Of SpotCell).Blank(
            dims:=rect.Size,
            blankSpot:=Function(x, y) New SpotCell With {.X = x, .Y = y},
            steps:=New SizeF(physicalCellWidth, physicalCellHeight),
            getSpot:=Function(i) New Point(i.X, i.Y)
        )
        Dim scaleX = d3js.scale _
            .linear() _
            .domain(values:=New Integer() {0, dimension_size.Width}) _
            .range(integers:={rect.Left, rect.Right})
        Dim scaleY = d3js.scale _
            .linear() _
            .domain(values:=New Integer() {0, dimension_size.Height}) _
            .range(integers:={rect.Top, rect.Bottom})
        Dim raster = New HeatMapRaster(Of PixelData)() _
            .SetDatas(spots) _
            .GetRasterPixels _
            .Where(Function(p) p.Scale > 0) _
            .ToArray

        For Each spot As Pixel In raster
            Dim xi As Double = scaleX(spot.X)
            Dim yi As Double = scaleY(spot.Y)
            Dim pixels As SpotCell() = heatmap _
                .Query(xi, yi, gridSize:=physicalCell) _
                .ToArray

            For Each cell As SpotCell In pixels
                cell.Scale += spot.Scale
            Next
        Next

        Return heatmap
    End Function
End Class
