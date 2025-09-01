Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Serialization.JSON

Public Module DziScanner

    <Extension>
    Public Function ScanCells(dzi As DziImage, level As Integer, dir As String,
                              Optional ostu_factor As Double = 0.7,
                              Optional noise As Double = 0.25,
                              Optional moran_knn As Integer = 32) As IEnumerable(Of CellScan)

        Dim bar As Tqdm.ProgressBar = Nothing
        Dim globalLookups As New List(Of CellScan)

        For Each file As String In Tqdm.Wrap(dir.ListFiles("*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray, bar:=bar)
            Dim image As Image = Image.FromFile(file)
            Dim bitmap As BitmapBuffer = image.GetMemoryBitmap
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))

            Call bar.SetLabel($"global lookups of tile {xy.GetJson} -> (offset:{tile.Left},{tile.Top}, width:{tile.Width} x height:{tile.Height})")
            Call globalLookups.AddRange(CellScan _
                    .CellLookups(grid:=Thresholding.ostuFilter(bitmap, flip:=False, ostu_factor, verbose:=False),
                                 binary_processing:=False,
                                 offset:=tile.Location))
        Next

        Return globalLookups.Split(noise).MoranI(knn:=moran_knn)
    End Function
End Module
