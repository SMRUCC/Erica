Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D

Public Class VisualCellMatches : Inherits Plot

    ReadOnly matches As CellMatchResult()
    ReadOnly slide1 As CellScan()
    ReadOnly slide2 As CellScan()

    Public Sub New(matches As IEnumerable(Of CellMatchResult), slide1 As CellScan(), slide2 As CellScan(), theme As Theme)
        MyBase.New(theme)

        Me.matches = matches.ToArray
        Me.slide1 = slide1
        Me.slide2 = slide2
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)

    End Sub
End Class
