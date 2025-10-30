Imports System.Drawing
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Public Class IHCScanner

    ReadOnly antibody As NamedCollection(Of Double)()
    ReadOnly A As Double(,)

    Public ReadOnly Property Antibodies As String()
        Get
            Return antibody.Select(Function(a) a.name).ToArray
        End Get
    End Property

    Sub New(antibody As Dictionary(Of String, Color))
        Me.antibody = antibody _
            .Select(Function(a)
                        Return New NamedCollection(Of Double)(a.Key, New Double() {a.Value.R / 255, a.Value.G / 255, a.Value.B / 255})
                    End Function) _
            .ToArray
        Me.A = New Double(2, antibody.Count - 1) {}

        For i As Integer = 0 To antibody.Count - 1
            Dim vec As Double() = Me.antibody(i).value

            A(0, i) = vec(0)
            A(1, i) = vec(1)
            A(2, i) = vec(2)
        Next
    End Sub

    Public Function UnmixPixel(pixel As Color) As Double()
        Return IHCUnmixing.UnmixPixel(pixel, A)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="dzi"></param>
    ''' <param name="level"></param>
    ''' <param name="dir"></param>
    ''' <param name="skipBlank">
    ''' skip of the blank tile image for make exports? tile image with all pixels is black or all pixels is white will be treated as blank tile image. 
    ''' </param>
    ''' <returns></returns>
    Public Function UnmixDziImage(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment, Optional skipBlank As Boolean = True) As Dictionary(Of String, DziImageBuffer())
        Dim imagefiles As DziImageBuffer() = DziImageBuffer.LoadBuffer(dzi, level, dir, skipBlank:=True).ToArray
        Dim layers As New Dictionary(Of String, DziImageBuffer())
        Dim N As Integer = Me.antibody.Length
        Dim grayscale As BitmapBuffer

        For Each antibody As NamedCollection(Of Double) In Me.antibody
            Call layers.Add(antibody.name, New DziImageBuffer(imagefiles.Length - 1) {})
        Next

        Call $"split ICH({antibody.Keys.JoinBy(", ")}) antibody channels...".info

        For Each i As Integer In TqdmWrapper.Range(0, imagefiles.Length, wrap_console:=App.EnableTqdm)
            Dim image As DziImageBuffer = imagefiles(i)
            Dim splits = IHCUnmixing.Unmix(image.bitmap, A, N, flip:=True)

            For offset As Integer = 0 To N - 1
                grayscale = splits(offset)

                If skipBlank Then
                    Dim allPixels As UInteger() = grayscale.GetARGBStream

                    If allPixels.All(Function(b) b = BitmapBuffer.UInt32White) OrElse
                        allPixels.All(Function(b) b = BitmapBuffer.UInt32Black2) OrElse
                        allPixels.All(Function(b) b = BitmapBuffer.UInt32Black1) Then

                        Continue For
                    End If
                End If

                layers(antibody(offset).name)(i) = New DziImageBuffer(image.tile, image.xy, grayscale)
            Next
        Next

        Erase imagefiles

        Return layers
    End Function

    Public Iterator Function ScanCells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                       Optional ostu_factor As Double = 0.7,
                                       Optional noise As Double = 0.25,
                                       Optional moran_knn As Integer = 32,
                                       Optional splitBlocks As Boolean = True) As IEnumerable(Of IHCCellScan)

        Dim layers As Dictionary(Of String, DziImageBuffer()) = UnmixDziImage(dzi, level, dir)

        For Each antibody As NamedCollection(Of Double) In Me.antibody
            Call $"processing the antibody layer: {antibody.name}".debug

            Dim scaled = DziImageBuffer.GlobalScales(layers(antibody.name))
            Dim tiles = layers(antibody.name).ToDictionary(Function(a) a.xy.JoinBy("_"), Function(a) a.bitmap)
            Dim cells As CellScan() = scaled.ScanBuffer(
                ostu_factor:=ostu_factor,
                flip:=False,
                splitBlocks:=splitBlocks,
                noise:=noise,
                moran_knn:=moran_knn).ToArray

            For Each cell As CellScan In cells
                Dim tile As BitmapBuffer = tiles(cell.tile_id)
                Dim weights = cell.scan_x _
                    .Select(Function(xi, i) tile.GetRed(CInt(xi), CInt(cell.scan_y(i))) / 255) _
                    .ToArray

                Yield New IHCCellScan With {
                    .antibody = antibody.name,
                    .area = cell.area,
                    .average_dist = cell.average_dist,
                    .density = cell.density,
                    .r2 = cell.r2,
                    .moranI = cell.moranI,
                    .physical_x = cell.physical_x,
                    .physical_y = cell.physical_y,
                    .points = cell.points,
                    .pvalue = cell.pvalue,
                    .ratio = cell.ratio,
                    .scan_x = cell.scan_x,
                    .scan_y = cell.scan_y,
                    .weight = If(weights.Length = 0, 0, weights.Average),
                    .r1 = cell.r1,
                    .x = cell.x,
                    .y = cell.y,
                    .tile_id = cell.tile_id
                }
            Next
        Next
    End Function

End Class
