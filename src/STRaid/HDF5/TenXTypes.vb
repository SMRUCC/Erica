' 10x Genomics Visium HD 原始 HDF5 数据结构类型定义
'
' 本文件定义读取 10x Genomics 的 feature_slice.h5 / molecule_info.h5 之后
' 所承载的结构化 .NET 对象。这些类型作为 STRaid 上层读取封装与
' 基础 HDF5 模块（Microsoft.VisualBasic.Data.IO.HDF5）之间的适配层使用，
' 不直接依赖基础模块的解析细节（仅通过 HDF5File / HDF5Reader 获取数据数组）。
'
' 命名空间与 STRaid 项目 (RootNamespace = Erica.Analysis.SpatialTissue.RaidData) 保持一致。

Imports Microsoft.VisualBasic.Data.IO.HDF5
Imports Microsoft.VisualBasic.Data.IO.HDF5.struct

Namespace Erica.Analysis.SpatialTissue.RaidData.HDF5

    ''' <summary>
    ''' CSR（Compressed Sparse Row）格式的三元组稀疏表达矩阵。
    ''' 与 scipy.sparse.csr_matrix 的内存布局一致：
    '''   data(indptr[i] : indptr[i+1]) 为第 i 行的非零元素值；
    '''   indices(indptr[i] : indptr[i+1]) 为对应的列（特征）下标。
    ''' </summary>
    Public Class HDF5SparseMatrix

        ''' <summary>
        ''' 非零元素值（行优先压缩）。
        ''' </summary>
        Public Property data As Double()

        ''' <summary>
        ''' 每个非零元素对应的列（特征）下标。
        ''' </summary>
        Public Property indices As Integer()

        ''' <summary>
        ''' 每一行在 data / indices 中的起始偏移，长度为 nRows + 1。
        ''' </summary>
        Public Property indptr As Integer()

        ''' <summary>
        ''' 矩阵形状 [nRows, nCols]，对应 (nSpots, nFeatures)。
        ''' </summary>
        Public Property shape As Integer()

        Public ReadOnly Property nRows As Integer
            Get
                If shape Is Nothing OrElse shape.Length < 1 Then
                    Return 0
                End If
                Return shape(0)
            End Get
        End Property

        Public ReadOnly Property nCols As Integer
            Get
                If shape Is Nothing OrElse shape.Length < 2 Then
                    Return 0
                End If
                Return shape(1)
            End Get
        End Property

        Public ReadOnly Property nnz As Integer
            Get
                If data Is Nothing Then
                    Return 0
                End If
                Return data.Length
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"CSR[{nRows} x {nCols}] nnz={nnz}"
        End Function
    End Class

    ''' <summary>
    ''' 单个基因/特征（feature）的元数据。
    ''' </summary>
    Public Class FeatureMeta

        ''' <summary>
        ''' 特征唯一 id（通常为 Ensembl id，如 ENSMUSG...）。
        ''' </summary>
        Public Property id As String

        ''' <summary>
        ''' 特征显示名（gene symbol）。
        ''' </summary>
        Public Property name As String

        ''' <summary>
        ''' 特征类型，如 Gene Expression。
        ''' </summary>
        Public Property featureType As String

        ''' <summary>
        ''' 所属基因组，如 GRCm39。
        ''' </summary>
        Public Property genome As String
    End Class

    ''' <summary>
    ''' Visium HD 的空间坐标信息。
    ''' </summary>
    Public Class SpatialCoords

        ''' <summary>
        ''' 分辨率名称（如 "hires" / "lowres" / "tessellation"）。
        ''' </summary>
        Public Property resolutions As String()

        ''' <summary>
        ''' 每个 spot 的局部图像坐标 [nSpots, 2]（x, y）。
        ''' </summary>
        Public Property localCoordinates As Double(,)

        ''' <summary>
        ''' tessellation（六边形 bin 网格）坐标 [nSpots, 2]。
        ''' </summary>
        Public Property tessellationCoordinates As Double(,)
    End Class

    ''' <summary>
    ''' feature_slice.h5 解析后的完整结果。
    ''' </summary>
    Public Class FeatureSliceData

        ''' <summary>
        ''' 每个 spot / barcode 的标识。
        ''' </summary>
        Public Property barcodes As String()

        ''' <summary>
        ''' 特征（基因）元数据表。
        ''' </summary>
        Public Property features As FeatureMeta()

        ''' <summary>
        ''' CSR 压缩表达矩阵。
        ''' </summary>
        Public Property matrix As HDF5SparseMatrix

        ''' <summary>
        ''' 基础数学模块承载的稀疏表达矩阵（COO 三元组直接构造），维度 [nBins, nFeatures]。
        ''' 与 <see cref="matrix"/> 表达同一份数据，但使用 <see cref="Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix"/>，
        ''' 便于上层直接进行线性代数运算。
        ''' </summary>
        Public Property sparseMatrix As Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix

        ''' <summary>
        ''' 空间坐标信息（可能为 Nothing）。
        ''' </summary>
        Public Property spatial As SpatialCoords
    End Class

    ''' <summary>
    ''' molecule_info.h5 解析后的完整结果。
    ''' molecule 级（单分子）字段，长度均为分子数 N。
    ''' </summary>
    Public Class MoleculeInfoData

        ''' <summary>
        ''' 每个分子对应的 barcode。
        ''' </summary>
        Public Property barcode As String()

        ''' <summary>
        ''' 每个分子所在的染色体。
        ''' </summary>
        Public Property chromosome As String()

        ''' <summary>
        ''' 每个分子对应的特征下标（指向 feature_slice 的 features）。
        ''' </summary>
        Public Property featureIndex As Integer()

        ''' <summary>
        ''' GEM group（通常全为 1）。
        ''' </summary>
        Public Property gemGroup As Integer()

        ''' <summary>
        ''' 每个分子所属基因组。
        ''' </summary>
        Public Property genome As String()

        ''' <summary>
        ''' 探针序列（仅在探针捕获实验中存在，否则为空）。
        ''' </summary>
        Public Property probe As String()

        ''' <summary>
        ''' 是否为控制读数（0/1）。
        ''' </summary>
        Public Property control As Integer()

        ''' <summary>
        ''' split index（分块下标）。
        ''' </summary>
        Public Property splitIndex As Integer()

        ''' <summary>
        ''' 每个分子的空间 x 坐标（可能为 Nothing）。
        ''' </summary>
        Public Property spatialX As Single()

        ''' <summary>
        ''' 每个分子的空间 y 坐标（可能为 Nothing）。
        ''' </summary>
        Public Property spatialY As Single()

        Public ReadOnly Property moleculeCount As Integer
            Get
                If barcode Is Nothing Then
                    Return 0
                End If
                Return barcode.Length
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"MoleculeInfo[N={moleculeCount}]"
        End Function
    End Class

    ''' <summary>
    ''' HDF5 读取辅助：将基础模块返回的 Object（通常是 Array）转换为强类型的一维数组。
    ''' 基础模块对 1-D 数据集返回具体元素类型的一维数组；对 2-D 返回矩形数组。
    ''' 这里统一做防御式拷贝，确保上层拿到长度为 N 的一维数组。
    ''' </summary>
    Public Module Hdf5ArrayHelpers

        ''' <summary>
        ''' 将 <paramref name="obj"/> 转换/拷贝为长度 N 的一维数组 T()。
        ''' 支持直接是 1-D T() 与矩形数组两种情况。
        ''' </summary>
        Public Function AsVector(Of T)(obj As Object) As T()
            If obj Is Nothing Then
                Return Nothing
            End If

            Dim arr As Array = DirectCast(obj, Array)

            If arr.Rank = 1 Then
                If TypeOf arr Is T() Then
                    Return DirectCast(arr, T())
                End If

                Dim n As Integer = arr.Length
                Dim out As T() = New T(n - 1) {}
                Array.Copy(arr, out, n)
                Return out
            Else
                ' 矩形数组：按行优先展平为一维
                Dim lengths As Integer() = Enumerable _
                    .Range(0, arr.Rank) _
                    .Select(Function(d) arr.GetLength(d)) _
                    .ToArray
                Dim total As Integer = lengths.Aggregate(1, Function(a, b) a * b)
                Dim out As T() = New T(total - 1) {}
                Dim idx(lengths.Length - 1) As Integer
                For i As Integer = 0 To total - 1
                    out(i) = DirectCast(arr.GetValue(idx), T)
                    ' 进位
                    For d As Integer = lengths.Length - 1 To 0 Step -1
                        idx(d) += 1
                        If idx(d) < lengths(d) Then
                            Exit For
                        End If
                        idx(d) = 0
                    Next
                Next
                Return out
            End If
        End Function

        ''' <summary>
        ''' 将 <paramref name="obj"/> 转换/拷贝为 2-D 矩形数组 T(,)。
        ''' </summary>
        Public Function AsMatrix(Of T)(obj As Object) As T(,)
            If obj Is Nothing Then
                Return Nothing
            End If

            Dim arr As Array = DirectCast(obj, Array)

            If arr.Rank = 2 Then
                Dim rows As Integer = arr.GetLength(0)
                Dim cols As Integer = arr.GetLength(1)
                Dim out(rows - 1, cols - 1) As T

                For i As Integer = 0 To rows - 1
                    For j As Integer = 0 To cols - 1
                        out(i, j) = DirectCast(arr.GetValue(i, j), T)
                    Next
                Next
                Return out
            ElseIf arr.Rank = 1 Then
                ' 单列向量 → 单列矩形数组
                Dim n As Integer = arr.Length
                Dim out(n - 1, 0) As T
                For i As Integer = 0 To n - 1
                    out(i, 0) = DirectCast(arr.GetValue(i), T)
                Next
                Return out
            Else
                Throw New NotSupportedException("Only 1-D/2-D arrays are supported by AsMatrix")
            End If
        End Function
    End Module

    ''' <summary>
    ''' molecule_info.h5 流式聚合后的 UMI 计数稀疏矩阵结果。
    ''' 矩阵使用基础数学模块 <see cref="Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix"/>
    ''' （COO 三元组构造的稀疏表达矩阵），维度为 [nBarcodes, nFeatures]。
    ''' </summary>
    Public Class MoleculeInfoMatrix

        ''' <summary>
        ''' 每个 barcode（spot）的标识，长度 = 矩阵行数。
        ''' </summary>
        Public Property barcodes As String()

        ''' <summary>
        ''' 特征（基因）元数据表，长度 = 矩阵列数。
        ''' </summary>
        Public Property features As FeatureMeta()

        ''' <summary>
        ''' UMI 计数稀疏矩阵，维度 [nBarcodes, nFeatures]。
        ''' 以 (barcode_idx, feature_idx) 为坐标，值为该 spot × gene 的 UMI 累加和。
        ''' </summary>
        Public Property matrix As Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix

        ''' <summary>
        ''' 参与聚合的分子总数（原始 molecule_info 行数）。
        ''' </summary>
        Public Property moleculeCount As Long

        Public Overrides Function ToString() As String
            Dim dims As String = If(matrix Is Nothing, "?", $"{matrix.RowDimension} x {matrix.ColumnDimension}")
            Return $"MoleculeInfoMatrix[{dims}] molecules={moleculeCount}"
        End Function
    End Class

    ''' <summary>
    ''' 统一的 Visium HD 解析结果入口。
    ''' 由 <see cref="TenXReader.OpenVisiumHD"/> 按文件结构自动分流构造：
    '''   - 含 <c>feature_slices</c> 组 → <see cref="FeatureSliceData"/>（基础稀疏矩阵承载表达量）；
    '''   - 含 <c>barcode_idx</c> 扁平表 → <see cref="MoleculeInfoMatrix"/>（UMI 聚合稀疏矩阵）。
    ''' </summary>
    Public Class VisiumHDResult

        ''' <summary>
        ''' 解析到的文件类型。
        ''' </summary>
        Public Property kind As VisiumHDKind

        ''' <summary>
        ''' feature_slice.h5 的结果（仅当 <see cref="kind"/> = FeatureSlice 时非空）。
        ''' 每个元素是一个分片（feature_slices 组下的一个子组），共享同一套 barcodes / features 元数据。
        ''' </summary>
        Public Property featureSlice As List(Of FeatureSliceData)

        ''' <summary>
        ''' molecule_info.h5 的结果（仅当 <see cref="kind"/> = MoleculeInfo 时非空）。
        ''' </summary>
        Public Property moleculeInfo As MoleculeInfoMatrix

        Public ReadOnly Property barcodes As String()
            Get
                If featureSlice IsNot Nothing AndAlso featureSlice.Count > 0 Then
                    Return featureSlice(0).barcodes
                ElseIf moleculeInfo IsNot Nothing Then
                    Return moleculeInfo.barcodes
                End If
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property features As FeatureMeta()
            Get
                If featureSlice IsNot Nothing AndAlso featureSlice.Count > 0 Then
                    Return featureSlice(0).features
                ElseIf moleculeInfo IsNot Nothing Then
                    Return moleculeInfo.features
                End If
                Return Nothing
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"VisiumHDResult[{kind}]"
        End Function
    End Class

    ''' <summary>
    ''' Visium HD 文件类型枚举。
    ''' </summary>
    Public Enum VisiumHDKind
        ''' <summary>未知 / 无法识别的结构。</summary>
        Unknown
        ''' <summary>feature_slice.h5：按切片分组的三元组表达矩阵。</summary>
        FeatureSlice
        ''' <summary>molecule_info.h5：扁平分子表，需聚合为 UMI 矩阵。</summary>
        MoleculeInfo
    End Enum
End Namespace
