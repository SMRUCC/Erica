Imports System
Imports bgee
Imports Microsoft.VisualBasic.Serialization.JSON

Module Program
    Sub Main(args As String())

        Dim data = "E:\Erica\test\bgee.json".LoadJSON(Of DataSet)

        Pause()
    End Sub
End Module
