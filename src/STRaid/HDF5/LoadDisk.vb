Imports System.Drawing
Imports HDF.PInvoke

Public Module LoadDisk

    Public Function LoadDiskMemory(h5ad As String) As AnnData
        Dim fileId = H5F.open(h5ad, H5F.ACC_RDONLY)
        Dim xpca = ReadData.Read_dataset(fileId, "/obsm/X_pca")
        Dim pca_result = xpca.GetSingles.Split(xpca.dims(1)).ToArray

        Dim spatial = ReadData.Read_dataset(fileId, "/obsm/spatial").GetLongs.Split(2).Select(Function(t) New Point(t(0), t(1))).ToArray
        Dim xumap = ReadData.Read_dataset(fileId, "/obsm/X_umap").GetSingles.Split(2).Select(Function(t) New PointF(t(0), t(1))).ToArray
        Dim vargeneids = ReadData.Read_strings(fileId, "/var/gene_ids")
        Dim obsindex = ReadData.Read_strings(fileId, "/obs/_index")
        Dim xdata = ReadData.Read_dataset(fileId, "/X/data").GetSingles.ToArray
        Dim xindices = ReadData.Read_dataset(fileId, "/X/indices").GetIntegers.ToArray
        Dim xindptr = ReadData.Read_dataset(fileId, "/X/indptr").GetIntegers.ToArray

        Pause()
    End Function
End Module
