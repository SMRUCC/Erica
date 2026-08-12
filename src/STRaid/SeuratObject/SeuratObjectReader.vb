Imports System.IO
Imports SMRUCC.Rsharp.RDataSet
Imports SMRUCC.Rsharp.RDataSet.Convertor
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports RData = SMRUCC.Rsharp.RDataSet.Struct.RData

''' <summary>
''' Reads a Seurat object from GNU R .rda/.rds files.
''' The reading pipeline: file -> Reader.ParseData -> ConvertToR.ToRObject -> extract slots.
''' </summary>
Public Module SeuratObjectReader

    ''' <summary>
    ''' Read a Seurat object from a file path.
    ''' </summary>
    ''' <param name="filePath">Path to .rda or .rds file.</param>
    ''' <returns>A populated SeuratObject.</returns>
    Public Function ReadFile(filePath As String) As SeuratObject
        Using stream As Stream = File.OpenRead(filePath)
            Return ReadStream(stream)
        End Using
    End Function

    ''' <summary>
    ''' Read a Seurat object from a stream.
    ''' </summary>
    ''' <param name="stream">Stream containing rda/rds data.</param>
    ''' <returns>A populated SeuratObject.</returns>
    Public Function ReadStream(stream As Stream) As SeuratObject
        ' Step 1: Parse the RData binary format
        Console.Error.WriteLine("[SeuratObjectReader] Step 1: Parsing RData...")
        Dim rdata As RData = Reader.ParseData(stream)
        Console.Error.WriteLine("[SeuratObjectReader] Step 1: Done. Object type = " & rdata.object?.info.type.ToString())

        ' Step 2: Convert RObject tree to R# objects
        Console.Error.WriteLine("[SeuratObjectReader] Step 2: Converting to R# objects...")
        Dim rObj As Object = ConvertToR.ToRObject(rdata.object)
        Console.Error.WriteLine("[SeuratObjectReader] Step 2: Done. Type = " & If(rObj?.GetType().Name, "Nothing"))

        ' Step 3: Navigate to the Seurat object
        Console.Error.WriteLine("[SeuratObjectReader] Step 3: Locating Seurat list...")
        Dim seuratList As list = LocateSeuratList(rObj)
        If seuratList Is Nothing Then
            Throw New InvalidDataException("Could not locate Seurat object in the R data file.")
        End If

        ' Step 4: Build the SeuratObject from the R# list
        Return BuildSeuratObject(seuratList)
    End Function

    ''' <summary>
    ''' Navigate from the parsed R object to the Seurat list.
    ''' Handles both .rda (named container) and .rds (direct object) formats.
    ''' </summary>
    Private Function LocateSeuratList(rObj As Object) As list
        If TypeOf rObj Is list Then
            Dim topList As list = DirectCast(rObj, list)

            ' Check if this is already the Seurat object (has .class = "Seurat")
            Dim cls As Object = topList.getByName(".class")
            If cls IsNot Nothing Then
                Dim clsStr As String = cls.ToString()
                If clsStr = "Seurat" OrElse clsStr.Contains("Seurat") Then
                    Return topList
                End If
            End If

            ' .rda format: the top-level list contains named variables.
            ' Look for a slot whose value is a list with .class = "Seurat"
            Dim names As String() = topList.getNames
            If names IsNot Nothing Then
                For Each name As String In names
                    If name Is Nothing Then Continue For

                    Dim slotVal As Object = topList.getByName(name)
                    If TypeOf slotVal Is list Then
                        Dim innerList As list = DirectCast(slotVal, list)
                        Dim innerCls As Object = innerList.getByName(".class")
                        If innerCls IsNot Nothing Then
                            Dim innerClsStr As String = innerCls.ToString()
                            If innerClsStr = "Seurat" OrElse innerClsStr.Contains("Seurat") Then
                                Return innerList
                            End If
                        End If

                        ' Fallback: if .class extraction failed, check for
                        ' characteristic Seurat slot names
                        If innerList.getByName("assays") IsNot Nothing OrElse
                           innerList.getByName("meta.data") IsNot Nothing Then
                            Return innerList
                        End If
                    End If
                Next
            End If

            ' If we still haven't found it, check if topList itself has
            ' characteristic Seurat slots (for RDS where .class might be
            ' empty/malformed but the slots are correct)
            If topList.getByName("assays") IsNot Nothing OrElse
               topList.getByName("meta.data") IsNot Nothing Then
                Return topList
            End If
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Build a SeuratObject from an R# list that represents a Seurat S4 object.
    ''' </summary>
    Private Function BuildSeuratObject(seuratList As list) As SeuratObject
        Dim seurat As New SeuratObject()

        ' Extract version
        seurat.Version = TryGetVersion(seuratList)

        ' Extract active assay name
        seurat.ActiveAssay = TryGetString(seuratList, "active.assay")

        ' Extract assays (the most complex part)
        seurat.Assays = ExtractAssays(seuratList)

        ' Extract meta.data
        seurat.MetaData = ExtractMetaData(seuratList)

        ' Extract cell names
        seurat.CellNames = ExtractCellNames(seuratList, seurat.MetaData)

        ' Extract active.ident
        seurat.ActiveIdent = ExtractActiveIdent(seuratList)

        ' Extract reductions (PCA, UMAP, etc.)
        seurat.Reductions = ExtractReductions(seuratList)

        ' Extract images (spatial data)
        seurat.Images = ExtractImages(seuratList)

        Return seurat
    End Function

    ''' <summary>
    ''' Try to get the Seurat version string.
    ''' </summary>
    Private Function TryGetVersion(seuratList As list) As String
        Try
            Dim verObj As Object = seuratList.getByName("version")
            If verObj Is Nothing Then Return "unknown"

            If TypeOf verObj Is list Then
                Dim verList As list = DirectCast(verObj, list)
                ' Seurat v5 stores version as a list with $packageVersion
                Dim pkgVer As Object = verList.getByName("packageVersion")
                If pkgVer IsNot Nothing Then
                    Return pkgVer.ToString()
                End If
                ' Fallback: check for common version slots
                Dim major As Object = verList.getByName("major")
                Dim minor As Object = verList.getByName("minor")
                If major IsNot Nothing AndAlso minor IsNot Nothing Then
                    Return $"{major}.{minor}"
                End If
                Return verList.ToString()
            End If

            If TypeOf verObj Is vector Then
                Dim v As vector = DirectCast(verObj, vector)
                If v.length > 0 AndAlso v.data.Length > 0 Then
                    Return v.data.GetValue(0)?.ToString()
                End If
            End If

            Return verObj.ToString()
        Catch
            Return "unknown"
        End Try
    End Function

    ''' <summary>
    ''' Extract all assays from the Seurat object.
    ''' </summary>
    Private Function ExtractAssays(seuratList As list) As Dictionary(Of String, SeuratAssay)
        Dim result As New Dictionary(Of String, SeuratAssay)

        Dim assaysObj As Object = seuratList.getByName("assays")
        If assaysObj Is Nothing Then Return result
        If Not TypeOf assaysObj Is list Then Return result

        Dim assaysList As list = DirectCast(assaysObj, list)
        Dim assayNames As String() = assaysList.getNames

        ' The active assay name from the Seurat object (e.g. "RNA")
        Dim activeAssay As String = TryGetString(seuratList, "active.assay")

        ' Collect valid assay slots (filter out .class and empty names)
        Dim assaySlots As New List(Of KeyValuePair(Of String, Object))
        If assayNames IsNot Nothing Then
            For Each name As String In assayNames
                If name = ".class" Then Continue For
                Dim val As Object = assaysList.getByName(name)
                If TypeOf val Is list Then
                    assaySlots.Add(New KeyValuePair(Of String, Object)(name, val))
                End If
            Next
        End If

        ' If only one assay and name is numeric/default, use active assay name
        If assaySlots.Count = 1 AndAlso String.IsNullOrEmpty(assaySlots(0).Key) Then
            Dim assayName As String = If(Not String.IsNullOrEmpty(activeAssay), activeAssay, "RNA")
            Dim assayList As list = DirectCast(assaySlots(0).Value, list)
            Dim assay As SeuratAssay = BuildAssay(assayList, assayName)
            result.Add(assayName, assay)
        Else
            For Each kvp In assaySlots
                Dim assayName As String = kvp.Key
                If String.IsNullOrEmpty(assayName) Then assayName = $"Assay{result.Count + 1}"
                Dim assayList As list = DirectCast(kvp.Value, list)
                Dim assay As SeuratAssay = BuildAssay(assayList, assayName)
                result.Add(assayName, assay)
            Next
        End If

        Return result
    End Function

    ''' <summary>
    ''' Build a single SeuratAssay from its R# list representation.
    ''' </summary>
    Private Function BuildAssay(assayList As list, name As String) As SeuratAssay
        Dim assay As New SeuratAssay() With {
            .Name = name,
            .Key = TryGetString(assayList, "key")
        }

        ' Extract layers (Seurat v5: layers slot with possibly unnamed sub-slots)
        Dim layersObj As Object = assayList.getByName("layers")
        If TypeOf layersObj Is list Then
            Dim layersList As list = DirectCast(layersObj, list)
            Dim layerNames As String() = layersList.getNames
            Dim layerIndex As Integer = 0

            If layerNames IsNot Nothing Then
                For Each layerName As String In layerNames
                    If layerName = ".class" Then Continue For
                    Dim layerObj As Object = layersList.getByName(layerName)
                    If TypeOf layerObj Is list Then
                        Dim matrix As Double(,) = SparseListToMatrix(DirectCast(layerObj, list))
                        ' Assign by position: 0=counts, 1=data, 2=scale.data
                        Select Case layerIndex
                            Case 0 : assay.Counts = matrix
                            Case 1 : assay.Data = matrix
                            Case 2 : assay.ScaleData = matrix
                        End Select
                        layerIndex += 1
                    End If
                Next
            End If
        End If

        ' Fallback: try direct slots
        If assay.Counts Is Nothing Then assay.Counts = TryGetMatrix(assayList, "counts")
        If assay.Data Is Nothing Then assay.Data = TryGetMatrix(assayList, "data")
        If assay.ScaleData Is Nothing Then assay.ScaleData = TryGetMatrix(assayList, "scale.data")

        ' Extract variable features
        assay.VariableFeatures = ExtractVariableFeatures(assayList)

        ' Extract feature-level metadata
        assay.FeatureMetaData = ExtractFeatureMetaData(assayList)

        ' Extract feature names
        assay.FeatureNames = ExtractFeatureNames(assayList, assay)

        Return assay
    End Function

    ''' <summary>
    ''' Extract variable features from an assay.
    ''' </summary>
    Private Function ExtractVariableFeatures(assayList As list) As String()
        Dim varFeatObj As Object = assayList.getByName("var.features")
        If varFeatObj Is Nothing Then Return Nothing

        If TypeOf varFeatObj Is vector Then
            Dim v As vector = DirectCast(varFeatObj, vector)
            Return ToStringArray(v.data)
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extract feature-level metadata (e.g. vst.variance, vst.mean).
    ''' </summary>
    Private Function ExtractFeatureMetaData(assayList As list) As Dictionary(Of String, Array)
        Dim result As New Dictionary(Of String, Array)

        ' Seurat v5: feature metadata in "meta.features" slot
        Dim metaFeatObj As Object = assayList.getByName("meta.features")
        If metaFeatObj Is Nothing Then Return result
        If Not TypeOf metaFeatObj Is dataframe Then Return result

        Dim df As dataframe = DirectCast(metaFeatObj, dataframe)
        If df.columns IsNot Nothing Then
            For Each kvp In df.columns
                result(kvp.Key) = kvp.Value
            Next
        End If

        Return result
    End Function

    ''' <summary>
    ''' Extract feature (gene) names for an assay.
    ''' </summary>
    Private Function ExtractFeatureNames(assayList As list, assay As SeuratAssay) As String()
        ' First try: from the count matrix dimnames
        If assay.Counts IsNot Nothing AndAlso assay.FeatureNames Is Nothing Then
            ' Feature names are typically the rownames of the count matrix,
            ' but our matrix extraction doesn't return row/col names.
            ' Try to get them from meta.features rownames.
        End If

        ' Try to get from meta.features rownames
        Dim metaFeatObj As Object = assayList.getByName("meta.features")
        If TypeOf metaFeatObj Is dataframe Then
            Dim df As dataframe = DirectCast(metaFeatObj, dataframe)
            If df.rownames IsNot Nothing AndAlso df.rownames.Length > 0 Then
                Return df.rownames
            End If
        End If

        ' Try to get from "features" slot (older Seurat)
        Dim featuresObj As Object = assayList.getByName("features")
        If featuresObj IsNot Nothing Then
            Return ExtractFeatureNamesFromObject(featuresObj)
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extract feature names from various object types.
    ''' </summary>
    Private Function ExtractFeatureNamesFromObject(obj As Object) As String()
        If obj Is Nothing Then Return Nothing

        If TypeOf obj Is vector Then
            Dim v As vector = DirectCast(obj, vector)
            Return ToStringArray(v.data)
        End If

        If TypeOf obj Is list Then
            Dim l As list = DirectCast(obj, list)
            Dim names As String() = l.getNames
            If names IsNot Nothing AndAlso names.Length > 0 Then
                Return names
            End If
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extract meta.data (cell-level metadata).
    ''' </summary>
    Private Function ExtractMetaData(seuratList As list) As Dictionary(Of String, Array)
        Dim result As New Dictionary(Of String, Array)

        Dim metaObj As Object = seuratList.getByName("meta.data")
        If metaObj Is Nothing Then Return result
        If Not TypeOf metaObj Is dataframe Then Return result

        Dim df As dataframe = DirectCast(metaObj, dataframe)
        If df.columns IsNot Nothing Then
            For Each kvp In df.columns
                result(kvp.Key) = kvp.Value
            Next
        End If

        Return result
    End Function

    ''' <summary>
    ''' Extract cell barcode names.
    ''' </summary>
    Private Function ExtractCellNames(seuratList As list, metaData As Dictionary(Of String, Array)) As String()
        ' Try from meta.data rownames first
        Dim metaObj As Object = seuratList.getByName("meta.data")
        If TypeOf metaObj Is dataframe Then
            Dim df As dataframe = DirectCast(metaObj, dataframe)
            If df.rownames IsNot Nothing AndAlso df.rownames.Length > 0 Then
                Return df.rownames
            End If
        End If

        ' Try from active.ident names
        Dim identObj As Object = seuratList.getByName("active.ident")
        If TypeOf identObj Is vector Then
            Dim v As vector = DirectCast(identObj, vector)
            Dim vNames As String() = v.getNames()
            If vNames IsNot Nothing AndAlso vNames.Length > 0 Then
                Return vNames
            End If
        End If

        ' Fallback: if metaData has columns, get length from first column
        If metaData IsNot Nothing AndAlso metaData.Count > 0 Then
            Dim firstCol As Array = metaData.Values.FirstOrDefault()
            If firstCol IsNot Nothing Then
                Return Enumerable.Range(1, firstCol.Length) _
                    .Select(Function(i) $"Cell{i:000}") _
                    .ToArray()
            End If
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extract active.ident (cell cluster identities).
    ''' </summary>
    Private Function ExtractActiveIdent(seuratList As list) As String()
        Dim identObj As Object = seuratList.getByName("active.ident")
        If identObj Is Nothing Then Return Nothing

        If TypeOf identObj Is vector Then
            Dim v As vector = DirectCast(identObj, vector)
            ' If it's a factor, return factor levels as character
            If v.factor IsNot Nothing Then
                Return factor.asCharacter(v)
            End If
            Return ToStringArray(v.data)
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extract dimensionality reductions (PCA, UMAP, t-SNE, etc.).
    ''' </summary>
    Private Function ExtractReductions(seuratList As list) As Dictionary(Of String, DimReduction)
        Dim result As New Dictionary(Of String, DimReduction)

        Dim redObj As Object = seuratList.getByName("reductions")
        If redObj Is Nothing Then Return result
        If Not TypeOf redObj Is list Then Return result

        Dim redList As list = DirectCast(redObj, list)
        Dim redNames As String() = redList.getNames
        If redNames Is Nothing Then Return result

        Dim reductionIndex As Integer = 0
        For Each redName As String In redNames
            If redName = ".class" Then Continue For

            Dim redSlotObj As Object = redList.getByName(redName)
            If Not TypeOf redSlotObj Is list Then Continue For

            Dim redSlot As list = DirectCast(redSlotObj, list)
            Dim key As String = TryGetString(redSlot, "key")

            ' Infer name from key if slot name is empty/numeric
            Dim displayName As String = redName
            If String.IsNullOrEmpty(displayName) OrElse Integer.TryParse(displayName, Nothing) Then
                If Not String.IsNullOrEmpty(key) Then
                    ' key is like "PC_" or "UMAP_"
                    displayName = key.TrimEnd("_"c).ToLower()
                Else
                    displayName = $"Reduction{reductionIndex + 1}"
                End If
            End If

            Dim reduction As New DimReduction() With {
                .Name = displayName,
                .Method = TryGetString(redSlot, "method"),
                .Key = key
            }

            ' Extract cell.embeddings - may be flat vector or matrix
            reduction.CellEmbeddings = TryGetReductionMatrix(redSlot, "cell.embeddings")
            reduction.CellNames = TryGetRowNames(redSlot, "cell.embeddings")

            ' Extract feature.loadings (PCA rotation) - may be flat vector
            reduction.FeatureLoadings = TryGetReductionMatrix(redSlot, "feature.loadings")
            reduction.FeatureLoadingNames = TryGetRowNames(redSlot, "feature.loadings")

            ' Extract stdev (PCA only)
            reduction.Stdev = TryGetDoubleVector(redSlot, "stdev")

            ' Extract total variance (if available)
            Dim miscObj As Object = redSlot.getByName("misc")
            If TypeOf miscObj Is list Then
                Dim misc As list = DirectCast(miscObj, list)
                Dim totalVarObj As Object = misc.getByName("total.variance")
                If totalVarObj IsNot Nothing Then
                    Double.TryParse(totalVarObj.ToString(), reduction.TotalVariance)
                End If
            End If

            result.Add(displayName, reduction)
            reductionIndex += 1
        Next

        Return result
    End Function

    ''' <summary>
    ''' Extract spatial images.
    ''' </summary>
    Private Function ExtractImages(seuratList As list) As Dictionary(Of String, SeuratImage)
        Dim result As New Dictionary(Of String, SeuratImage)

        Dim imgObj As Object = seuratList.getByName("images")
        If imgObj Is Nothing Then Return result
        If Not TypeOf imgObj Is list Then Return result

        Dim imgList As list = DirectCast(imgObj, list)
        Dim imgNames As String() = imgList.getNames
        If imgNames Is Nothing Then Return result

        For Each imgName As String In imgNames
            If imgName = ".class" Then Continue For

            Dim imgSlotObj As Object = imgList.getByName(imgName)
            If Not TypeOf imgSlotObj Is list Then Continue For

            Dim imgSlot As list = DirectCast(imgSlotObj, list)
            Dim image As New SeuratImage() With {
                .Name = imgName
            }

            ' Extract coordinates
            image.Coordinates = TryGetCoordinates(imgSlot)

            ' Extract scale factors
            image.ScaleFactors = TryGetScaleFactors(imgSlot)

            result.Add(imgName, image)
        Next

        Return result
    End Function

    ' ========== Helper Functions ==========

    ''' <summary>
    ''' Try to get a string value from a list slot.
    ''' </summary>
    Private Function TryGetString(l As list, name As String) As String
        Try
            Dim obj As Object = l.getByName(name)
            If obj Is Nothing Then Return Nothing
            If TypeOf obj Is vector Then
                Dim v As vector = DirectCast(obj, vector)
                If v.length > 0 AndAlso v.data.Length > 0 Then
                    Return v.data.GetValue(0)?.ToString()
                End If
            End If
            Return obj.ToString()
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Try to extract a numeric matrix from a list slot.
    ''' Handles both dense matrices (dataframe) and sparse matrices (dgCMatrix list).
    ''' </summary>
    Private Function TryGetMatrix(l As list, name As String) As Double(,)
        Try
            Dim obj As Object = l.getByName(name)
            If obj Is Nothing Then Return Nothing

            ' Case 1: Dense matrix stored as dataframe
            If TypeOf obj Is dataframe Then
                Return DataframeToMatrix(DirectCast(obj, dataframe))
            End If

            ' Case 2: Sparse matrix (dgCMatrix) stored as list with i/p/x/Dim/Dimnames/factors
            If TypeOf obj Is list Then
                Dim matList As list = DirectCast(obj, list)
                ' Check if this looks like a dgCMatrix
                If matList.getByName("i") IsNot Nothing AndAlso
                   matList.getByName("p") IsNot Nothing AndAlso
                   matList.getByName("x") IsNot Nothing Then
                    Return SparseListToMatrix(matList)
                End If
            End If

            ' Case 3: Simple array
            If TypeOf obj Is Array Then
                Dim arr As Array = DirectCast(obj, Array)
                If arr.Rank = 2 Then
                    Return DirectCast(arr, Double(,))
                End If
                If arr.Rank = 1 Then
                    ' Treat as single-column matrix
                    Dim result As Double(,) = New Double(arr.Length - 1, 0) {}
                    For i As Integer = 0 To arr.Length - 1
                        result(i, 0) = Convert.ToDouble(arr.GetValue(i))
                    Next
                    Return result
                End If
            End If

            Return Nothing
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Extract a reduction matrix (cell.embeddings or feature.loadings) which
    ''' may be stored as a flat vector (n cells/features * n dimensions) with
    ''' dim attribute stored separately, or as a full matrix.
    ''' </summary>
    Private Function TryGetReductionMatrix(l As list, name As String) As Double(,)
        Try
            Dim obj As Object = l.getByName(name)
            If obj Is Nothing Then Return Nothing

            ' Case 1: Already a matrix (2D array or dataframe)
            If TypeOf obj Is dataframe Then
                Return DataframeToMatrix(DirectCast(obj, dataframe))
            End If

            If TypeOf obj Is Array Then
                Dim arr As Array = DirectCast(obj, Array)
                If arr.Rank = 2 Then
                    Return DirectCast(arr, Double(,))
                End If
            End If

            ' Case 2: Flat vector - try to reshape using the reduction's dim info
            ' The vector contains values in column-major order (R default).
            ' We need to know the number of cells (rows) and dimensions (cols).
            ' For PCA: ncols = stdev.length, for UMAP: ncols = 2.
            If TypeOf obj Is vector Then
                Dim v As vector = DirectCast(obj, vector)
                If v.data Is Nothing OrElse v.data.Length = 0 Then Return Nothing

                Dim nTotal As Integer = v.data.Length

                ' Try to determine dimensions
                Dim nCols As Integer = 0
                ' For PCA, stdev length = ncols
                Dim stdevObj As Object = l.getByName("stdev")
                If TypeOf stdevObj Is vector Then
                    Dim stdevV As vector = DirectCast(stdevObj, vector)
                    If stdevV.data IsNot Nothing Then
                        nCols = stdevV.data.Length
                    End If
                End If

                ' For UMAP/t-SNE, key suggests 2D
                If nCols = 0 Then
                    Dim key As String = TryGetString(l, "key")
                    If Not String.IsNullOrEmpty(key) Then
                        If key.StartsWith("UMAP", StringComparison.OrdinalIgnoreCase) OrElse
                           key.StartsWith("TSNE", StringComparison.OrdinalIgnoreCase) OrElse
                           key.StartsWith("tSNE", StringComparison.OrdinalIgnoreCase) Then
                            nCols = 2
                        End If
                    End If
                End If

                ' Default: assume 2 columns
                If nCols = 0 Then nCols = 2

                Dim nRows As Integer = nTotal \ nCols

                If nRows * nCols <> nTotal Then
                    ' Mismatch - try swapping
                    Dim temp As Integer = nRows
                    nRows = nCols
                    nCols = temp
                    If nRows * nCols <> nTotal Then
                        Return Nothing
                    End If
                End If

                Dim result As Double(,) = New Double(nRows - 1, nCols - 1) {}
                ' R stores column-major: element [i, j] is at position i + j * nRows
                For j As Integer = 0 To nCols - 1
                    For i As Integer = 0 To nRows - 1
                        result(i, j) = Convert.ToDouble(v.data.GetValue(i + j * nRows))
                    Next
                Next

                Return result
            End If

            Return Nothing
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Convert an R# dataframe to a 2D Double matrix.
    ''' </summary>
    Private Function DataframeToMatrix(df As dataframe) As Double(,)
        If df Is Nothing OrElse df.columns Is Nothing OrElse df.columns.Count = 0 Then
            Return Nothing
        End If

        Dim nCols As Integer = df.ncols
        Dim nRows As Integer = df.nrows

        If nRows = 0 OrElse nCols = 0 Then Return Nothing

        Dim result As Double(,) = New Double(nRows - 1, nCols - 1) {}
        Dim colNames As String() = df.colnames

        For j As Integer = 0 To nCols - 1
            Dim colData As Array = df.columns(colNames(j))
            If colData Is Nothing Then Continue For

            For i As Integer = 0 To Math.Min(nRows, colData.Length) - 1
                Dim val As Object = colData.GetValue(i)
                If val IsNot Nothing Then
                    result(i, j) = Convert.ToDouble(val)
                End If
            Next
        Next

        Return result
    End Function

    ''' <summary>
    ''' Convert a dgCMatrix sparse matrix (R list with i/p/x/Dim) to a dense 2D array.
    ''' dgCMatrix is in CSC (Compressed Sparse Column) format:
    '''   i: row indices (0-based) of non-zero entries
    '''   p: column pointers (indices into i and x where each column starts)
    '''   x: values of non-zero entries
    '''   Dim: integer vector of length 2 (nrow, ncol)
    ''' </summary>
    Private Function SparseListToMatrix(matList As list) As Double(,)
        Try
            ' Get dimensions
            Dim dimObj As Object = matList.getByName("Dim")
            If dimObj Is Nothing Then Return Nothing

            Dim dims As Integer() = Nothing
            If TypeOf dimObj Is vector Then
                Dim v As vector = DirectCast(dimObj, vector)
                If v.data IsNot Nothing AndAlso v.data.Length >= 2 Then
                    dims = New Integer(1) {}
                    dims(0) = Convert.ToInt32(v.data.GetValue(0))
                    dims(1) = Convert.ToInt32(v.data.GetValue(1))
                End If
            ElseIf TypeOf dimObj Is Array Then
                Dim arr As Array = DirectCast(dimObj, Array)
                If arr.Length >= 2 Then
                    dims = New Integer(1) {}
                    dims(0) = Convert.ToInt32(arr.GetValue(0))
                    dims(1) = Convert.ToInt32(arr.GetValue(1))
                End If
            End If

            If dims Is Nothing Then Return Nothing
            Dim nRow As Integer = dims(0)
            Dim nCol As Integer = dims(1)

            ' Get p (column pointers)
            Dim pObj As Object = matList.getByName("p")
            Dim p As Integer() = TryGetIntArray(pObj)
            If p Is Nothing OrElse p.Length < nCol + 1 Then Return Nothing

            ' Get i (row indices)
            Dim iObj As Object = matList.getByName("i")
            Dim i As Integer() = TryGetIntArray(iObj)
            If i Is Nothing Then Return Nothing

            ' Get x (values)
            Dim xObj As Object = matList.getByName("x")
            Dim x As Double() = TryGetDoubleArray(xObj)
            If x Is Nothing Then Return Nothing

            ' Build dense matrix
            Dim result As Double(,) = New Double(nRow - 1, nCol - 1) {}

            For col As Integer = 0 To nCol - 1
                Dim startIdx As Integer = p(col)
                Dim endIdx As Integer = p(col + 1)

                For idx As Integer = startIdx To endIdx - 1
                    If idx < i.Length AndAlso idx < x.Length Then
                        Dim row As Integer = i(idx)
                        If row < nRow Then
                            result(row, col) = x(idx)
                        End If
                    End If
                Next
            Next

            Return result
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Try to extract row names from a matrix slot (from its dimnames attribute).
    ''' </summary>
    Private Function TryGetRowNames(l As list, matrixName As String) As String()
        ' Row names are typically stored as dimnames[[1]] in R matrices.
        ' In the R# representation, we may not have direct access to dimnames.
        ' For now, return Nothing; the caller can derive from other sources.
        Return Nothing
    End Function

    ''' <summary>
    ''' Try to get a Double vector from a list slot.
    ''' </summary>
    Private Function TryGetDoubleVector(l As list, name As String) As Double()
        Try
            Dim obj As Object = l.getByName(name)
            Return TryGetDoubleArray(obj)
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Convert an object to an Integer array.
    ''' </summary>
    Private Function TryGetIntArray(obj As Object) As Integer()
        If obj Is Nothing Then Return Nothing

        If TypeOf obj Is vector Then
            Dim v As vector = DirectCast(obj, vector)
            If v.data Is Nothing Then Return Nothing
            Dim result As Integer() = New Integer(v.data.Length - 1) {}
            For idx As Integer = 0 To v.data.Length - 1
                result(idx) = Convert.ToInt32(v.data.GetValue(idx))
            Next
            Return result
        End If

        If TypeOf obj Is Array Then
            Dim arr As Array = DirectCast(obj, Array)
            Dim result As Integer() = New Integer(arr.Length - 1) {}
            For idx As Integer = 0 To arr.Length - 1
                result(idx) = Convert.ToInt32(arr.GetValue(idx))
            Next
            Return result
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Convert an object to a Double array.
    ''' </summary>
    Private Function TryGetDoubleArray(obj As Object) As Double()
        If obj Is Nothing Then Return Nothing

        If TypeOf obj Is vector Then
            Dim v As vector = DirectCast(obj, vector)
            If v.data Is Nothing Then Return Nothing
            Dim result As Double() = New Double(v.data.Length - 1) {}
            For idx As Integer = 0 To v.data.Length - 1
                result(idx) = Convert.ToDouble(v.data.GetValue(idx))
            Next
            Return result
        End If

        If TypeOf obj Is Array Then
            Dim arr As Array = DirectCast(obj, Array)
            Dim result As Double() = New Double(arr.Length - 1) {}
            For idx As Integer = 0 To arr.Length - 1
                result(idx) = Convert.ToDouble(arr.GetValue(idx))
            Next
            Return result
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Convert an Array to a String array.
    ''' </summary>
    Private Function ToStringArray(arr As Array) As String()
        If arr Is Nothing Then Return Nothing
        Dim result As String() = New String(arr.Length - 1) {}
        For i As Integer = 0 To arr.Length - 1
            Dim val As Object = arr.GetValue(i)
            result(i) = If(val?.ToString(), "")
        Next
        Return result
    End Function

    ''' <summary>
    ''' Try to extract spatial coordinates from an image slot.
    ''' </summary>
    Private Function TryGetCoordinates(imgSlot As list) As Dictionary(Of String, Double())
        Dim result As New Dictionary(Of String, Double())

        ' Try "coordinates" slot
        Dim coordObj As Object = imgSlot.getByName("coordinates")
        If TypeOf coordObj Is dataframe Then
            Dim df As dataframe = DirectCast(coordObj, dataframe)
            If df.columns IsNot Nothing AndAlso df.rownames IsNot Nothing Then
                For Each kvp In df.columns
                    Dim vals As Double() = New Double(kvp.Value.Length - 1) {}
                    For i As Integer = 0 To kvp.Value.Length - 1
                        vals(i) = Convert.ToDouble(kvp.Value.GetValue(i))
                    Next
                    result(kvp.Key) = vals
                Next
            End If
        End If

        Return If(result.Count > 0, result, Nothing)
    End Function

    ''' <summary>
    ''' Try to extract scale factors from an image slot.
    ''' </summary>
    Private Function TryGetScaleFactors(imgSlot As list) As Dictionary(Of String, Double)
        Dim result As New Dictionary(Of String, Double)

        Dim sfObj As Object = imgSlot.getByName("scale.factors")
        If sfObj Is Nothing Then sfObj = imgSlot.getByName("scaleFactors")

        If TypeOf sfObj Is list Then
            Dim sfList As list = DirectCast(sfObj, list)
            Dim sfNames As String() = sfList.getNames
            If sfNames IsNot Nothing Then
                For Each sfName As String In sfNames
                    If sfName = ".class" Then Continue For
                    Dim valObj As Object = sfList.getByName(sfName)
                    If valObj IsNot Nothing Then
                        Dim d As Double = 0
                        If Double.TryParse(valObj.ToString(), d) Then
                            result(sfName) = d
                        End If
                    End If
                Next
            End If
        End If

        Return If(result.Count > 0, result, Nothing)
    End Function

End Module
