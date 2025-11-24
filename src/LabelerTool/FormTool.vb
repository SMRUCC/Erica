Imports System.IO
Imports HEView
Imports Microsoft.VisualBasic.Drawing

Public Class FormTool

    Dim allDataObjects As CellScan()
    Dim sizeOriginal As Size
    Dim polygonPoints As New List(Of Point)
    Dim renderedBitmap As Bitmap
    Dim isDrawing As Boolean = False
    Dim currentMousePos As Point

    Dim worldBounds As New RectangleF(0, 0, 20000, 15000)
    Dim bitmapSize As New Size(2000, 1500)

    Private Sub FormTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call SkiaDriver.Register()
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Using file As New OpenFileDialog With {.Filter = "Excel Table(*.csv)|*.csv"}
            If file.ShowDialog = DialogResult.OK Then
                allDataObjects = HEView.Data _
                    .TableReader(file.FileName.Open(FileMode.Open, doClear:=False, [readOnly]:=True)) _
                    .ToArray


            End If
        End Using
    End Sub

    Private Sub RenderDataToBitmap()

    End Sub
End Class
