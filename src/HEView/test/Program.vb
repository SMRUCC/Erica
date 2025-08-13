Imports HEView
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module Program

    Sub New()
        Call SkiaDriver.Register()
    End Sub

    Sub Main(args As String())
        Dim bin = SkiaImage.FromFile("Z:\aaa.bmp")
        Dim cells = CellScan.CellLookups(bin.ToBitmap.MemoryBuffer, binary_processing:=False).ToArray

        Pause()
    End Sub
End Module
