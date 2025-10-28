Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.CommandLine.InteropService.Pipeline
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Serialization.JSON

Public Module DziScanner

    Private Function globalThreshold(imagefiles As DziImageBuffer()) As Integer
        Dim bits As New BucketSet(Of UInteger)
        Dim bar As Tqdm.ProgressBar = Nothing
        Dim wrap_tqdm As Boolean = App.EnableTqdm

        For Each file As DziImageBuffer In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Call bits.Add(file.bitmap.GetARGBStream)
        Next

        Return otsuThreshold(bits)
    End Function

    ''' <summary>
    ''' scan with RGB colors
    ''' </summary>
    ''' <param name="dzi"></param>
    ''' <param name="level"></param>
    ''' <param name="dir"></param>
    ''' <param name="ostu_factor"></param>
    ''' <param name="noise"></param>
    ''' <param name="moran_knn"></param>
    ''' <param name="splitBlocks"></param>
    ''' <returns></returns>
    <Extension>
    Public Function ScanIHCRGBCells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                    Optional ostu_factor As Double = 0.7,
                                    Optional noise As Double = 0.25,
                                    Optional moran_knn As Integer = 32,
                                    Optional splitBlocks As Boolean = True) As (r As CellScan(), g As CellScan(), b As CellScan())

        Dim imagefiles As DziImageBuffer() = DziImageBuffer.LoadBuffer(dzi, level, dir, skipBlank:=True).ToArray
        Dim r As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim g As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim b As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}

        Call "split ICH1 antibody channels...".info

        For Each i As Integer In TqdmWrapper.Range(0, imagefiles.Length, wrap_console:=App.EnableTqdm)
            Dim image As DziImageBuffer = imagefiles(i)
            Dim splits = image.bitmap.RGB(flip:=True)

            r(i) = New DziImageBuffer(image.tile, image.xy, splits.R)
            g(i) = New DziImageBuffer(image.tile, image.xy, splits.G)
            b(i) = New DziImageBuffer(image.tile, image.xy, splits.B)
        Next

        Erase imagefiles

        r = DziImageBuffer.GlobalScales(r)
        g = DziImageBuffer.GlobalScales(g)
        b = DziImageBuffer.GlobalScales(b)

        Call "scan cells with red channel...".info
        Dim r_cells As CellScan() = r.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase r

        Call "scan cells with green channel...".info
        Dim g_cells As CellScan() = g.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase g

        Call "scan cells with blue channel...".info
        Dim b_cells As CellScan() = b.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase b

        Return (r_cells, g_cells, b_cells)
    End Function

    <Extension>
    Public Sub DumpExport(images As IEnumerable(Of DziImageBuffer), outputdir As String)
        For Each img As DziImageBuffer In images
            Call img.bitmap.Save($"{outputdir}/{img.xy.JoinBy("_")}.bmp")
        Next
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="dzi"></param>
    ''' <param name="level"></param>
    ''' <param name="dir">A directory path that contains the image files in current <paramref name="level"/>.</param>
    ''' <param name="ostu_factor"></param>
    ''' <param name="noise"></param>
    ''' <param name="moran_knn"></param>
    ''' <param name="splitBlocks"></param>
    ''' <returns></returns>
    <Extension>
    Public Function ScanCells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                              Optional ostu_factor As Double = 0.7,
                              Optional noise As Double = 0.25,
                              Optional moran_knn As Integer = 32,
                              Optional splitBlocks As Boolean = True,
                              Optional flip As Boolean = False) As IEnumerable(Of CellScan)

        Return DziImageBuffer.LoadBuffer(dzi, level, dir) _
            .ToArray _
            .DoCall(AddressOf DziImageBuffer.GlobalScales) _
            .ScanBuffer(ostu_factor:=ostu_factor,
                        flip:=flip,
                        splitBlocks:=splitBlocks,
                        noise:=noise,
                        moran_knn:=moran_knn
             )
    End Function

    <Extension>
    Friend Function ScanBuffer(ByRef imagefiles As DziImageBuffer(),
                               ostu_factor As Double,
                               flip As Boolean,
                               splitBlocks As Integer,
                               noise As Double,
                               moran_knn As Integer) As IEnumerable(Of CellScan)

        Dim bar As Tqdm.ProgressBar = Nothing
        Dim globalLookups As New List(Of CellScan)
        Dim wrap_tqdm As Boolean = App.EnableTqdm
        Dim d As Integer = imagefiles.Length / 25
        Dim offset As i32 = 0
        Dim threshold As Integer = ostu_factor * globalThreshold(imagefiles)

        If d = 0 Then
            d = 1
        End If

        Call $"global threshold for ostu filter is {threshold}.".debug

        For Each file As DziImageBuffer In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim bitmap As BitmapBuffer = file.bitmap
            Dim xy As Integer() = file.xy
            Dim tile As Rectangle = file.tile
            Dim tip As String = $"{xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height}) found {globalLookups.Count} single cells"
            Dim lookups = CellScan _
                .CellLookups(grayscale:=Thresholding.ostuFilter(bitmap,
                                                                threshold:=threshold,
                                                                flip:=flip,
                                                                verbose:=False),
                             offset:=tile.Location,
                             verbose:=False) _
                .ToArray
            Dim tile_id As String = xy.JoinBy("_")

            For i As Integer = 0 To lookups.Length - 1
                lookups(i).tile_id = tile_id
            Next

            Call globalLookups.AddRange(lookups)
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
End Module
