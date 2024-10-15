Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports Microsoft.VisualBasic.MIME.Html.Render




#If NET48 Then
Imports Pen = System.Drawing.Pen
Imports Pens = System.Drawing.Pens
Imports Brush = System.Drawing.Brush
Imports Font = System.Drawing.Font
Imports Brushes = System.Drawing.Brushes
Imports SolidBrush = System.Drawing.SolidBrush
Imports DashStyle = System.Drawing.Drawing2D.DashStyle
Imports Image = System.Drawing.Image
Imports Bitmap = System.Drawing.Bitmap
Imports GraphicsPath = System.Drawing.Drawing2D.GraphicsPath
Imports FontStyle = System.Drawing.FontStyle
#Else
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports Pens = Microsoft.VisualBasic.Imaging.Pens
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports DashStyle = Microsoft.VisualBasic.Imaging.DashStyle
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports GraphicsPath = Microsoft.VisualBasic.Imaging.GraphicsPath
Imports FontStyle = Microsoft.VisualBasic.Imaging.FontStyle
#End If

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
        Dim image As IGraphics = DriverLoad.CreateGraphicsDevice(dimension_size.Scale(theme.pointSize), Color.Transparent)
        Dim expr0 As New DoubleRange(From spot As PixelData In spots Select spot.Scale)
        Dim index As New DoubleRange(0, mapLevels - 1)
        Dim colors As SolidBrush() = Designer.GetBrushes(theme.colorSet, mapLevels)
        Dim css As CSSEnvirnment = g.LoadEnvironment

        For Each spot As PixelData In spots
            Dim pos As New PointF(spot.X * theme.pointSize, spot.Y * theme.pointSize)
            Dim rect As New RectangleF(pos, size)
            Dim i As Integer = expr0.ScaleMapping(spot.Scale, index)
            Dim color As Brush = colors(i)

            Call image.FillEllipse(color, rect)
        Next

        Call image.Flush()
        Call g.DrawImage(DirectCast(image, GdiRasterGraphics).ImageResource, canvas.PlotRegion(css))
    End Sub
End Class