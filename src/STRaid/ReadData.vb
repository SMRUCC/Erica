Imports HDF.PInvoke

Public Class ReadData


    Sub New(path As String)
        Dim fileId = H5F.open(path, H5F.ACC_RDONLY)
        Dim dataSetId = H5D.open(fileId, "/X/data")
        Dim dataSpaceId = H5D.get_space(dataSetId)

        Pause()
    End Sub

End Class
