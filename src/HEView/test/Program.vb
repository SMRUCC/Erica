Imports System
Imports HEView
Imports Microsoft.VisualBasic.Imaging

Module Program
    Sub Main(args As String())
        Dim cells = CellScan.CellLookups(New Bitmap("Z:\aaa.bmp").MemoryBuffer).ToArray

        Pause()
    End Sub
End Module
