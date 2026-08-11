Imports System
Imports Erica.Analysis.SpatialTissue.RaidData.HDF5

Module Program

    ' 用户给定的真实 Visium HD 10x Genomics 测试文件路径
    Private Const FeatureSliceFile As String =
        "C:\Users\Administrator\Downloads\Visium_HD_6p5mm_Rat_Liver_feature_slice.h5"
    Private Const MoleculeInfoFile As String =
        "C:\Users\Administrator\Downloads\Visium_HD_6p5mm_Rat_Liver_molecule_info.h5"

    Private passCount As Integer = 0
    Private failCount As Integer = 0

    Sub Main(args As String())
        Dim memStart As Long = GC.GetTotalMemory(True)

        Console.WriteLine("=== Visium HD 10x HDF5 流式稀疏读取验证 ===")
        Console.WriteLine($"起始托管内存: {memStart \ (1024 * 1024)} MB")
        Console.WriteLine()

        VerifyFeatureSlice()
        VerifyMoleculeInfo()

        ' TEMP diagnostic for S4 / Seurat parsing
        Diagnose("G:\Erica\src\STRaid\test\data\test_seurat.rda")
        Diagnose("G:\Erica\src\STRaid\test\data\test_seurat.rds")

        Dim memEnd As Long = GC.GetTotalMemory(True)
        Console.WriteLine()
        Console.WriteLine($"结束托管内存: {memEnd \ (1024 * 1024)} MB")
        Console.WriteLine($"净内存变化: {(memEnd - memStart) \ (1024 * 1024)} MB")
        Console.WriteLine()
        Console.WriteLine($"=== 验证完成: PASS={passCount}, FAIL={failCount} ===")

        If failCount > 0 Then
            Environment.ExitCode = 1
        End If
    End Sub

    Private Sub Check(name As String, condition As Boolean, detail As String)
        If condition Then
            passCount += 1
            Console.WriteLine($"  [PASS] {name} - {detail}")
        Else
            failCount += 1
            Console.WriteLine($"  [FAIL] {name} - {detail}")
        End If
    End Sub

    Private Sub VerifyFeatureSlice()
        Console.WriteLine("--- feature_slice.h5 ---")
        Dim memBefore As Long = GC.GetTotalMemory(True)

        Dim result As VisiumHDResult
        Try
            result = TenXReader.OpenVisiumHD(FeatureSliceFile)
        Catch ex As Exception
            Check("OpenVisiumHD(feature_slice)", False, $"抛出异常: {ex.GetType().Name}: {ex.Message}")
            System.IO.File.WriteAllText("G:\Erica\src\STRaid\test\feature_err.txt", ex.ToString())
            Return
        End Try

        Check("DetectKind=FeatureSlice", result.kind = VisiumHDKind.FeatureSlice, $"kind={result.kind}")

        If result.featureSlice Is Nothing OrElse result.featureSlice.Count = 0 Then
            Check("featureSlice 非空", False, "未解析出任何分片")
            Return
        End If

        Dim nBarcodes As Integer = If(result.barcodes Is Nothing, 0, result.barcodes.Length)
        Dim nFeatures As Integer = If(result.features Is Nothing, 0, result.features.Length)

        Check("barcodes 规模", nBarcodes > 0, $"nBarcodes={nBarcodes}")
        Check("features 规模", nFeatures > 0, $"nFeatures={nFeatures}")
        If result.features IsNot Nothing AndAlso nFeatures > 0 Then
            Check("features 元数据", Not String.IsNullOrEmpty(result.features(0).id),
                  $"首特征 id={result.features(0).id}, type={result.features(0).featureType}")
        End If

        Dim sliceCount As Integer = result.featureSlice.Count
        Dim totalNnz As Long = 0L
        Dim maxRowDim As Integer = 0
        Dim maxColDim As Integer = 0
        Dim okDims As Boolean = True
        Dim okNnz As Boolean = True
        Dim peakMem As Long = memBefore

        For i As Integer = 0 To sliceCount - 1
            Dim slice As FeatureSliceData = result.featureSlice(i)
            Dim m As Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix = slice.sparseMatrix

            If m Is Nothing Then
                okDims = False
                Exit For
            End If

            totalNnz += m.nnz
            If m.RowDimension > maxRowDim Then maxRowDim = m.RowDimension
            If m.ColumnDimension > maxColDim Then maxColDim = m.ColumnDimension

            ' 不变量：行维度 == nBarcodes（bin 数），列维度 == nFeatures
            If m.RowDimension <> nBarcodes Then okDims = False
            If m.ColumnDimension <> nFeatures Then okDims = False
            ' 不变量：单个分片 nnz 必须为正且不超过所有 bin×feature 组合数
            If m.nnz <= 0 OrElse CLng(m.nnz) > CLng(nBarcodes) * CLng(nFeatures) Then okNnz = False

            Dim cur As Long = GC.GetTotalMemory(False)
            If cur > peakMem Then peakMem = cur
        Next

        Check("分片维度匹配", okDims, $"maxRowDim={maxRowDim} (期望 {nBarcodes}), maxColDim={maxColDim} (期望 {nFeatures})")
        Check("分片 nnz 合法", okNnz, $"total nnz={totalNnz:N0}, 分片数={sliceCount}")
        Check("分片数", sliceCount > 0, $"sliceCount={sliceCount}")

        Dim memAfter As Long = GC.GetTotalMemory(True)
        Console.WriteLine($"  [INFO] feature_slice 峰值托管内存: {peakMem \ (1024 * 1024)} MB, 结束: {memAfter \ (1024 * 1024)} MB")
    End Sub

    Private Sub VerifyMoleculeInfo()
        Console.WriteLine("--- molecule_info.h5 ---")
        Dim memBefore As Long = GC.GetTotalMemory(True)

        Dim result As VisiumHDResult
        Try
            result = TenXReader.OpenVisiumHD(MoleculeInfoFile)
        Catch ex As Exception
            Check("OpenVisiumHD(molecule_info)", False, $"抛出异常: {ex.GetType().Name}: {ex.Message}")
            System.IO.File.WriteAllText("G:\Erica\src\STRaid\test\molecule_err.txt", ex.ToString())
            Return
        End Try

        Check("DetectKind=MoleculeInfo", result.kind = VisiumHDKind.MoleculeInfo, $"kind={result.kind}")

        If result.moleculeInfo Is Nothing Then
            Check("moleculeInfo 非空", False, "未解析出结果")
            Return
        End If

        Dim nBarcodes As Integer = If(result.moleculeInfo.barcodes Is Nothing, 0, result.moleculeInfo.barcodes.Length)
        Dim nFeatures As Integer = If(result.moleculeInfo.features Is Nothing, 0, result.moleculeInfo.features.Length)
        Dim m As Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix = result.moleculeInfo.matrix
        Dim moleculeCount As Long = result.moleculeInfo.moleculeCount

        Check("barcodes 规模", nBarcodes > 0, $"nBarcodes={nBarcodes}")
        Check("features 规模", nFeatures > 0, $"nFeatures={nFeatures}")

        If m Is Nothing Then
            Check("matrix 非空", False, "聚合矩阵为 Nothing")
            Return
        End If

        Check("矩阵维度[RowDimension]", m.RowDimension = nBarcodes, $"{m.RowDimension} (期望 {nBarcodes})")
        Check("矩阵维度[ColumnDimension]", m.ColumnDimension = nFeatures, $"{m.ColumnDimension} (期望 {nFeatures})")

        Dim nnz As Long = m.nnz
        Check("nnz <= 分子总数", nnz <= moleculeCount, $"nnz={nnz:N0}, moleculeCount={moleculeCount:N0}")

        ' 不变量：nnz 必须是 (barcode, feature) 去重后的合法值，且坐标范围由维度保证
        Check("nnz 正向", nnz > 0, $"nnz={nnz:N0}")

        ' 坐标范围抽查：若干越界坐标应返回 0（不存在）
        Dim outOfRangeZero As Boolean = True
        If nBarcodes > 0 AndAlso nFeatures > 0 Then
            If m.Get(nBarcodes, 0) <> 0.0 Then outOfRangeZero = False
            If m.Get(0, nFeatures) <> 0.0 Then outOfRangeZero = False
        End If
        Check("越界坐标返回 0", outOfRangeZero, "越界 (row,col) 取值为 0")

        Dim memAfter As Long = GC.GetTotalMemory(True)
        Console.WriteLine($"  [INFO] molecule_info 峰值托管内存: {memAfter \ (1024 * 1024)} MB, 结束: {memAfter \ (1024 * 1024)} MB")
        Console.WriteLine($"  [INFO] UMI 矩阵维度 {m.RowDimension} x {m.ColumnDimension}, nnz={nnz:N0}, 分子总数={moleculeCount:N0}")
    End Sub

End Module
