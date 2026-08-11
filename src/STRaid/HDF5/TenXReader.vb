' 10x Genomics Visium HD HDF5 流式稀疏读取封装层
'
' 本文件在基础 HDF5 模块（Microsoft.VisualBasic.Data.IO.HDF5，含 HDF5Sparse 流式分块 API）
' 之上按 10x 语义封装两种文件结构：
'   - feature_slice.h5：feature_slices/<id>/{row,col,data} 三元组分片表达矩阵（流式逐 slice 返回）；
'   - molecule_info.h5：barcode_idx / feature_idx / count 扁平分子表，流式分治聚合为 UMI 稀疏矩阵。
'
' 所有分块数据集均通过 HDF5Sparse.EnumerateChunkArrays 按 chunk 流式解码，避免一次性解压整块到内存；
' 大矩阵用 Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix（COO 三元组构造）承载，
' 稠密化约 940GB 的表达矩阵因此以稀疏形式常驻内存（约数 GB）。

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.IO.HDF5
Imports Microsoft.VisualBasic.Data.IO.HDF5.Microsoft.VisualBasic.Data.IO.HDF5
Imports Microsoft.VisualBasic.Data.IO.HDF5.struct
Imports Microsoft.VisualBasic.Language

Namespace HDF5

    ''' <summary>
    ''' Visium HD 两种 10x HDF5 文件的统一流式稀疏读取入口。
    ''' </summary>
    Public Module TenXReader

        ''' <summary>
        ''' 打开一个 Visium HD 的 10x HDF5 文件并自动分流解析。
        ''' 内部按文件根结构探测：
        '''   - 含 <c>feature_slices</c> 组 → 按 feature_slice 解析；
        '''   - 含 <c>barcode_idx</c> 数据集 → 按 molecule_info 解析并聚合成 UMI 矩阵。
        ''' </summary>
        ''' <param name="filePath">h5 文件路径。</param>
        ''' <returns>包含识别类型与对应结构化结果（基础 SparseMatrix + barcodes + features）的统一对象。</returns>
        Public Function OpenVisiumHD(filePath As String) As VisiumHDResult
            Using h5 As New HDF5File(filePath)
                Dim kind As VisiumHDKind = DetectKind(h5)

                Select Case kind
                    Case VisiumHDKind.FeatureSlice
                        ' ReadFeatureSlices 是流式 Iterator，捕获了 h5；必须在 Using 作用域内消费，
                        ' 否则 Using 释放 h5 后迭代会访问已关闭的文件句柄。此处物化分片集合，
                        ' 单个分片非零数很小（约 1~2 万），总内存仍维持在稀疏量级，不会 OOM。
                        Dim slices As New List(Of FeatureSliceData)(ReadFeatureSlices(h5))
                        Return New VisiumHDResult With {
                            .kind = kind,
                            .featureSlice = slices
                        }
                    Case VisiumHDKind.MoleculeInfo
                        Return New VisiumHDResult With {
                            .kind = kind,
                            .moleculeInfo = ReadMoleculeInfo(h5)
                        }
                    Case Else
                        Throw New InvalidOperationException($"无法识别的 Visium HD 文件结构：{filePath}")
                End Select
            End Using
        End Function

        ''' <summary>
        ''' 探测文件类型：优先判定 feature_slice（含 feature_slices 组），其次 molecule_info（含 barcode_idx 数据集）。
        ''' </summary>
        Public Function DetectKind(h5 As HDF5File) As VisiumHDKind
            If TryGetObject(h5, "feature_slices") IsNot Nothing Then
                Return VisiumHDKind.FeatureSlice
            ElseIf TryGetObject(h5, "barcode_idx") IsNot Nothing Then
                Return VisiumHDKind.MoleculeInfo
            Else
                Return VisiumHDKind.Unknown
            End If
        End Function

        ''' <summary>
        ''' 安全获取对象：路径不存在时返回 Nothing 而非抛异常。
        ''' </summary>
        Private Function TryGetObject(h5 As HDF5File, path As String) As HDF5Reader
            Try
                Return h5.GetObject(path)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' 读取一个字符串数据集（如 barcodes / features/id）为 String()。
        ''' </summary>
        Private Function ReadStringVector(h5 As HDF5File, path As String) As String()
            ' 使用安全查找：HDF5File.GetObject 在根组中找不到符号名时会抛 LINQ "no matching element"，
            ' 这里改用 TryGetObject 捕获后返回 Nothing，便于上层给出明确的“数据集缺失”诊断。
            Dim reader As HDF5Reader = TryGetObject(h5, path)

            If reader Is Nothing OrElse reader.dataset Is Nothing Then
                Return Nothing
            End If

            Dim arr As Object = reader.dataset.data(reader.superblock)
            Return Hdf5ArrayHelpers.AsVector(Of String)(arr)
        End Function

        ''' <summary>
        ''' 读取 features 组（复合类型拆分为 id / name / genome / feature_type 四个子数据集）为 FeatureMeta()。
        ''' </summary>
        Private Function ReadFeatures(h5 As HDF5File) As FeatureMeta()
            Dim ids As String() = ReadStringVector(h5, "features/id")
            Dim names As String() = ReadStringVector(h5, "features/name")
            Dim genomes As String() = ReadStringVector(h5, "features/genome")
            Dim types As String() = ReadStringVector(h5, "features/feature_type")

            If ids Is Nothing Then
                Return Nothing
            End If

            Dim n As Integer = ids.Length
            Dim out As FeatureMeta() = New FeatureMeta(n - 1) {}

            For i As Integer = 0 To n - 1
                out(i) = New FeatureMeta With {
                    .id = ids(i),
                    .name = If(names IsNot Nothing AndAlso i < names.Length, names(i), ""),
                    .genome = If(genomes IsNot Nothing AndAlso i < genomes.Length, genomes(i), ""),
                    .featureType = If(types IsNot Nothing AndAlso i < types.Length, types(i), "")
                }
            Next

            Return out
        End Function

        ''' <summary>
        ''' 读取 barcodes 字符串数组（|S43 定长字符串数据集）。
        ''' </summary>
        Private Function ReadBarcodes(h5 As HDF5File) As String()
            Return ReadStringVector(h5, "barcodes")
        End Function

        ''' <summary>
        ''' 解析 feature_slice.h5：逐级枚举 feature_slices 下的分片子组，对每个分片用
        ''' COO 三元组（row / col / data）流式构造稀疏矩阵。每个分片独立产出一份
        ''' FeatureSliceData，元数据（barcodes / features）在所有分片间共享。
        ''' </summary>
        ''' <remarks>
        ''' 采用流式逐分片 yield，单个分片的非零数很小（约 1~2 万），内存峰值维持在单分片量级，
        ''' 避免一次性物化所有分片导致的 OOM。
        ''' </remarks>
        Public Iterator Function ReadFeatureSlices(h5 As HDF5File) As IEnumerable(Of FeatureSliceData)
            Dim groupReader As HDF5Reader = h5.GetObject("feature_slices")
            Dim barcodes As String() = ReadBarcodes(h5)
            Dim features As FeatureMeta() = ReadFeatures(h5)

            If groupReader Is Nothing OrElse groupReader.dataGroup Is Nothing Then
                Throw New InvalidOperationException("feature_slices 组不存在或无法解析。")
            End If

            ' 枚举 feature_slices 下的分片子组名（如 "0", "1", ...）
            Dim sliceNames As String() = groupReader.dataGroup.objects _
                .Select(Function(o) o.symbolName) _
                .OrderBy(Function(s) s) _
                .ToArray

            For Each sliceId As String In sliceNames
                Dim rowPath As String = $"feature_slices/{sliceId}/row"
                Dim colPath As String = $"feature_slices/{sliceId}/col"
                Dim dataPath As String = $"feature_slices/{sliceId}/data"

                ' row / col 为 uint32（UInteger），data 为表达量（float32/uint32 -> Double）。
                ' 用 UInteger 索引 + UInteger 值（表达量以 Double 承载）。
                Dim sliceMatrix As Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix =
                    HDF5Sparse.GetSparseMatrixFromTriplets(Of UInteger, UInteger)(
                        h5, rowPath, colPath, dataPath)

                Yield New FeatureSliceData With {
                    .barcodes = barcodes,
                    .features = features,
                    .sparseMatrix = sliceMatrix,
                    .matrix = Nothing,
                    .spatial = Nothing
                }
            Next
        End Function

        ''' <summary>
        ''' 解析 molecule_info.h5：流式遍历 barcode_idx（uint64）/ feature_idx（uint32）/ count（uint32）
        ''' 三个扁平分块数组，按 (barcode, feature) 累加 UMI count，最终一次性构造稀疏矩阵。
        ''' </summary>
        ''' <remarks>
        ''' 聚合使用 Dictionary(Of Long, Double)，键 = barcode * nFeatures + feature。分块枚举保证内存峰值
        ''' 维持在单 chunk 量级；聚合字典仅保存非零 (barcode, feature) 项，远小于 5.07 亿原始分子行。
        ''' </remarks>
        Public Function ReadMoleculeInfo(h5 As HDF5File) As MoleculeInfoMatrix
            Dim barcodes As String() = ReadBarcodes(h5)
            Dim features As FeatureMeta() = ReadFeatures(h5)

            If barcodes Is Nothing Then
                Throw New InvalidOperationException("molecule_info.h5 缺少 barcodes 数据集。")
            End If
            If features Is Nothing Then
                Throw New InvalidOperationException("molecule_info.h5 缺少 features 组。")
            End If

            Dim nFeatures As Integer = features.Length
            Dim nBarcodes As Integer = barcodes.Length

            ' 安全范围校验：barcode_idx 为 uint64，key = barcode * nFeatures + feature 须落在 Long 范围内。
            If CLng(nBarcodes) * CLng(nFeatures) > Long.MaxValue Then
                Throw New OverflowException("barcode × feature 维度溢出 64 位 key 范围。")
            End If

            ' 聚合字典：键 = barcode * nFeatures + feature
            Dim agg As New Dictionary(Of Long, Double)
            Dim barcodeEnum As IEnumerable(Of ULong()) = h5.EnumerateChunkArrays(Of ULong)("barcode_idx")
            Dim featureEnum As IEnumerable(Of UInteger()) = h5.EnumerateChunkArrays(Of UInteger)("feature_idx")
            Dim countEnum As IEnumerable(Of UInteger()) = h5.EnumerateChunkArrays(Of UInteger)("count")

            Dim nProcessed As Long = 0L
            Dim stride As Long = 5_000_000L

            Using bIter = barcodeEnum.GetEnumerator()
                Using fIter = featureEnum.GetEnumerator()
                    Using cIter = countEnum.GetEnumerator()

                        Do While bIter.MoveNext() AndAlso fIter.MoveNext() AndAlso cIter.MoveNext()
                            Dim b As ULong() = bIter.Current
                            Dim f As UInteger() = fIter.Current
                            Dim c As UInteger() = cIter.Current

                            If b.Length <> f.Length OrElse b.Length <> c.Length Then
                                Throw New InvalidOperationException("molecule_info 三元组（barcode_idx/feature_idx/count）长度不一致。")
                            End If

                            For i As Integer = 0 To b.Length - 1
                                Dim bc As ULong = b(i)
                                Dim ft As UInteger = f(i)
                                Dim cnt As UInteger = c(i)

                                If bc > UInteger.MaxValue Then
                                    Throw New OverflowException($"barcode_idx 值 {bc} 超出 32 位范围，无法映射到矩阵行。")
                                End If

                                Dim key As Long = CLng(bc) * CLng(nFeatures) + CLng(ft)
                                Dim v As Double

                                If agg.TryGetValue(key, v) Then
                                    agg(key) = v + CDbl(cnt)
                                Else
                                    agg.Add(key, CDbl(cnt))
                                End If

                                nProcessed += 1L
                                If nProcessed Mod stride = 0 Then
                                    ' 进度采样：避免刷屏，仅周期性输出
                                    Call $"molecule_info: aggregated {nProcessed} molecules, dict={agg.Count}".debug
                                End If
                            Next
                        Loop
                    End Using
                End Using
            End Using

            ' 聚合完成：展开为 COO 三元组并构造稀疏矩阵
            Dim rows As New List(Of Integer)(agg.Count)
            Dim cols As New List(Of Integer)(agg.Count)
            Dim vals As New List(Of Double)(agg.Count)

            For Each kv As KeyValuePair(Of Long, Double) In agg
                Dim key As Long = kv.Key
                Dim ft As Integer = CInt(key Mod nFeatures)
                Dim bc As Integer = CInt(key \ nFeatures)
                rows.Add(bc)
                cols.Add(ft)
                vals.Add(kv.Value)
            Next

            ' 释放聚合字典（显式，便于 GC 及时回收大对象）
            agg.Clear()
            agg = Nothing

            Dim matrix As New Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix(
                rows.ToArray, cols.ToArray, vals.ToArray, nBarcodes, nFeatures)

            Return New MoleculeInfoMatrix With {
                .barcodes = barcodes,
                .features = features,
                .matrix = matrix,
                .moleculeCount = nProcessed
            }
        End Function
    End Module
End Namespace
