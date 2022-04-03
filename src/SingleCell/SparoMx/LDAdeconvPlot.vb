Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve

Public Class LDAdeconvPlot : Inherits Plot

    ReadOnly lda As Deconvolve

    Public Sub New(lda As Deconvolve, theme As Theme)
        MyBase.New(theme)
        Me.lda = lda
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)

    End Sub
End Class
