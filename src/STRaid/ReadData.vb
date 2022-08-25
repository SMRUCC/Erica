Imports Microsoft.VisualBasic.Data.IO.HDF5

Public Class ReadData

    Dim buffer As HDF5File

    Sub New(path As String)
        buffer = HDF5File.Open(path)
        Dim genome = buffer("/var/__categories/genome").data

        Dim feature_types = buffer("/var/__categories/feature_types").data

        Dim X = buffer("/X/data").data

        Pause()
    End Sub

End Class
