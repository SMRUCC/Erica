Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem

Public Class BgeeDiskReader

    Sub New(file As String)
        Dim disk As New StreamPack(file, [readonly]:=True)
    End Sub
End Class
