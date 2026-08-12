''' <summary>
''' Represents a single assay (e.g. RNA, spatial) in a Seurat object.
''' Each assay can contain multiple layers (counts, data, scale.data, etc.)
''' stored as matrices.
''' </summary>
Public Class SeuratAssay

    ''' <summary>
    ''' The assay name (e.g. "RNA", "Spatial")
    ''' </summary>
    Public Property Name As String

    ''' <summary>
    ''' The key prefix used for feature names (e.g. "rna_")
    ''' </summary>
    Public Property Key As String

    ''' <summary>
    ''' Raw count matrix (rows = features, columns = cells).
    ''' This corresponds to the "counts" layer in Seurat v5.
    ''' </summary>
    Public Property Counts As Double(,)

    ''' <summary>
    ''' Normalized data matrix (rows = features, columns = cells).
    ''' This corresponds to the "data" layer in Seurat v5 (log-normalized).
    ''' </summary>
    Public Property Data As Double(,)

    ''' <summary>
    ''' Scaled data matrix (rows = features, columns = cells).
    ''' This corresponds to the "scale.data" layer in Seurat v5 (z-scored).
    ''' May be Nothing if ScaleData has not been run.
    ''' </summary>
    Public Property ScaleData As Double(,)

    ''' <summary>
    ''' Variable features for this assay.
    ''' A string array of feature names that are marked as variable.
    ''' </summary>
    Public Property VariableFeatures As String()

    ''' <summary>
    ''' Feature-level metadata for this assay.
    ''' Contains per-gene annotations (e.g. highly variable status).
    ''' Keys: column names (e.g. "vst.variance", "vst.mean")
    ''' Values: arrays of feature-level data
    ''' </summary>
    Public Property FeatureMetaData As Dictionary(Of String, Array)

    ''' <summary>
    ''' Feature names (gene names) for this assay.
    ''' </summary>
    Public Property FeatureNames As String()

    ''' <summary>
    ''' Number of features (genes) in this assay.
    ''' </summary>
    Public ReadOnly Property nFeatures As Integer
        Get
            If FeatureNames IsNot Nothing Then
                Return FeatureNames.Length
            End If
            If Counts IsNot Nothing Then
                Return Counts.GetLength(0)
            End If
            If Data IsNot Nothing Then
                Return Data.GetLength(0)
            End If
            Return 0
        End Get
    End Property

    ''' <summary>
    ''' Number of cells in this assay.
    ''' </summary>
    Public ReadOnly Property nCells As Integer
        Get
            If Counts IsNot Nothing Then
                Return Counts.GetLength(1)
            End If
            If Data IsNot Nothing Then
                Return Data.GetLength(1)
            End If
            Return 0
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return $"SeuratAssay[{Name}] features={nFeatures} cells={nCells} key={Key}"
    End Function

End Class
