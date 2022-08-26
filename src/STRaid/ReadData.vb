Imports HDF.PInvoke

Public Class ReadData


    Sub New(path As String)
        Dim fileId = H5F.open(path, H5F.ACC_RDONLY)

        Pause()
    End Sub

End Class
