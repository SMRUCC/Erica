Imports System.Runtime.CompilerServices

Public Class IDFactorEnums

    Friend ReadOnly enums As New Dictionary(Of String, (index As Integer, name As String))
    Friend ReadOnly toID As New Dictionary(Of Integer, String)

    Public ReadOnly Property size As Integer
        Get
            Return enums.Count
        End Get
    End Property

    Default Public ReadOnly Property ID(key As String) As Integer
        Get
            Return enums(key).index
        End Get
    End Property

    Sub New()
    End Sub

    Sub New(enums As Dictionary(Of String, (index As Integer, name As String)))
        Me.enums = enums
        Me.toID = enums.ToDictionary(Function(a) a.Value.index, Function(a) a.Key)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overloads Function ToString([enum] As Integer) As String
        Return toID(key:=[enum])
    End Function

    Public Function EnumValue(id As String, name As String) As Integer
        If Not enums.ContainsKey(id) Then
            Dim intptr As Integer = enums.Count + 1

            Call toID.Add(intptr, id)
            Call enums.Add(id, (intptr, name))
        End If

        Return enums(id).index
    End Function

End Class
