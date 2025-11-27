Imports Microsoft.VisualBasic.Imaging.Math2D

Public Class FormRotateParameters

    Public ReadOnly Property Argument As Transform
        Get
            Return New Transform With {
                .theta = Val(TextBox1.Text)
            }
        End Get
    End Property

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        DialogResult = DialogResult.OK
    End Sub
End Class