
Imports HDF.PInvoke
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
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

    <ExportAPI("read.h5ad")>
    Public Function readH5ad(h5adfile As String) As AnnData
        Return LoadDisk.LoadDiskMemory(h5adfile)
    End Function

    <ExportAPI("spatialMap")>
    Public Function spatialMap(h5ad As AnnData) As dataframe
        Dim spatial = h5ad.obsm.spatial
        Dim labels = h5ad.obs.class_labels
        Dim clusters = h5ad.obs.clusters _
            .Select(Function(i) labels(i)) _
            .ToArray
        Dim colors As String() = h5ad.uns.clusters_colors
        Dim x As Double() = spatial.Select(Function(a) CDbl(a.X)).ToArray
        Dim y As Double() = spatial.Select(Function(a) CDbl(a.Y)).ToArray

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
End Module
