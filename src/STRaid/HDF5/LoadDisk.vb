Imports System.Drawing
Imports HDF.PInvoke

Public Module LoadDisk

    Public Function LoadDiskMemory(h5ad As String, Optional loadExpr0 As Boolean = True) As AnnData
        Dim fileId = H5F.open(h5ad, H5F.ACC_RDONLY)

        ' /obs
        Dim obs As Obs = loadObs(fileId)
        ' /var
        Dim var As Var = loadVar(fileId)
        ' /obsm
        Dim obsm As Obsm = loadObsm(fileId)
        ' /uns
        Dim uns As Uns = loadUns(fileId)
        ' /X
        Dim x As X = Nothing

        If loadExpr0 Then
            x = loadX(fileId, var.gene_ids.Length)
        End If

        Return New AnnData With {
            .X = x,
            .var = var,
            .obsm = obsm,
            .obs = obs,
            .uns = uns,
            .source = h5ad.BaseName
        }
    End Function

    Private Function loadUns(fileId As Long) As Uns
        Dim colors As String() = ReadData.Read_strings(fileId, "/uns/clusters_colors")
        Dim annotations As String() = ReadData.Read_strings(fileId, "/uns/annotation_colors")

        Return New Uns With {
            .clusters_colors = colors,
            .annotation_colors = annotations
        }
    End Function

    Private Function loadObs(fileId As Long) As Obs
        ' Dim obsindex = ReadData.Read_strings(fileId, "/obs/_index")
        Dim clusters As Integer()
        Dim labels As String()

        If ReadData.HasDataSet(fileId, "/obs/clusters") Then
            clusters = ReadData.Read_dataset(fileId, "/obs/clusters") _
                .dataBytes _
                .Select(Function(b) CInt(b)) _
                .ToArray
            labels = ReadData.Read_strings(fileId, "/obs/__categories/clusters")
        Else
            clusters = ReadData.Read_dataset(fileId, "/obs/annotation") _
                .dataBytes _
                .Select(Function(b) CInt(b)) _
                .ToArray
            labels = ReadData.Read_strings(fileId, "/obs/__categories/annotation")
        End If

        Return New Obs With {
            .clusters = clusters,
            .class_labels = labels
        }
    End Function

    Private Function readPCA(fileId As Long) As Single()()
        Dim xpca = ReadData.Read_dataset(fileId, "/obsm/X_pca")
        Dim pca_result = xpca.GetSingles.Split(xpca.dims(1)).ToArray

        Return pca_result
    End Function

    Private Function loadSpatialMap(fileId As Long) As PointF()
        Dim raw = ReadData.Read_dataset(fileId, "/obsm/spatial")

        If raw.byte_size = 8 Then
            Return raw.GetLongs _
                .Split(2) _
                .Select(Function(t) New PointF(t(0), t(1))) _
                .ToArray
        Else

        End If
    End Function

    Private Function loadObsm(fileId As Long) As Obsm
        Dim spatial = loadSpatialMap(fileId)
        Dim xumap = ReadData.Read_dataset(fileId, "/obsm/X_umap") _
            .GetSingles _
            .Split(2) _
            .Select(Function(t) New PointF(t(0), t(1))) _
            .ToArray
        Dim pca_result As Single()() = Nothing

        If ReadData.HasDataSet(fileId, "/obsm/X_pca") Then
            pca_result = readPCA(fileId)
        End If

        Return New Obsm With {
            .spatial = spatial,
            .X_pca = pca_result,
            .X_umap = xumap
        }
    End Function

    Private Function loadVar(fileId As Long) As Var
        Dim var_geneids = ReadData.Read_strings(fileId, "/var/gene_ids")
        Dim cell_counts = ReadData.Read_dataset(fileId, "/var/n_cells_by_counts").GetLongs.ToArray
        Dim var_geneNames = ReadData.Read_strings(fileId, "/var/gene_short_name")

        Return New Var With {
            .gene_ids = var_geneids,
            .n_cells_by_counts = cell_counts,
            .gene_short_name = var_geneNames
        }
    End Function

    Private Function loadX(fileId As Long, maxColumns As Integer) As X
        Dim xdata = ReadData.Read_dataset(fileId, "/X/data").GetSingles.ToArray
        Dim xindices = ReadData.Read_dataset(fileId, "/X/indices").GetIntegers.ToArray
        Dim xindptr = ReadData.Read_dataset(fileId, "/X/indptr").GetIntegers.ToArray
        Dim x As X = X.ShapeMatrix(xdata, xindices, xindptr, geneIdsize:=maxColumns)

        Return x
    End Function
End Module
