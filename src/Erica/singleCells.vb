Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Vectorization
Imports STRaid
Imports STRaid.HDF5

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
    ''' <param name="h5adfile">The file path to the h5ad rawdata file</param>
    ''' <param name="loadExpr0"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' this function only works on Windows platform.
    ''' </remarks>
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
    ''' <example>
    ''' let stRaid = read.h5ad("/path/to/expr_mat.h5ad");
    ''' let spatial = spatialMap(stRaid);
    ''' 
    ''' str(spatial);
    ''' </example>
    <ExportAPI("spatialMap")>
    Public Function spatialMap(h5ad As AnnData, Optional useCellAnnotation As Boolean? = Nothing) As dataframe
        Dim annos As SpotAnnotation() = SpotAnnotation _
            .LoadAnnotations(h5ad, useCellAnnotation) _
            .ToArray
        Dim x = annos.Select(Function(a) a.x).ToArray
        Dim y = annos.Select(Function(a) a.y).ToArray
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

    ''' <summary>
    ''' Create spatial annotations data set for each spot data
    ''' </summary>
    ''' <param name="x">X of the spot dataset, a numeric vector</param>
    ''' <param name="y">Y of the spot dataset, a numeric vector</param>
    ''' <param name="label">A character vector that assign the class label of each
    ''' spatial spot, the vector size of this parameter must be equals to the 
    ''' vector size of x and y.</param>
    ''' <param name="colors">A character value of the spatial class color 
    ''' palette name or a vector of color code character for assign each 
    ''' spatial spot.</param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("spatial_annotations")>
    <RApiReturn(GetType(SpotAnnotation))>
    Public Function spatial_annotations(<RRawVectorArgument> x As Object,
                                        <RRawVectorArgument> y As Object,
                                        <RRawVectorArgument> label As Object,
                                        <RRawVectorArgument>
                                        Optional colors As Object = "paper",
                                        Optional env As Environment = Nothing) As Object

        Dim px As Double() = CLRVector.asNumeric(x)
        Dim py As Double() = CLRVector.asNumeric(y)
        Dim labels As String() = CLRVector.asCharacter(label)
        Dim colorSet As String() = CLRVector.asCharacter(colors)

        If px.Length <> py.Length Then
            Return Internal.debug.stop($"the vector size of the spatial information x({px.Length}) should be matched with y({py.Length})!", env)
        End If
        If labels.Length <> px.Length AndAlso labels.Length > 1 Then
            Return Internal.debug.stop($"the class label information({labels.Length}) is not matched with the spatial information [x,y]({px.Length})!", env)
        End If

        If colorSet.Length = 1 Then
            colorSet = Designer.GetColors(colorSet(0), labels.Distinct.Count) _
                .Select(Function(a) a.ToHtmlColor) _
                .ToArray
        End If
        If colorSet.Length <> labels.Length Then
            colorSet = Designer.Colors(
                col:=colorSet.Select(Function(c) c.TranslateColor).ToArray,
                n:=labels.Distinct.Count
            ) _
            .Select(Function(a) a.ToHtmlColor) _
            .ToArray
        End If

        If colorSet.Length <> labels.Length Then

        End If
    End Function
End Module
