Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.CommandLine.InteropService.Pipeline
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Serialization.JSON

Public Module DziScanner

    Private Function globalThreshold(dir As IFileSystemEnvironment, imagefiles As String()) As Integer
        Dim bits As New BucketSet(Of UInteger)
        Dim bar As Tqdm.ProgressBar = Nothing
        Dim wrap_tqdm As Boolean = App.EnableTqdm

        For Each file As String In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim image As Image = Image.FromStream(dir.OpenFile(file, IO.FileMode.Open, IO.FileAccess.Read))
            Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)

            Call bitmap.Dispose()
        Next

        Return otsuThreshold(bits)
    End Function

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
                              Optional splitBlocks As Boolean = True) As IEnumerable(Of CellScan)

        Dim bar As Tqdm.ProgressBar = Nothing
        Dim globalLookups As New List(Of CellScan)
        Dim wrap_tqdm As Boolean = App.EnableTqdm
        Dim imagefiles As String() = dir.EnumerateFiles("/", "*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray
        Dim d As Integer = imagefiles.Length / 25
        Dim offset As i32 = 0

        If d = 0 Then
            d = 1
        End If

        For Each file As String In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim image As Image = Image.FromStream(dir.OpenFile(file, IO.FileMode.Open, IO.FileAccess.Read))
            Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))
            Dim tip As String = $"global lookups of tile {xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height})"

            Call globalLookups.AddRange(CellScan _
                    .CellLookups(grid:=Thresholding.ostuFilter(bitmap, flip:=False, ostu_factor, verbose:=False),
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
