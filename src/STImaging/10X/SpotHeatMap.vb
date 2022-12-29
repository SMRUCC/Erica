Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap

Public Class SpotHeatMap : Inherits HeatMapPlot

    ReadOnly spots As PixelData()
    ReadOnly dimension_size As Size

    Public Sub New(layer As PixelData(), dimension_size As Size, theme As Theme)
        MyBase.New(theme)

        Me.spots = layer
        Me.dimension_size = dimension_size
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)

    End Sub
End Class
