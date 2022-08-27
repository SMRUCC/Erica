Public Class IDFactorEnums

    Friend ReadOnly enums As New Dictionary(Of String, (index As Integer, name As String))

    Sub New()
    End Sub

    Sub New(enums As Dictionary(Of String, (index As Integer, name As String)))
        Me.enums = enums
    End Sub

    Public Function EnumValue(id As String, name As String) As Integer
        If Not enums.ContainsKey(id) Then
            Call enums.Add(id, (enums.Count + 1, name))
        End If

        Return enums(id).index
    End Function

End Class
