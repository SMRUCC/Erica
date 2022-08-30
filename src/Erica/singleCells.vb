Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SparoMx
Imports STRaid

<Package("singleCell")>
Public Module singleCells

    Sub Main()
        Call Internal.generic.add("plot", GetType(Deconvolve), AddressOf plotLDA)
    End Sub

    Public Function plotLDA(lda As Deconvolve, args As list, env As Environment) As Object
        Dim theme As New Theme With {
            .background = "white"
        }
        Dim app As New LDAdeconvPlot(lda, theme)
        Dim size As String = InteropArgumentHelper.getSize(args!size, env, "2700,2100")
        Dim driver As Drivers = env.getDriver
        Dim ppi As Integer = args.getValue("ppi", env, [default]:=300)

        Return app.Plot(size, ppi, driver)
    End Function

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

    <ExportAPI("expression_list")>
    Public Function ExpressionList(raw As AnnData, Optional q As Double = 0.2) As Dictionary(Of String, String())
        Return raw.ExpressionList(q)
    End Function
End Module
