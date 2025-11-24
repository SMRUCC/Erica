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
End Class
