Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.MarchingSquares
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports Erica.Analysis.SpatialTissue.RaidData


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

Public Class Render

    Dim matrix As MatrixViewer
    Dim pixels As Point()
    Dim colorMap As String = ScalerPalette.Jet.Description

    Public ReadOnly Property geneIDs As String()
        Get
            Return matrix.FeatureIDs.ToArray
        End Get
    End Property

    Public ReadOnly Property dimension As Size

    Sub New(anndat As AnnData, Optional colorMaps As ScalerPalette = ScalerPalette.turbo)
        Dim matrix = anndat.ExportExpression.T

        Me.colorMap = colorMaps
        Me.matrix = New HTSMatrixViewer(matrix)
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

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="matrix">
    ''' sample id should be the barcodes
    ''' </param>
    ''' <param name="spots"></param>
    ''' <param name="colorMaps"></param>
    Sub New(matrix As MatrixViewer, spots As SpatialSpot(), Optional colorMaps As ScalerPalette = ScalerPalette.turbo)
        Dim spotIndex = spots.ToDictionary(Function(a) a.barcode)

        Me.matrix = matrix
        Me.pixels = matrix.SampleIDs _
            .Select(Function(barcode) spotIndex(barcode).GetSpotPoint) _
            .ToArray
        Me.colorMap = colorMaps
        Me.dimension = New Size With {
            .Width = pixels.Select(Function(i) i.X).Max,
            .Height = pixels.Select(Function(i) i.Y).Max
        }
    End Sub

    Public Shared Function SpotDiff(ByRef pixels As Point(), Optional top As Integer = 10) As SizeF
        Dim offsetX = pixels.Select(Function(i) i.X).Min
        Dim offsetY = pixels.Select(Function(i) i.Y).Min
        Dim pixelList = pixels
        Dim diffFormula = Function(dimVal As Func(Of Point, Double))
                              Return pixelList _
                                 .OrderBy(Function(i) dimVal(i)) _
                                 .Select(Function(i) dimVal(i)) _
                                 .ToArray _
                                 .DoCall(AddressOf NumberGroups.diff) _
                                 .OrderByDescending(Function(d) d) _
                                 .Take(top) _
                                 .Average
                          End Function

        pixels = pixels _
            .Select(Function(i) New Point(i.X - offsetX, i.Y - offsetY)) _
            .ToArray

        Dim diffX = diffFormula(Function(p) p.X)
        Dim diffY = diffFormula(Function(p) p.Y)

        Return New SizeF(diffX, diffY)
    End Function

    Public Shared Function ScaleSpots(pixels As Point()) As Point()
        Dim diff = SpotDiff(pixels)

        pixels = pixels _
            .Select(Function(i) New Point(CInt(i.X / diff.Width), CInt(i.Y / diff.Height))) _
            .ToArray

        Return pixels
    End Function

    ''' <summary>
    ''' get the raw data of a specific gene layer
    ''' </summary>
    ''' <param name="geneId"></param>
    ''' <returns></returns>
    Public Iterator Function GetLayer(geneId As String) As IEnumerable(Of PixelData)
        Dim vec As Double() = matrix.GetGeneExpression(geneId)

        For i As Integer = 0 To pixels.Length - 1
            Yield New PixelData(pixels(i), vec(i))
        Next
    End Function

    ''' <summary>
    ''' imaging in contour heatmap
    ''' </summary>
    ''' <param name="geneId"></param>
    ''' <returns></returns>
    Public Function Imaging(geneId As String) As Bitmap
        Dim layer As PixelData() = GetLayer(geneId).ToArray
        Dim render As New PixelRender(colorMap, 20, defaultColor:=Color.Black)
        Dim sample As MeasureData() = layer.Select(Function(i) New MeasureData(i)).ToArray
        Dim contours As GeneralPath() = ContourLayer _
            .GetContours(
                sample:=sample,
                interpolateFill:=False,
                levels:={0.01, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9, 0.925, 0.95, 0.975, 1}
            ) _
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

            Call Console.WriteLine(layerMap.ToString)

            For Each poly In polygons
                Dim rect As New Rectangle(
                    x:=poly.xpoints.Min,
                    y:=poly.ypoints.Max,
                    width:=poly.xpoints.Max - poly.xpoints.Min,
                    height:=poly.ypoints.Max - poly.ypoints.Min
                )

                For x As Integer = rect.Left To rect.Right
                    For y As Integer = rect.Top To rect.Bottom
                        pixels.Add(New PixelData(x, y, scale))
                    Next
                Next
            Next
        Next

        layer = pixels _
            .GroupBy(Function(i) $"{i.X},{i.Y}") _
            .Select(Function(i)
                        Return i.OrderByDescending(Function(p) p.Scale).First
                    End Function) _
            .ToArray

#Disable Warning
        Dim img As Bitmap

        If layer.Length = 0 Then
            img = New Bitmap(dimension.Width, dimension.Height)
        Else
            img = render.RenderRasterImage(layer, Me.dimension)
        End If

        Dim canvas = DriverLoad.CreateGraphicsDevice(New Size(img.Width * 5, img.Height * 5), Color.Black)

        Call canvas.DrawImage(img, 0, 0, canvas.Width, canvas.Height)
        Call canvas.Flush()

        img = DirectCast(canvas, GdiRasterGraphics).ImageResource

        For level As Integer = 0 To 10
            img = GaussBlur.GaussBlur(img)
        Next
#Enable Warning

        Return img
    End Function

End Class
