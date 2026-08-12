Imports System.IO

''' <summary>
''' Represents a complete Seurat object read from a GNU R .rda/.rds file.
''' This is the top-level container for single-cell / spatial transcriptomics data.
''' </summary>
Public Class SeuratObject

    ''' <summary>
    ''' Seurat version string (e.g. "5.0.1")
    ''' </summary>
    Public Property Version As String

    ''' <summary>
    ''' All assays in the Seurat object (e.g. RNA, Spatial).
    ''' Key: assay name, Value: the assay data.
    ''' </summary>
    Public Property Assays As Dictionary(Of String, SeuratAssay)

    ''' <summary>
    ''' The name of the currently active assay.
    ''' </summary>
    Public Property ActiveAssay As String

    ''' <summary>
    ''' Cell-level metadata.
    ''' Keys: metadata column names (e.g. "orig.ident", "nCount_RNA", "nFeature_RNA")
    ''' Values: arrays of per-cell values.
    ''' </summary>
    Public Property MetaData As Dictionary(Of String, Array)

    ''' <summary>
    ''' Cell/spot barcode names.
    ''' </summary>
    Public Property CellNames As String()

    ''' <summary>
    ''' The active cluster identity (factor levels per cell).
    ''' </summary>
    Public Property ActiveIdent As String()

    ''' <summary>
    ''' Dimensionality reduction results.
    ''' Key: reduction name (e.g. "pca", "umap"), Value: the reduction data.
    ''' </summary>
    Public Property Reductions As Dictionary(Of String, DimReduction)

    ''' <summary>
    ''' Spatial images (for spatial transcriptomics data).
    ''' Key: image name, Value: the image data.
    ''' </summary>
    Public Property Images As Dictionary(Of String, SeuratImage)

    ''' <summary>
    ''' Number of cells in this Seurat object.
    ''' </summary>
    Public ReadOnly Property nCells As Integer
        Get
            If CellNames IsNot Nothing Then Return CellNames.Length
            If MetaData IsNot Nothing AndAlso MetaData.Count > 0 Then
                Dim firstCol As Array = MetaData.Values.FirstOrDefault()
                If firstCol IsNot Nothing Then Return firstCol.Length
            End If
            Return 0
        End Get
    End Property

    ''' <summary>
    ''' Total number of features across all assays.
    ''' </summary>
    Public ReadOnly Property nFeatures As Integer
        Get
            If Assays Is Nothing OrElse Assays.Count = 0 Then Return 0
            Dim total As Integer = 0
            For Each assay In Assays.Values
                total += assay.nFeatures
            Next
            Return total
        End Get
    End Property

    ''' <summary>
    ''' Convenience method: read a Seurat object from an .rda/.rds file path.
    ''' </summary>
    ''' <param name="filePath">Path to the .rda or .rds file.</param>
    ''' <returns>A populated SeuratObject, or Nothing on error.</returns>
    Public Shared Function ReadFromFile(filePath As String) As SeuratObject
        If Not File.Exists(filePath) Then
            Console.Error.WriteLine($"File not found: {filePath}")
            Return Nothing
        End If

        Try
            Return SeuratObjectReader.ReadFile(filePath)
        Catch ex As Exception
            Console.Error.WriteLine($"Error reading Seurat object from '{filePath}': {ex.GetType().Name}: {ex.Message}")
            Console.Error.WriteLine(ex.StackTrace)
            Return Nothing
        End Try
    End Function

    Public Overrides Function ToString() As String
        Dim assayNames As String = If(Assays IsNot Nothing, String.Join(",", Assays.Keys), "none")
        Dim redNames As String = If(Reductions IsNot Nothing, String.Join(",", Reductions.Keys), "none")
        Return $"SeuratObject[v{Version}] cells={nCells} features={nFeatures} assays=[{assayNames}] reductions=[{redNames}]"
    End Function

End Class
