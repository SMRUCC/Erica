Imports HEView
Imports Microsoft.VisualBasic.Data.Framework
Imports Microsoft.VisualBasic.Drawing

Module Program

    Sub New()
        Call SkiaDriver.Register()
    End Sub

    Sub Main(args As String())
        Dim bin = SkiaImage.FromFile("Z:\aaa.bmp")
        Dim cells = CellScan.CellLookups(bin.ToBitmap.MemoryBuffer, binary_processing:=False).MoranI(knn:=16).ToArray
        Dim result = cells.Tabular

        Call result.WriteCsv("Z:/cells.csv")

        Pause()
    End Sub
End Module
