Imports Microsoft.VisualBasic.Data.IO.HDF5

Public Class ReadData

    Dim buffer As HDF5File

    Sub New(path As String)
        buffer = HDF5File.Open(path)
    End Sub

End Class
