Imports HEView
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.Framework
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Pointf = System.Drawing.PointF

Module Program

    Sub New()
        Call SkiaDriver.Register()
    End Sub

    Sub Main(args As String())
        Dim bin = SkiaImage.FromFile("Z:\aaa.bmp")
        Dim cells = CellScan.CellLookups(bin.ToBitmap.MemoryBuffer, binary_processing:=False).Split.MoranI(knn:=16).ToArray
        Dim result = cells.Tabular

        Call result.WriteCsv("Z:/cells.csv")

        Dim densityRange As New DoubleRange(cells.Select(Function(c) c.moranI))
        Dim colors As SolidBrush() = Designer.GetBrushes("jet", 30)
        Dim index As New DoubleRange(0, colors.Length - 1)

        Using gfx As New Graphics(bin.Size)
            For Each cell As CellScan In TqdmWrapper.Wrap(cells)
                Dim shape As Pointf() = cell.GetShape
                Dim i As Integer = densityRange.ScaleMapping(cell.moranI, index)

                Call gfx.FillPolygon(colors(i), shape)
            Next

            Call gfx.ImageResource.SaveAs("Z:/cells.png")
        End Using

        ' Pause()
    End Sub
End Module
