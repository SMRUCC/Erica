Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Scripting.Runtime

Public Module DziScanner

    <Extension>
    Public Iterator Function ScanCells(dzi As DziImage, level As Integer, dir As String,
                                       Optional ostu_factor As Double = 0.7,
                                       Optional noise As Double = 0.25,
                                       Optional moran_knn As Integer = 32) As IEnumerable(Of CellScan)

        For Each file As String In dir.ListFiles("*.jpg", "*.png", "*.jpeg", "*.bmp")
            Dim image As Image = Image.FromFile(file)
            Dim bitmap As BitmapBuffer = image.GetMemoryBitmap
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))

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
            .CellLookups(grid:=Thresholding.ostuFilter(data, flip:=False, ostu_factor),
                         binary_processing:=False,
                         offset:=offset) _
            .Split(noise) _
            .MoranI(knn:=moran_knn) _
            .ToArray

        Return cells
    End Function
End Module
