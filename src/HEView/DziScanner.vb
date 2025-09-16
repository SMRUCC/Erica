Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.CommandLine.InteropService.Pipeline
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Serialization.JSON

Public Module DziScanner

    <Extension>
    Public Function ScanCells(dzi As DziImage, level As Integer, dir As String,
                              Optional ostu_factor As Double = 0.7,
                              Optional noise As Double = 0.25,
                              Optional moran_knn As Integer = 32,
                              Optional splitBlocks As Boolean = True) As IEnumerable(Of CellScan)

        Dim bar As Tqdm.ProgressBar = Nothing
        Dim globalLookups As New List(Of CellScan)
        Dim wrap_tqdm As Boolean = App.EnableTqdm
        Dim imagefiles As String() = dir.ListFiles("*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray
        Dim d As Integer = imagefiles.Length / 25
        Dim offset As i32 = 0

        If d = 0 Then
            d = 1
        End If

        For Each file As String In Tqdm.Wrap(imagefiles, bar:=bar, wrap_console:=wrap_tqdm)
            Dim image As Image = Image.FromFile(file)
            Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))
            Dim tip As String = $"global lookups of tile {xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height})"

            Call globalLookups.AddRange(CellScan _
                    .CellLookups(grid:=Thresholding.ostuFilter(bitmap, flip:=False, ostu_factor, verbose:=False),
                                 binary_processing:=False,
                                 offset:=tile.Location))

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
