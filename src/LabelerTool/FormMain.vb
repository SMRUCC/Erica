Imports System.IO
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Public Class FormMain

    Private Sub OpenImageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenImageToolStripMenuItem.Click
        Using file As New OpenFileDialog With {.Filter = "image file(*.png)|*.png"}
            If file.ShowDialog = DialogResult.OK Then
                Dim image As Microsoft.VisualBasic.Imaging.Image = Microsoft.VisualBasic.Imaging.Image.FromStream(file.FileName.Open(FileMode.Open, doClear:=False, [readOnly]:=True))
                Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)
                Dim pixels As Color() = bitmap.GetPixelsAll.ToArray

                For i As Integer = 0 To pixels.Length - 1
                    If pixels(i).Equals(Color.Black, tolerance:=9) OrElse pixels(i).IsTransparent Then
                        pixels(i) = Color.White
                    End If
                Next

                Using s As New MemoryStream
                    bitmap = New BitmapBuffer(pixels, bitmap.Size)
                    bitmap.Save(s)
                    s.Seek(Scan0, SeekOrigin.Begin)

                    PictureBox1.BackgroundImage = System.Drawing.Image.FromStream(s)
                End Using
            End If
        End Using
    End Sub

    Private Sub OpenLabelToolToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenLabelToolToolStripMenuItem.Click
        Call New FormTool().ShowDialog()
    End Sub

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call SkiaDriver.Register()
    End Sub
End Class