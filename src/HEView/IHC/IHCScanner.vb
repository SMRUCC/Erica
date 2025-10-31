Imports System.Drawing
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.CommandLine.InteropService.Pipeline
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Serialization.JSON

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

    Public Function ScanCells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                              Optional ostu_factor As Double = 0.7,
                              Optional noise As Double = 0.25,
                              Optional moran_knn As Integer = 32,
                              Optional splitBlocks As Boolean = True) As IEnumerable(Of IHCCellScan)

        Dim imagefiles As DziImageBuffer() = DziImageBuffer.LoadBuffer(dzi, level, dir, skipBlank:=True).ToArray
        Dim bar As Tqdm.ProgressBar = Nothing
        Dim globalLookups As New List(Of IHCCellScan)
        Dim wrap_tqdm As Boolean = App.EnableTqdm
        Dim d As Integer = imagefiles.Length / 25
        Dim offset As i32 = 0
        Dim threshold As Integer = ostu_factor * DziScanner.globalThreshold(imagefiles)

        If d = 0 Then
            d = 1
        End If

        Call $"global threshold for ostu filter is {threshold}.".debug

        For Each file As DziImageBuffer In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim bitmap As BitmapBuffer = file.grayscale
            Dim xy As Integer() = file.xy
            Dim tile As Rectangle = file.tile
            Dim tip As String = $"{xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height}) found {globalLookups.Count} single cells"
            Dim lookups = CellScan _
                .CellLookups(grayscale:=bitmap, offset:=tile.Location, verbose:=False) _
                .ToArray
            Dim tile_id As String = xy.JoinBy("_")

            ' for each cell
            For j As Integer = 0 To lookups.Length - 1
                Dim cell As IHCCellScan = lookups(j).Clone(New IHCCellScan)
                Dim y As Integer() = cell.scan_y.AsInteger

                cell.antibody = New Dictionary(Of String, Double)
                cell.tile_id = tile_id

                lookups(j) = cell

                Dim pixels As Color() = cell.scan_x.Select(Function(xi, i) file.bitmap.GetPixel(xi, y(i))).ToArray
                Dim layers As Double()() = pixels.Select(Function(pixel) UnmixPixel(pixel)).ToArray
                Dim antibodySet = Me.antibody _
                    .Select(Function(a, i)
                                Return New NamedCollection(Of Double)(a.name, From pixel As Double() In layers Select pixel(i))
                            End Function) _
                    .ToArray

                For Each antibody As NamedCollection(Of Double) In antibodySet
                    cell.antibody(antibody.name) = antibody.Average
                Next
            Next

            Call globalLookups.AddRange(From cell As CellScan
                                        In lookups
                                        Select DirectCast(cell, IHCCellScan))
            Call bitmap.Dispose()

            If Not wrap_tqdm Then
                If ++offset Mod d = 0 Then
                    Call RunSlavePipeline.SendProgress(100 * CInt(offset) / imagefiles.Length, tip)
                End If
            Else
                Call bar.SetLabel(tip)
            End If
        Next

        If splitBlocks Then
            Return globalLookups _
                .FilterNoise(noise) _
                .Split() _
                .MoranI(knn:=moran_knn)
        Else
            Return globalLookups _
                .FilterNoise(noise) _
                .MoranI(knn:=moran_knn)
        End If
    End Function

End Class
