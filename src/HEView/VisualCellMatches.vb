Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Axis
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports Microsoft.VisualBasic.MIME.Html.Render

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
        Dim xTicks = slide1.JoinIterates(slide2).Select(Function(a) a.physical_x).CreateAxisTicks
        Dim yTicks = slide1.JoinIterates(slide2).Select(Function(a) a.physical_y).CreateAxisTicks
        Dim css As CSSEnvirnment = g.LoadEnvironment
        Dim x = d3js.scale.linear.domain(values:=xTicks).range(values:=canvas.GetXLinearScaleRange(css))
        Dim y = d3js.scale.linear.domain(values:=yTicks).range(values:=canvas.GetYLinearScaleRange(css))
        Dim scaler As New DataScaler(rev:=True) With {
            .AxisTicks = (xTicks.AsVector, yTicks.AsVector),
            .region = canvas.PlotRegion(css),
            .X = x,
            .Y = y
        }

        If theme.drawAxis Then
            Call Axis.DrawAxis(g, canvas, scaler, xlabel, ylabel, theme)
        End If
    End Sub
End Class
