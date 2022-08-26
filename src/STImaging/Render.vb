Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.MarchingSquares
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports STRaid

Public Class Render

    Dim matrix As Dictionary(Of String, DataFrameRow)
    Dim pixels As Point()
    Dim dimension As Size

    Public ReadOnly Property geneIDs As String()
        Get
            Return matrix.Keys.ToArray
        End Get
    End Property

    Sub New(anndat As AnnData)
        Dim matrix = anndat.ExportExpression.T

        Me.matrix = matrix.expression.ToDictionary(Function(a) a.geneID)
        Me.pixels = matrix.sampleID _
            .Select(Function(str)
                        Dim t As Integer() = str _
                            .Split(","c) _
                            .Select(AddressOf Integer.Parse) _
                            .ToArray
                        Dim p As New Point(t(0), t(1))

                        Return p
                    End Function) _
            .ToArray _
            .DoCall(AddressOf ScaleSpots)
        Me.dimension = New Size(
            width:=pixels.Select(Function(i) i.X).Max,
            height:=pixels.Select(Function(i) i.Y).Max
        )
    End Sub

    Private Shared Function ScaleSpots(pixels As Point()) As Point()
        Dim offsetX = pixels.Select(Function(i) i.X).Min
        Dim offsetY = pixels.Select(Function(i) i.Y).Min

        pixels = pixels _
            .Select(Function(i) New Point(i.X - offsetX, i.Y - offsetY)) _
            .ToArray

        Dim diffX = NumberGroups.diff(pixels.OrderBy(Function(i) i.X).Select(Function(i) CDbl(i.X)).ToArray).Average
        Dim diffY = NumberGroups.diff(pixels.OrderBy(Function(i) i.Y).Select(Function(i) CDbl(i.Y)).ToArray).Average

        pixels = pixels _
            .Select(Function(i) New Point(CInt(i.X / diffX), CInt(i.Y / diffY))) _
            .ToArray

        Return pixels
    End Function

    Public Iterator Function GetLayer(geneId As String) As IEnumerable(Of PixelData)
        Dim vec As DataFrameRow = matrix(geneId)

        For i As Integer = 0 To pixels.Length - 1
            Yield New PixelData(pixels(i), vec.experiments(i))
        Next
    End Function

    Public Function Imaging(geneId As String) As Bitmap
        Dim layer As PixelData() = GetLayer(geneId).ToArray
        Dim render As New PixelRender("Jet", 120, defaultColor:=Color.Black)
        Dim sample As MeasureData() = layer.Select(Function(i) New MeasureData(i)).ToArray
        Dim contours As GeneralPath() = ContourLayer _
            .GetContours(sample, interpolateFill:=False) _
            .ToArray
        Dim pixels As New List(Of PixelData)

        For Each layerMap As GeneralPath In contours
            Dim scale As Double = layerMap.level
            Dim polygons = layerMap _
                .GetPolygons _
                .Select(Function(g)
                            Return New Microsoft.VisualBasic.Imaging.Math2D.Polygon2D(g)
                        End Function) _
                .ToArray

            For x As Integer = 0 To dimension.Width
                For y As Integer = 0 To dimension.Height
                    If polygons.Any(Function(g) g.inside(x, y)) Then
                        pixels.Add(New PixelData(x, y, scale))
                    End If
                Next
            Next
        Next

        layer = pixels _
            .GroupBy(Function(i) $"{i.X},{i.Y}") _
            .Select(Function(i)
                        Return i.OrderByDescending(Function(p) p.Scale).First
                    End Function) _
            .ToArray

        Dim img = render.RenderRasterImage(layer, Me.dimension)

        Return img
    End Function

End Class
