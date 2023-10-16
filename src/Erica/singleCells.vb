Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports STRaid

''' <summary>
''' single cell data toolkit
''' </summary>
<Package("singleCell")>
Public Module singleCells

    ''' <summary>
    ''' extract the raw expression data matrix from the h5ad object
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    ''' <example>
    ''' require(GCModeller);
    ''' 
    ''' imports "STdata" from "Erica";
    ''' imports "geneExpression" from "phenotype_kit";
    ''' 
    ''' let stRaid = read.h5ad("/path/to/expr_mat.h5ad");
    ''' let exp_mat = singleCell::HTS_matrix(stRaid);
    ''' 
    ''' geneExpression::write.expr_matrix(exp_mat, file = "/path/to/save.csv");
    ''' </example>
    <ExportAPI("HTS_matrix")>
    <RApiReturn(GetType(HTS.DataFrame.Matrix))>
    Public Function HTS_matrix(h5ad As Object, Optional env As Environment = Nothing) As Object
        If h5ad Is Nothing Then
            Return Nothing
        End If
        If TypeOf h5ad Is AnnData Then
            Return DirectCast(h5ad, AnnData).ExportExpression
        ElseIf TypeOf h5ad Is String Then
            Return DirectCast(h5ad, String).DoCall(AddressOf LoadDisk.LoadRawExpressionMatrix)
        Else
            Return Internal.debug.stop({
                $"A file path or h5ad anndata is required!",
                $"given: {h5ad.GetType.FullName}"
            }, env)
        End If
    End Function

    ''' <summary>
    ''' read h5ad object from a specific hdf5 file
    ''' </summary>
    ''' <param name="h5adfile"></param>
    ''' <param name="loadExpr0"></param>
    ''' <returns></returns>
    <ExportAPI("read.h5ad")>
    Public Function readH5ad(h5adfile As String, Optional loadExpr0 As Boolean = True) As AnnData
        Return LoadDisk.LoadDiskMemory(h5adfile, loadExpr0)
    End Function

    ''' <summary>
    ''' export the spatial maps data
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <param name="useCellAnnotation"></param>
    ''' <returns></returns>
    <ExportAPI("spatialMap")>
    Public Function spatialMap(h5ad As AnnData, Optional useCellAnnotation As Boolean? = Nothing) As dataframe
        Dim annos As SpotAnnotation() = SpotAnnotation _
            .LoadAnnotations(h5ad, useCellAnnotation) _
            .ToArray
        Dim x = annos.Select(Function(a) a.X).ToArray
        Dim y = annos.Select(Function(a) a.Y).ToArray
        Dim colors = annos.Select(Function(a) a.color).ToArray
        Dim clusters = annos.Select(Function(a) a.label).ToArray

        Return New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"x", x},
                {"y", y},
                {"class", clusters},
                {"color", colors}
            }
        }
    End Function

    ''' <summary>
    ''' Extract the PCA matrix from h5ad
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <returns></returns>
    <ExportAPI("pca_annotation")>
    Public Function exportPCA(h5ad As AnnData) As dataframe
        Dim pca = h5ad.obsm.X_pca.MatrixTranspose.ToArray
        Dim labels = h5ad.obs.class_labels
        Dim clusters = h5ad.obs.clusters _
            .Select(Function(i) labels(i)) _
            .ToArray
        Dim colors As String() = h5ad.uns.clusters_colors

        colors = h5ad.obs.clusters _
            .Select(Function(i) colors(i)) _
            .ToArray

        Dim pca_matrix As New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"class", clusters},
                {"color", colors}
            }
        }

        For i As Integer = 0 To pca.Length - 1
            pca_matrix.add($"pc{i + 1}", pca(i))
        Next

        Return pca_matrix
    End Function

    ''' <summary>
    ''' Extract the UMAP matrix from h5ad
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <returns></returns>
    <ExportAPI("umap_annotation")>
    Public Function exportUMAP(h5ad As AnnData) As dataframe
        Dim umap = h5ad.obsm.X_umap
        Dim labels = h5ad.obs.class_labels
        Dim clusters = h5ad.obs.clusters _
            .Select(Function(i) labels(i)) _
            .ToArray
        Dim x As Double() = umap.Select(Function(a) CDbl(a.X)).ToArray
        Dim y As Double() = umap.Select(Function(a) CDbl(a.Y)).ToArray
        Dim colors As String() = h5ad.uns.clusters_colors

        colors = h5ad.obs.clusters _
            .Select(Function(i) colors(i)) _
            .ToArray

        Return New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"x", x},
                {"y", y},
                {"class", clusters},
                {"color", colors}
            }
        }
    End Function

    ''' <summary>
    ''' Extract the gene id set with non-missing expression status under 
    ''' the given threshold value <paramref name="q"/>.
    ''' </summary>
    ''' <param name="raw"></param>
    ''' <param name="q">
    ''' the percentage threshold value for assign a gene a missing feature
    ''' </param>
    ''' <returns></returns>
    <ExportAPI("expression_list")>
    Public Function ExpressionList(raw As AnnData, Optional q As Double = 0.2) As Dictionary(Of String, String())
        Return raw.ExpressionList(q)
    End Function
End Module
