''' <summary>
''' Represents a dimensionality reduction result (e.g. PCA, UMAP, t-SNE)
''' stored in a Seurat object's reductions slot.
''' </summary>
Public Class DimReduction

    ''' <summary>
    ''' The reduction name (e.g. "pca", "umap", "tsne")
    ''' </summary>
    Public Property Name As String

    ''' <summary>
    ''' The reduction method/algorithm name (e.g. "PCA", "UMAP")
    ''' </summary>
    Public Property Method As String

    ''' <summary>
    ''' The key prefix for dimension names (e.g. "PC_", "UMAP_")
    ''' </summary>
    Public Property Key As String

    ''' <summary>
    ''' Cell embeddings matrix (rows = cells, columns = dimensions).
    ''' For PCA this is the PC scores. For UMAP/t-SNE this is the
    ''' 2D/3D coordinates.
    ''' </summary>
    Public Property CellEmbeddings As Double(,)

    ''' <summary>
    ''' Cell names corresponding to the rows of CellEmbeddings.
    ''' </summary>
    Public Property CellNames As String()

    ''' <summary>
    ''' Feature loadings matrix (rows = features, columns = dimensions).
    ''' For PCA this is the rotation matrix. May be Nothing for
    ''' non-linear methods like UMAP/t-SNE.
    ''' </summary>
    Public Property FeatureLoadings As Double(,)

    ''' <summary>
    ''' Feature names corresponding to the rows of FeatureLoadings.
    ''' </summary>
    Public Property FeatureLoadingNames As String()

    ''' <summary>
    ''' Standard deviation of each component (PCA only).
    ''' </summary>
    Public Property Stdev As Double()

    ''' <summary>
    ''' Total variance explained (PCA only).
    ''' </summary>
    Public Property TotalVariance As Double

    ''' <summary>
    ''' Number of dimensions in this reduction.
    ''' </summary>
    Public ReadOnly Property nDimensions As Integer
        Get
            If CellEmbeddings IsNot Nothing Then
                Return CellEmbeddings.GetLength(1)
            End If
            Return 0
        End Get
    End Property

    ''' <summary>
    ''' Number of cells in this reduction.
    ''' </summary>
    Public ReadOnly Property nCells As Integer
        Get
            If CellEmbeddings IsNot Nothing Then
                Return CellEmbeddings.GetLength(0)
            End If
            Return 0
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return $"DimReduction[{Name}] method={Method} cells={nCells} dims={nDimensions} key={Key}"
    End Function

End Class
