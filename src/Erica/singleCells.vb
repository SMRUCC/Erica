Imports System.Drawing
Imports System.IO
Imports HEView
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Algorithm.base
Imports Microsoft.VisualBasic.Data.ChartPlots
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Data.Framework.IO
Imports Microsoft.VisualBasic.Emit.Delegates
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors.Scaler
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.SignalProcessing.HungarianAlgorithm
Imports Microsoft.VisualBasic.MIME.application.json
Imports Microsoft.VisualBasic.MIME.application.json.Javascript
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports SMRUCC.genomics.Analysis
Imports SMRUCC.genomics.Analysis.SingleCell
Imports SMRUCC.genomics.Analysis.Spatial.RAID
Imports SMRUCC.genomics.Analysis.Spatial.RAID.HDF5
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Vectorization
Imports clr_df = Microsoft.VisualBasic.Data.Framework.DataFrame
Imports RInternal = SMRUCC.Rsharp.Runtime.Internal

#If NET48 Then
Imports Pen = System.Drawing.Pen
Imports Pens = System.Drawing.Pens
Imports Brush = System.Drawing.Brush
Imports Font = System.Drawing.Font
Imports Brushes = System.Drawing.Brushes
Imports SolidBrush = System.Drawing.SolidBrush
Imports DashStyle = System.Drawing.Drawing2D.DashStyle
Imports Image = System.Drawing.Image
Imports Bitmap = System.Drawing.Bitmap
Imports GraphicsPath = System.Drawing.Drawing2D.GraphicsPath
Imports FontStyle = System.Drawing.FontStyle
#Else
#End If

''' <summary>
''' single cell data toolkit
''' </summary>
<Package("singleCell")>
<RTypeExport("dzi", GetType(DziImage))>
Public Module singleCells

    Sub Main()
        Call RInternal.Object.Converts.addHandler(GetType(SpotAnnotation()), AddressOf SpotAnnotationMatrix)
        Call RInternal.Object.Converts.addHandler(GetType(CellScan()), AddressOf HEcellsMatrix)
        Call RInternal.Object.Converts.addHandler(GetType(IHCCellScan()), AddressOf HEcellsMatrix2)
        Call RInternal.Object.Converts.addHandler(GetType(CellMatchResult()), AddressOf matchesTable)

        Call RInternal.generic.add("plot", GetType(CellScan()), AddressOf plotCellScans)
        Call RInternal.generic.add("plot", GetType(CellMatchResult()), AddressOf plotCellMatches)
    End Sub

    <RGenericOverloads("as.data.frame")>
    Private Function matchesTable(matches As CellMatchResult(), args As list, env As Environment) As Object
        Dim df As New dataframe With {.columns = New Dictionary(Of String, Array)}

        Call df.add("cell1", From m As CellMatchResult In matches Select m.CellA.CellGuid)
        Call df.add("cell2", From m As CellMatchResult In matches Select m.CellB.CellGuid)

        Call df.add("cell1_x", From m As CellMatchResult In matches Select m.CellA.physical_x)
        Call df.add("cell1_y", From m As CellMatchResult In matches Select m.CellA.physical_y)

        Call df.add("cell2_x", From m As CellMatchResult In matches Select m.CellB.physical_x)
        Call df.add("cell2_y", From m As CellMatchResult In matches Select m.CellB.physical_y)

        Call df.add("distance", From m As CellMatchResult In matches Select m.Distance)
        Call df.add("morphology", From m As CellMatchResult In matches Select m.Morphology)
        Call df.add("match_score", From m As CellMatchResult In matches Select m.MatchScore)

        Return df
    End Function

    <RGenericOverloads("plot")>
    Private Function plotCellMatches(matches As CellMatchResult(), args As list, env As Environment) As Object
        Dim slide1 As CellScan() = args.getValue(Of CellScan())("slide1", env)
        Dim slide2 As CellScan() = args.getValue(Of CellScan())("slide2", env)
        Dim padding As String = InteropArgumentHelper.getPadding(args!padding, "padding: 10% 10% 15% 20%;")
        Dim theme As New Theme With {
            .background = RColorPalette.getColor(args.getBySynonyms("background", "bg"), [default]:="white"),
            .gridFill = RColorPalette.getColor(args.getBySynonyms("gridFill", "grid.fill", "fill"), [default]:="white"),
            .padding = padding
        }
        Dim app As New VisualCellMatches(matches, slide1, slide2, theme)
        Dim size As String = InteropArgumentHelper.getSize(args.getByName("size"), env)
        Dim driver As Drivers = env.getDriver
        Dim dpi As Integer = CLRVector.asInteger(args.getBySynonyms("dpi", "ppi")).ElementAtOrDefault(0, [default]:=100)

        Return app.Plot(size, dpi, driver)
    End Function

    <RGenericOverloads("plot")>
    Private Function plotCellScans(cells As CellScan(), args As list, env As Environment) As Object
        Dim polygons As Polygon2D() = cells.Select(Function(cell) New Polygon2D(cell.scan_x, cell.scan_y)).ToArray
        Dim colors = RColorPalette.getColorSet(args.getBySynonyms("colors", "colorset", "colorSet"), "paper")
        Dim size = InteropArgumentHelper.getSize(args.getBySynonyms("size"), env)
        Dim scatter As Boolean = CLRVector.asScalarLogical(args.getBySynonyms("scatter"))
        Dim padding As String = InteropArgumentHelper.getPadding(args!padding, "padding: 10% 10% 15% 20%;")
        Dim theme As New Theme With {.colorSet = colors, .padding = padding}
        Dim driver As Drivers = env.getDriver

        If polygons.IsNullOrEmpty Then
            Return g.GraphicsPlots(
                size.SizeParser, "padding:0px", "white",
                plotAPI:=Sub(ByRef gfx, rect)

                         End Sub,
                driver:=driver)
        Else
            Dim app As New FillPolygons(polygons, scatter, theme)
            Return app.Plot(size, driver:=driver)
        End If
    End Function

    <RGenericOverloads("as.data.frame")>
    Private Function HEcellsMatrix2(cells As IHCCellScan(), args As list, env As Environment) As dataframe
        Dim df As dataframe = HEcellsMatrix(DirectCast(cells, CellScan()), args, env)
        Call df.add("antibody", From cell As IHCCellScan In cells Select cell.antibody)
        Return df
    End Function

    <RGenericOverloads("as.data.frame")>
    Private Function HEcellsMatrix(cells As CellScan(), args As list, env As Environment) As dataframe
        Dim df As New dataframe With {
            .rownames = cells _
                .Select(Function(c) c.CellGuid) _
                .ToArray,
            .columns = New Dictionary(Of String, Array)
        }

        Call df.add("tile_id", From cell As CellScan In cells Select cell.tile_id)
        Call df.add("x", From cell As CellScan In cells Select cell.x)
        Call df.add("y", From cell As CellScan In cells Select cell.y)
        Call df.add("physical_x", From cell As CellScan In cells Select cell.physical_x)
        Call df.add("physical_y", From cell As CellScan In cells Select cell.physical_y)
        Call df.add("area", From cell As CellScan In cells Select cell.area)
        Call df.add("ratio", From cell As CellScan In cells Select cell.ratio)
        Call df.add("size", From cell As CellScan In cells Select cell.points)
        Call df.add("r1", From cell As CellScan In cells Select cell.r1)
        Call df.add("r2", From cell As CellScan In cells Select cell.r2)
        Call df.add("theta", From cell As CellScan In cells Select cell.theta)
        Call df.add("weight", From cell As CellScan In cells Select cell.weight)
        Call df.add("density", From cell As CellScan In cells Select cell.density)
        Call df.add("mean_distance", From cell As CellScan In cells Select cell.average_dist)
        Call df.add("moran-I", From cell As CellScan In cells Select cell.moranI)
        Call df.add("p-value", From cell As CellScan In cells Select cell.pvalue)

        Return df
    End Function

    ''' <summary>
    ''' read the csv table file as cells data matrix
    ''' </summary>
    ''' <param name="file"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("read.cells")>
    <RApiReturn(GetType(CellScan), GetType(IHCCellScan))>
    Public Function readCellsMatrix(<RRawVectorArgument> file As Object, Optional env As Environment = Nothing) As Object
        Dim s = SMRUCC.Rsharp.GetFileStream(file, FileAccess.Read, env)

        If s Like GetType(Message) Then
            Return s.TryCast(Of Message)
        End If

        Dim df As DataFrameResolver = DataFrameResolver.Load(s.TryCast(Of Stream))
        Dim antibody As Integer = df.GetOrdinal("antibody")
        Dim isIHCCells As Boolean = antibody > -1
        Dim cells As New List(Of CellScan)
        Dim x As Integer = df.GetOrdinal("x")
        Dim y As Integer = df.GetOrdinal("y")
        Dim physical_x As Integer = df.GetOrdinal("physical_x")
        Dim physical_y As Integer = df.GetOrdinal("physical_y")
        Dim area As Integer = df.GetOrdinal("area")
        Dim ratio As Integer = df.GetOrdinal("ratio")
        Dim size As Integer = df.GetOrdinal("size")
        Dim r1 As Integer = df.GetOrdinal("r1")
        Dim r2 As Integer = df.GetOrdinal("r2")
        Dim theta As Integer = df.GetOrdinal("theta")
        Dim weight As Integer = df.GetOrdinal("weight")
        Dim density As Integer = df.GetOrdinal("density")
        Dim mean_distance As Integer = df.GetOrdinal("mean_distance")
        Dim moran_I As Integer = df.GetOrdinal("moran-I")
        Dim p_value As Integer = df.GetOrdinal("p-value")
        Dim tile_id As Integer = df.GetOrdinal("tile_id")

        Do While df.Read
            Dim cell As CellScan = If(isIHCCells, New IHCCellScan With {.antibody = df.GetString(antibody)}, New CellScan)

            cell.x = df.GetDouble(x)
            cell.y = df.GetDouble(y)
            cell.physical_x = df.GetDouble(physical_x)
            cell.physical_y = df.GetDouble(physical_y)
            cell.area = df.GetDouble(area)
            cell.ratio = df.GetDouble(ratio)
            cell.points = df.GetInt32(size)
            cell.r1 = df.GetDouble(r1)
            cell.r2 = df.GetDouble(r2)
            cell.weight = df.GetDouble(weight)
            cell.density = df.GetDouble(density)
            cell.average_dist = df.GetDouble(mean_distance)
            cell.moranI = df.GetDouble(moran_I)
            cell.pvalue = df.GetDouble(p_value)
            cell.tile_id = df.GetString(tile_id)
            cell.theta = df.GetDouble(theta)

            Call cells.Add(cell)
        Loop

        Return cells.ToArray
    End Function

    <RGenericOverloads("as.data.frame")>
    Private Function SpotAnnotationMatrix(spots As SpotAnnotation(), args As list, env As Environment) As dataframe
        Dim x = spots.Select(Function(a) a.x).ToArray
        Dim y = spots.Select(Function(a) a.y).ToArray
        Dim colors = spots.Select(Function(a) a.color).ToArray
        Dim clusters = spots.Select(Function(a) a.label).ToArray
        Dim hasBarcode As Boolean = spots.Any(Function(c) Not c.barcode.StringEmpty(, True))
        Dim cell_labels As String() = Nothing

        If hasBarcode Then
            cell_labels = spots _
                .Select(Function(c) c.barcode) _
                .ToArray
        End If

        Return New dataframe With {
            .columns = New Dictionary(Of String, Array) From {
                {"x", x},
                {"y", y},
                {"class", clusters},
                {"color", colors}
            },
            .rownames = cell_labels
        }
    End Function

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
            Return RInternal.debug.stop({
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
    ''' Read expression matrix from a TCGA MTX dataset folder
    ''' </summary>
    ''' <param name="dataset">a folder path to the TCGA MTX dataset files, this folder should includes the data files:
    ''' 1. barcodes.tsv
    ''' 2. features.tsv
    ''' 3. matrix.mtx
    ''' </param>
    ''' <returns>
    ''' a clr <see cref="clr_df"/> object that contains all information that read from the given TCGA dataset folder.
    ''' </returns>
    ''' <example>
    ''' let matrix = read.TCGA_mtx("/home/TCGA/gdc_download_20251007_143507.913543/cc317a3f-9aa8-4e82-b394-25a13320956b/aa55ac5e-7a48-45fd-9fb5-7e013805b247/");
    ''' </example>
    <ExportAPI("read.TCGA_mtx")>
    <RApiReturn(GetType(clr_df))>
    Public Function readTCGAMTX(dataset As String) As Object
        Return TCGA.MTXReader(dataset)
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
    Public Function spatialMap(h5ad As AnnData,
                               Optional useCellAnnotation As Boolean? = Nothing,
                               Optional env As Environment = Nothing) As dataframe

        Dim annos As SpotAnnotation() = SpotAnnotation _
            .LoadAnnotations(h5ad, useCellAnnotation) _
            .ToArray
        Dim spatial_df As dataframe = SpotAnnotationMatrix(annos, New list, env)

        Return spatial_df
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
    ''' <param name="barcode">
    ''' the raw reference barcode to the spots
    ''' </param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("spatial_annotations")>
    <RApiReturn(GetType(SpotAnnotation))>
    Public Function spatial_annotations(<RRawVectorArgument> x As Object,
                                        <RRawVectorArgument> y As Object,
                                        <RRawVectorArgument> label As Object,
                                        <RRawVectorArgument>
                                        Optional colors As Object = "paper",
                                        <RRawVectorArgument>
                                        Optional barcode As Object = Nothing,
                                        Optional env As Environment = Nothing) As Object

        Dim px As Double() = CLRVector.asNumeric(x)
        Dim py As Double() = CLRVector.asNumeric(y)
        Dim labels As String() = CLRVector.asCharacter(label)
        Dim colorSet As String() = CLRVector.asCharacter(colors)
        Dim barcodes As String() = CLRVector.asCharacter(barcode)

        If px.Length <> py.Length Then
            Return RInternal.debug.stop($"the vector size of the spatial information x({px.Length}) should be matched with y({py.Length})!", env)
        End If
        If labels.Length <> px.Length AndAlso labels.Length > 1 Then
            Return RInternal.debug.stop($"the class label information({labels.Length}) is not matched with the spatial information [x,y]({px.Length})!", env)
        End If
        If Not barcodes.IsNullOrEmpty AndAlso barcodes.Length <> px.Length Then
            Return RInternal.debug.stop($"the vector size of the spatial information x,y({px.Length}) should be matched with the spot barcodes({barcodes.Length})!", env)
        End If

        If colorSet.Length = 1 Then
            colorSet = Designer.GetColors(colorSet(0), labels.Distinct.Count) _
                .Select(Function(a) a.ToHtmlColor) _
                .ToArray
        End If
        If colorSet.Length <> labels.Length Then
            colorSet = Designer.CubicSpline(
                colors:=colorSet _
                    .Select(Function(c) c.TranslateColor) _
                    .ToArray,
                n:=labels.Distinct.Count
            ) _
            .Select(Function(a) a.ToHtmlColor) _
            .ToArray
        End If

        Dim spots As New List(Of SpotAnnotation)

        If colorSet.Length <> labels.Length Then
            ' category mapping
            Dim mapper As New CategoryColorProfile(labels, colorSet)

            For i As Integer = 0 To labels.Length - 1
                Call spots.Add(New SpotAnnotation With {
                    .color = mapper.GetColor(labels(i)).ToHtmlColor,
                    .label = labels(i),
                    .x = px(i),
                    .y = py(i),
                    .barcode = barcodes.ElementAtOrNull(i)
                })
            Next
        Else
            ' sequence mapping
            For i As Integer = 0 To labels.Length - 1
                Call spots.Add(New SpotAnnotation With {
                    .color = colorSet(i),
                    .label = labels(i),
                    .x = px(i),
                    .y = py(i),
                    .barcode = barcodes.ElementAtOrNull(i)
                })
            Next
        End If

        Return spots.ToArray
    End Function

    ''' <summary>
    ''' scan the cells from a given HE image
    ''' </summary>
    ''' <param name="HEstain">
    ''' the HE image, should be a grayscale bitmap image.
    ''' </param>
    ''' <param name="flip"></param>
    ''' <param name="ostu_factor"></param>
    ''' <param name="offset"></param>
    ''' <param name="noise"></param>
    ''' <param name="moran_knn"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("HE_cells")>
    <RApiReturn(GetType(CellScan))>
    Public Function HECells(HEstain As Object,
                            Optional flip As Boolean = False,
                            Optional ostu_factor As Double = 0.7,
                            <RRawVectorArgument(TypeCodes.double)>
                            Optional offset As Object = Nothing,
                            Optional noise As Double = 0.25,
                            Optional moran_knn As Integer = 32,
                            Optional split_blocks As Boolean = False,
                            Optional env As Environment = Nothing) As Object

        Dim data As BitmapBuffer

        If HEstain Is Nothing Then
            Return Nothing
        End If

        If TypeOf HEstain Is Image OrElse HEstain.GetType.ImplementInterface(Of IRasterMemory) Then
            data = DirectCast(HEstain, IRasterMemory).GetMemoryBuffer
        ElseIf TypeOf HEstain Is Bitmap Then
            data = DirectCast(HEstain, Bitmap).MemoryBuffer
        ElseIf TypeOf HEstain Is BitmapBuffer Then
            data = DirectCast(HEstain, BitmapBuffer)
        Else
            Return Message.InCompatibleType(GetType(BitmapBuffer), HEstain.GetType, env)
        End If

        Dim offsetVec As Double() = CLRVector.asNumeric(offset)
        Dim offsetPt As PointF = If(offsetVec.IsNullOrEmpty, Nothing, New PointF(offsetVec(0), offsetVec(1)))
        Dim cells As CellScan() = CellScan _
            .CellLookups(data, offset:=offset) _
            .FilterNoise(noise)

        If split_blocks Then
            cells = cells.Split().ToArray
        End If

        cells = cells.MoranI(knn:=moran_knn)

        Return cells
    End Function

    ''' <summary>
    ''' read the deepzoom image metadata xml file
    ''' </summary>
    ''' <param name="file"></param>
    ''' <returns></returns>
    <ExportAPI("read.dziImage")>
    Public Function dziFromfile(file As String) As DziImage
        Return file.LoadXml(Of DziImage)
    End Function

    ''' <summary>
    ''' Parse the dzi image metadata from a given xml document data
    ''' </summary>
    ''' <param name="xml"></param>
    ''' <returns></returns>
    <ExportAPI("parse_dziImage")>
    Public Function parseDzi(xml As String) As DziImage
        Return xml.LoadFromXml(Of DziImage)
    End Function

    ''' <summary>
    ''' Scan all single cell shapes from the given dzi slide data
    ''' </summary>
    ''' <param name="dzi">metadata of the dzi image</param>
    ''' <param name="level">usually be the max zoom level</param>
    ''' <param name="dir">
    ''' A directory path that contains the image files in current <paramref name="level"/>.
    ''' </param>
    ''' <param name="ostu_factor"></param>
    ''' <param name="noise">
    ''' quantile level for filter the polygon shape points. all cell shapes which has its
    ''' shape points less than this quantile level will be treated as noise
    ''' </param>
    ''' <param name="moran_knn"></param>
    ''' <param name="split_blocks"></param>
    ''' <returns>
    ''' if scanning of the IHC1 channels, then this function will returns a tuple list that contains
    ''' the rgb channels single cell detections result of the IHC1 image.
    ''' if scanning of the IHC2 channels, then this function will returns a tuple list that contains
    ''' the cmyk channels single cell detection result of the IHC2 image.
    ''' </returns>
    <ExportAPI("scan.dzi_cells")>
    <RApiReturn(GetType(CellScan), GetType(IHCCellScan))>
    Public Function scanDziCells(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                 Optional ostu_factor As Double = 0.7,
                                 Optional noise As Double = 0.25,
                                 Optional moran_knn As Integer = 32,
                                 Optional flip As Boolean = False,
                                 Optional split_blocks As Boolean = False,
                                 Optional split_rgb As Boolean = False,
                                 Optional IHC_antibody As list = Nothing,
                                 Optional env As Environment = Nothing) As Object

        If IHC_antibody IsNot Nothing Then
            Dim unmix As IHCScanner = antibodyColors(IHC_antibody)
            Dim cells = unmix.ScanCells(dzi, level, dir,
                ostu_factor:=ostu_factor,
                noise:=noise,
                moran_knn:=moran_knn,
                splitBlocks:=split_blocks).ToArray

            Return cells
        ElseIf split_rgb Then
            Dim rgb As list = list.empty
            Dim channels = dzi.ScanIHCRGBCells(level, dir,
                ostu_factor:=ostu_factor,
                noise:=noise,
                moran_knn:=moran_knn,
                splitBlocks:=split_blocks
            )

            Call rgb.add("r", channels.r)
            Call rgb.add("g", channels.g)
            Call rgb.add("b", channels.b)

            Return rgb
        End If

        Return dzi.ScanCells(level, dir, ostu_factor,
                             noise:=noise,
                             moran_knn:=moran_knn,
                             splitBlocks:=split_blocks,
                             flip:=flip) _
                  .ToArray
    End Function

    ''' <summary>
    ''' write the cell matrix data into a bson file
    ''' </summary>
    ''' <param name="cells">a vector of the cell objects</param>
    ''' <param name="file">file path to the bson file for save the cell matrix data</param>
    <ExportAPI("write.cells_bson")>
    Public Sub writeCellBson(cells As CellScan(), file As String)
        Using s As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)
            Dim json As JsonElement = If(
                TypeOf cells Is IHCCellScan(),
                DirectCast(cells, IHCCellScan()).CreateJSONElement,
                cells.CreateJSONElement)

            Call BSON.SafeWriteBuffer(json, s)
        End Using
    End Sub

    Private Function antibodyColors(IHC_antibody As list) As IHCScanner
        Dim antibody As New Dictionary(Of String, Color)

        For Each name As String In IHC_antibody.getNames
            antibody(name) = RColorPalette.GetRawColor(IHC_antibody.getByName(name))
        Next

        Return New IHCScanner(antibody)
    End Function

    ''' <summary>
    ''' unmix IHC stained pixel color into the corresponding antibody expression values
    ''' </summary>
    ''' <param name="pixel">[r,g,b] color value, in range [0,1]</param>
    ''' <param name="IHC_antibody">a tuple list of the antibody reference colors</param>
    ''' <returns></returns>
    <ExportAPI("ihc_unmixing")>
    Public Function IHCUnmixing_f(<RRawVectorArgument> pixel As Object,
                                  Optional IHC_antibody As list = Nothing,
                                  Optional env As Environment = Nothing) As Object

        Dim rgb As Double() = CLRVector.asNumeric(pixel)
        Dim unmix As IHCScanner = antibodyColors(IHC_antibody)
        Dim color As Color = Color.FromArgb(CInt(rgb(0) * 255), CInt(rgb(1) * 255), CInt(rgb(2) * 255))
        Dim vec As Double() = unmix.UnmixPixel(color)
        Dim names As String() = unmix.Antibodies

        Return New list(vec, names)
    End Function

    <ExportAPI("dzi_unmix")>
    Public Function dzi_unmix(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment, IHC_antibody As list, export_dir As String) As Object
        Dim unmix As IHCScanner = antibodyColors(IHC_antibody)
        Dim layers As Dictionary(Of String, DziImageBuffer()) = unmix.UnmixDziImage(dzi, level, dir)

        For Each antibody_name As String In unmix.Antibodies
            Dim antibody_dir As String = Path.Combine(export_dir, antibody_name)

            For Each image As DziImageBuffer In layers(antibody_name)
                Call image.bitmap.Save($"{antibody_dir}/{image.xy.JoinBy("_")}.bmp")
            Next
        Next

        Return Nothing
    End Function

    <ExportAPI("geo_transform")>
    <RApiReturn(GetType(CellScan))>
    Public Function geo_transform(cells As CellScan(), transform As GeometryTransform) As Object
        If TypeOf transform Is Transform Then
            Return CellScan.ApplyTransform(cells, DirectCast(transform, Transform))
        Else
            Return CellScan.ApplyAffineTransform(cells, DirectCast(transform, AffineTransform))
        End If
    End Function

    <ExportAPI("hungarian_assignment")>
    Public Function MakeHungarianAssignment(phase1 As CellScan(), phase2 As CellScan()) As Integer()
        Dim distanceMap As New DistanceMap(Of CellScan)(phase1, phase2, Function(a, b) a.DistanceTo(b))
        Dim assignMap As Integer() = HungarianAlgorithm.FindAssignments(distanceMap.GetMap)

        Return assignMap
    End Function

    <ExportAPI("greedy_matches")>
    Public Function cellGreedyMatches(sliceA As CellScan(), sliceB As CellScan()) As CellMatchResult()
        ' 创建细胞匹配器 
        Dim matcher As New CellMatcher(maxDistance:=50.0, distanceWeight:=0.6, morphologyWeight:=0.4)
        ' 执行匹配
        Dim matchResults As CellMatchResult() = matcher _
            .GreedyMatchCells(sliceA, sliceB) _
            .OrderByDescending(Function(x) x.MatchScore) _
            .ToArray

        ' 输出统计信息
        Call VBDebugger.EchoLine(matcher.GetMatchStatistics(matchResults, sliceA.Length, sliceB.Length))

        Return matchResults.ToArray
    End Function
End Module
