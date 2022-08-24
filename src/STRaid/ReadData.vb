Imports Microsoft.VisualBasic.Data.IO.HDF5

Public Class ReadData

    Dim buffer As HDF5File

    Sub New(path As String)
        buffer = HDF5File.Open(path)

        Dim X = buffer("/X/data").data

        Pause()
    End Sub

End Class
