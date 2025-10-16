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

    Public Function globalIHC1Threshold(imagefiles As DziImageBuffer()) As (r As Integer, g As Integer, b As Integer)
        Dim bitsR As New BucketSet(Of UInteger)
        Dim bitsG As New BucketSet(Of UInteger)
        Dim bitsB As New BucketSet(Of UInteger)
        Dim bar As Tqdm.ProgressBar = Nothing
        Dim wrap_tqdm As Boolean = App.EnableTqdm

        For Each file As DziImageBuffer In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim bitmap As BitmapBuffer = file.bitmap
            Dim rgb = bitmap.RGB

            Call bitsR.Add(rgb.R.GetARGBStream)
            Call bitsG.Add(rgb.G.GetARGBStream)
            Call bitsB.Add(rgb.B.GetARGBStream)
            Call bitmap.Dispose()
        Next

        Return (otsuThreshold(bitsR), otsuThreshold(bitsG), otsuThreshold(bitsB))
    End Function

    ''' <summary>
    ''' scan with CMYK colors
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
    Public Function ScanIHC2Cells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                  Optional ostu_factor As Double = 0.7,
                                  Optional noise As Double = 0.25,
                                  Optional moran_knn As Integer = 32,
                                  Optional splitBlocks As Boolean = True) As (Lg6G As CellScan(), CiH3 As CellScan(), p16 As CellScan(), CD11b As CellScan(), PanCK As CellScan(), Dapi As CellScan())

        Dim imagefiles As DziImageBuffer() = DziImageBuffer.LoadBuffer(dzi, level, dir, skipBlank:=True).ToArray
        Dim Lg6G As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim CiH3 As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim p16 As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim CD11b As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim PanCK As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim Dapi As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim A As Double(,) = IHCUnmixing.GetReferenceMatrixIHC2

        Call "split ICH2 antibody channels...".info

        For Each i As Integer In TqdmWrapper.Range(0, imagefiles.Length, wrap_console:=App.EnableTqdm)
            Dim image As DziImageBuffer = imagefiles(i)
            Dim splits = IHCUnmixing.Unmix(image.bitmap, A, 6)

            Lg6G(i) = New DziImageBuffer(image.tile, image.xy, splits(0))
            CiH3(i) = New DziImageBuffer(image.tile, image.xy, splits(1))
            p16(i) = New DziImageBuffer(image.tile, image.xy, splits(2))
            CD11b(i) = New DziImageBuffer(image.tile, image.xy, splits(3))
            PanCK(i) = New DziImageBuffer(image.tile, image.xy, splits(4))
            Dapi(i) = New DziImageBuffer(image.tile, image.xy, splits(5))
        Next

        Erase imagefiles

        Lg6G = DziImageBuffer.GlobalScales(Lg6G)
        CiH3 = DziImageBuffer.GlobalScales(CiH3)
        p16 = DziImageBuffer.GlobalScales(p16)
        CD11b = DziImageBuffer.GlobalScales(CD11b)
        PanCK = DziImageBuffer.GlobalScales(PanCK)
        Dapi = DziImageBuffer.GlobalScales(Dapi)

        Call "scan cells with antibody Lg6G...".info
        Dim Lg6G_cells As CellScan() = Lg6G.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase Lg6G

        Call "scan cells with antibody CiH3...".info
        Dim CiH3_cells As CellScan() = CiH3.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase CiH3

        Call "scan cells with antibody p16...".info
        Dim p16_cells As CellScan() = p16.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase p16

        Call "scan cells with antibody CD11b...".info
        Dim CD11b_cells As CellScan() = CD11b.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase CD11b

        Call "scan cells with antibody PanCK...".info
        Dim PanCK_cells As CellScan() = PanCK.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase PanCK

        Call "scan cells with antibody Dapi...".info
        Dim Dapi_cells As CellScan() = Dapi.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase Dapi

        Return (Lg6G_cells, CiH3_cells, p16_cells, CD11b_cells, PanCK_cells, Dapi_cells)
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
    Public Function ScanIHC1Cells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                  Optional ostu_factor As Double = 0.7,
                                  Optional noise As Double = 0.25,
                                  Optional moran_knn As Integer = 32,
                                  Optional splitBlocks As Boolean = True) As (CD11b As CellScan(), CD11c As CellScan(), CD8 As CellScan(), PanCK As CellScan(), Dapi As CellScan())

        Dim imagefiles As DziImageBuffer() = DziImageBuffer.LoadBuffer(dzi, level, dir, skipBlank:=True).ToArray
        Dim CD11b As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim CD11c As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim CD8 As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim PanCK As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim Dapi As DziImageBuffer() = New DziImageBuffer(imagefiles.Length - 1) {}
        Dim A As Double(,) = IHCUnmixing.GetReferenceMatrixIHC1

        Call "split ICH1 antibody channels...".info

        For Each i As Integer In TqdmWrapper.Range(0, imagefiles.Length, wrap_console:=App.EnableTqdm)
            Dim image As DziImageBuffer = imagefiles(i)
            Dim splits = IHCUnmixing.Unmix(image.bitmap, A, 5)

            CD11b(i) = New DziImageBuffer(image.tile, image.xy, splits(0))
            CD11c(i) = New DziImageBuffer(image.tile, image.xy, splits(1))
            CD8(i) = New DziImageBuffer(image.tile, image.xy, splits(2))
            PanCK(i) = New DziImageBuffer(image.tile, image.xy, splits(3))
            Dapi(i) = New DziImageBuffer(image.tile, image.xy, splits(4))
        Next

        Erase imagefiles

        CD11b = DziImageBuffer.GlobalScales(CD11b)
        CD11c = DziImageBuffer.GlobalScales(CD11c)
        CD8 = DziImageBuffer.GlobalScales(CD8)
        PanCK = DziImageBuffer.GlobalScales(PanCK)
        Dapi = DziImageBuffer.GlobalScales(Dapi)

        Call "scan cells with antibody CD11b...".info
        Dim CD11b_cells As CellScan() = CD11b.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase CD11b

        Call "scan cells with antibody CD11c...".info
        Dim CD11c_cells As CellScan() = CD11c.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase CD11c

        Call "scan cells with antibody CD8...".info
        Dim CD8_cells As CellScan() = CD8.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase CD8

        Call "scan cells with antibody PanCK...".info
        Dim PanCK_cells As CellScan() = PanCK.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase PanCK

        Call "scan cells with antibody Dapi...".info
        Dim Dapi_cells As CellScan() = Dapi.ScanBuffer(ostu_factor:=ostu_factor, flip:=False, splitBlocks:=splitBlocks, noise:=noise, moran_knn:=moran_knn).ToArray

        Erase Dapi

        Return (CD11b_cells, CD11c_cells, CD8_cells, PanCK_cells, Dapi_cells)
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
            .ScanBuffer(ostu_factor:=ostu_factor,
                        flip:=flip,
                        splitBlocks:=splitBlocks,
                        noise:=noise,
                        moran_knn:=moran_knn
             )
    End Function

    <Extension>
    Private Function ScanBuffer(ByRef imagefiles As DziImageBuffer(),
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
            Dim tip As String = $"global lookups tile {xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height}) found {globalLookups.Count} single cells"

            Call globalLookups.AddRange(CellScan _
                    .CellLookups(grid:=Thresholding.ostuFilter(bitmap,
                                                               threshold:=threshold,
                                                               flip:=flip,
                                                               verbose:=False),
                                 binary_processing:=False,
                                 offset:=tile.Location))
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
