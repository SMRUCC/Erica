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
    Public Iterator Function ScanCells(dzi As DziImage, level As Integer, dir As String,
                                       Optional ostu_factor As Double = 0.7,
                                       Optional noise As Double = 0.25,
                                       Optional moran_knn As Integer = 32) As IEnumerable(Of CellScan)

        Dim bar As Tqdm.ProgressBar = Nothing

        For Each file As String In Tqdm.Wrap(dir.ListFiles("*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray, bar:=bar)
            Dim image As Image = Image.FromFile(file)
            Dim bitmap As BitmapBuffer = image.GetMemoryBitmap
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))

            Call bar.SetLabel($"{xy.GetJson} -> (offset:{tile.Left},{tile.Top},  width:{tile.Width} x height:{tile.Height})")

            For Each cell As CellScan In bitmap.ScanTile(tile.Location, ostu_factor, noise, moran_knn)
                Yield cell
            Next
        Next
    End Function

    <Extension>
    Private Function ScanTile(data As BitmapBuffer, offset As Point,
                              Optional ostu_factor As Double = 0.7,
                              Optional noise As Double = 0.25,
                              Optional moran_knn As Integer = 32) As IEnumerable(Of CellScan)

        Dim cells = CellScan _
            .CellLookups(grid:=Thresholding.ostuFilter(data, flip:=False, ostu_factor, verbose:=False),
                         binary_processing:=False,
                         offset:=offset) _
            .Split(noise) _
            .MoranI(knn:=moran_knn) _
            .ToArray

        Return cells
    End Function
End Module
