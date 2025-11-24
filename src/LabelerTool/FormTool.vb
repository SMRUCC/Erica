Imports System.IO
Imports HEView
Imports Microsoft.VisualBasic.Drawing

Public Class FormTool

    Dim originalData As CellScan()
    Dim sizeOriginal As Size
    Dim polygonPointsScreen As New List(Of Point)
    Dim displayBitmap As Bitmap
    Dim isDrawing As Boolean = False
    Dim currentMousePos As Point

    Private Sub FormTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call SkiaDriver.Register()
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Using file As New OpenFileDialog With {.Filter = "Excel Table(*.csv)|*.csv"}
            If file.ShowDialog = DialogResult.OK Then
                originalData = HEView.Data _
                    .TableReader(file.FileName.Open(FileMode.Open, doClear:=False, [readOnly]:=True)) _
                    .ToArray
            End If
        End Using
    End Sub
End Class
