Imports System.Drawing
Imports HDF.PInvoke

Public Module LoadDisk

    Public Function LoadDiskMemory(h5ad As String) As AnnData
        Dim fileId = H5F.open(h5ad, H5F.ACC_RDONLY)

        ' /X
        Dim x As X = loadX(fileId)
        ' /var
        Dim var As Var = loadVar(fileId)
        ' /obsm
        Dim obsm As Obsm = loadObsm(fileId)

        ' Dim obsindex = ReadData.Read_strings(fileId, "/obs/_index")

        Return New AnnData With {
            .X = x,
            .var = var,
            .obsm = obsm
        }
    End Function

    Private Function loadObsm(fileId As Long) As Obsm
        Dim xpca = ReadData.Read_dataset(fileId, "/obsm/X_pca")
        Dim pca_result = xpca.GetSingles.Split(xpca.dims(1)).ToArray
        Dim spatial = ReadData.Read_dataset(fileId, "/obsm/spatial").GetLongs.Split(2).Select(Function(t) New Point(t(0), t(1))).ToArray
        Dim xumap = ReadData.Read_dataset(fileId, "/obsm/X_umap").GetSingles.Split(2).Select(Function(t) New PointF(t(0), t(1))).ToArray

        Return New Obsm With {
            .spatial = spatial,
            .X_pca = pca_result,
            .X_umap = xumap
        }
    End Function

    Private Function loadVar(fileId As Long) As Var
        Dim var_geneids = ReadData.Read_strings(fileId, "/var/gene_ids")

        Return New Var With {
            .gene_ids = var_geneids
        }
    End Function

    Private Function loadX(fileId As Long) As X
        Dim xdata = ReadData.Read_dataset(fileId, "/X/data").GetSingles.ToArray
        Dim xindices = ReadData.Read_dataset(fileId, "/X/indices").GetIntegers.ToArray
        Dim xindptr = ReadData.Read_dataset(fileId, "/X/indptr").GetIntegers.ToArray
        Dim x As X = X.ShapeMatrix(xdata, xindices, xindptr)

        Return x
    End Function
End Module
