Imports System
Imports System.IO
Imports Erica.Analysis.SpatialTissue.RaidData
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

        Console.WriteLine("=== Visium HD 10x HDF5 / SeuratObject RData 验证 ===")
        Console.WriteLine($"起始托管内存: {memStart \ (1024 * 1024)} MB")
        Console.WriteLine()

        ' === SeuratObject 读取验证 ===
        ' First, run the original diagnostic to check if basic parsing works
        Console.WriteLine("=== Original Diagnose Test ===")
        Diagnose("G:\Erica\src\STRaid\test\data\test_seurat.rds")
        Console.WriteLine()

        TestSeuratObjectRDS()
        TestSeuratObjectRDA()

        Console.WriteLine()

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

#Region "SeuratObject 读取验证"

    Private Sub TestSeuratObjectRDS()
        Console.WriteLine("--- SeuratObject RDS 读取 ---")
        Dim filePath As String = "G:\Erica\src\STRaid\test\data\test_seurat.rds"

        If Not File.Exists(filePath) Then
            Console.WriteLine($"  [SKIP] 测试文件不存在: {filePath}")
            Console.WriteLine($"  请运行: C:\Program Files\R\R-4.5.0\bin\Rscript.exe G:\Erica\src\STRaid\test\gen_test_seurat.R")
            Return
        End If

        Dim memBefore As Long = GC.GetTotalMemory(True)
        Dim seurat As SeuratObject = Nothing

        Try
            Console.WriteLine($"  [DEBUG] Calling SeuratObjectReader.ReadFile for RDS...")
            seurat = SeuratObjectReader.ReadFile(filePath)
            Console.WriteLine($"  [DEBUG] ReadFile completed successfully.")
        Catch ex As Exception
            Check("SeuratObjectReader.ReadFile(RDS)", False, $"抛出异常: {ex.GetType().Name}: {ex.Message}")
            Console.Error.WriteLine($"  [STACK] {ex.StackTrace}")
            File.WriteAllText("G:\Erica\src\STRaid\test\seurat_rds_err.txt", ex.ToString())
            Return
        End Try

        ValidateSeuratObject(seurat, "RDS")

        Dim memAfter As Long = GC.GetTotalMemory(True)
        Console.WriteLine($"  [INFO] RDS 读取内存: before={memBefore \ (1024 * 1024)} MB, after={memAfter \ (1024 * 1024)} MB")
    End Sub

    Private Sub TestSeuratObjectRDA()
        Console.WriteLine("--- SeuratObject RDA 读取 ---")
        Dim filePath As String = "G:\Erica\src\STRaid\test\data\test_seurat.rda"

        If Not File.Exists(filePath) Then
            Console.WriteLine($"  [SKIP] 测试文件不存在: {filePath}")
            Console.WriteLine($"  请运行: C:\Program Files\R\R-4.5.0\bin\Rscript.exe G:\Erica\src\STRaid\test\gen_test_seurat.R")
            Return
        End If

        Dim memBefore As Long = GC.GetTotalMemory(True)
        Dim seurat As SeuratObject = Nothing

        Try
            seurat = SeuratObjectReader.ReadFile(filePath)
        Catch ex As Exception
            Check("SeuratObjectReader.ReadFile(RDA)", False, $"抛出异常: {ex.GetType().Name}: {ex.Message}")
            File.WriteAllText("G:\Erica\src\STRaid\test\seurat_rda_err.txt", ex.ToString())
            Return
        End Try

        ValidateSeuratObject(seurat, "RDA")

        Dim memAfter As Long = GC.GetTotalMemory(True)
        Console.WriteLine($"  [INFO] RDA 读取内存: before={memBefore \ (1024 * 1024)} MB, after={memAfter \ (1024 * 1024)} MB")
    End Sub

    Private Sub ValidateSeuratObject(seurat As SeuratObject, source As String)
        Check($"{source}: SeuratObject 非空", seurat IsNot Nothing, "")

        If seurat Is Nothing Then Return

        Console.WriteLine($"  [INFO] {source}: {seurat.ToString()}")
        Console.WriteLine($"  [INFO] {source}: Version={seurat.Version}")

        ' Validate assays
        Check($"{source}: Assays 非空", seurat.Assays IsNot Nothing AndAlso seurat.Assays.Count > 0,
              $"count={If(seurat.Assays?.Count, 0)}")

        If seurat.Assays IsNot Nothing AndAlso seurat.Assays.Count > 0 Then
            For Each kvp In seurat.Assays
                Dim assay As SeuratAssay = kvp.Value
                Console.WriteLine($"  [INFO] {source}: Assay[{assay.Name}] features={assay.nFeatures} cells={assay.nCells} key={assay.Key}")

                Check($"{source}: Assay[{assay.Name}] nFeatures > 0", assay.nFeatures > 0,
                      $"nFeatures={assay.nFeatures}")
                Check($"{source}: Assay[{assay.Name}] nCells > 0", assay.nCells > 0,
                      $"nCells={assay.nCells}")

                ' Check counts matrix
                If assay.Counts IsNot Nothing Then
                    Dim hasData As Boolean = False
                    For i As Integer = 0 To Math.Min(assay.Counts.GetLength(0) - 1, 4)
                        For j As Integer = 0 To Math.Min(assay.Counts.GetLength(1) - 1, 4)
                            If assay.Counts(i, j) > 0 Then hasData = True
                        Next
                    Next
                    Check($"{source}: Assay[{assay.Name}] counts 有数据",
                          hasData, $"dims={assay.Counts.GetLength(0)}x{assay.Counts.GetLength(1)}")
                Else
                    Console.WriteLine($"  [WARN] {source}: Assay[{assay.Name}] counts is Nothing")
                End If

                ' Check data (normalized)
                If assay.Data IsNot Nothing Then
                    Console.WriteLine($"  [INFO] {source}: Assay[{assay.Name}] data dims={assay.Data.GetLength(0)}x{assay.Data.GetLength(1)}")
                End If

                ' Check scale.data
                If assay.ScaleData IsNot Nothing Then
                    Console.WriteLine($"  [INFO] {source}: Assay[{assay.Name}] scale.data dims={assay.ScaleData.GetLength(0)}x{assay.ScaleData.GetLength(1)}")
                End If

                ' Check variable features
                If assay.VariableFeatures IsNot Nothing AndAlso assay.VariableFeatures.Length > 0 Then
                    Check($"{source}: Assay[{assay.Name}] VariableFeatures",
                          assay.VariableFeatures.Length > 0,
                          $"nVarFeatures={assay.VariableFeatures.Length}")
                End If
            Next
        End If

        ' Validate meta.data
        Check($"{source}: MetaData 非空",
              seurat.MetaData IsNot Nothing AndAlso seurat.MetaData.Count > 0,
              $"columns={If(seurat.MetaData?.Count, 0)}")

        If seurat.MetaData IsNot Nothing Then
            Console.WriteLine($"  [INFO] {source}: MetaData columns: {String.Join(", ", seurat.MetaData.Keys)}")
        End If

        ' Validate cell names
        Check($"{source}: CellNames",
              seurat.CellNames IsNot Nothing AndAlso seurat.CellNames.Length > 0,
              $"nCells={If(seurat.CellNames?.Length, 0)}")

        ' Validate reductions
        If seurat.Reductions IsNot Nothing AndAlso seurat.Reductions.Count > 0 Then
            Console.WriteLine($"  [INFO] {source}: Reductions: {String.Join(", ", seurat.Reductions.Keys)}")
            For Each kvp In seurat.Reductions
                Dim red As DimReduction = kvp.Value
                Console.WriteLine($"  [INFO] {source}: Reduction[{red.Name}] method={red.Method} cells={red.nCells} dims={red.nDimensions}")

                Check($"{source}: Reduction[{red.Name}] CellEmbeddings 非空",
                      red.CellEmbeddings IsNot Nothing,
                      $"dims={If(red.CellEmbeddings, "null")}")

                If red.CellEmbeddings IsNot Nothing Then
                    Check($"{source}: Reduction[{red.Name}] CellEmbeddings dims correct",
                          red.nCells > 0 AndAlso red.nDimensions > 0,
                          $"cells={red.nCells} dims={red.nDimensions}")
                End If
            Next
        Else
            Console.WriteLine($"  [WARN] {source}: No reductions found")
        End If

        ' Validate active.ident
        If seurat.ActiveIdent IsNot Nothing AndAlso seurat.ActiveIdent.Length > 0 Then
            Check($"{source}: ActiveIdent 非空",
                  seurat.ActiveIdent.Length > 0,
                  $"length={seurat.ActiveIdent.Length}")
        End If

        ' Validate nCells consistency
        If seurat.CellNames IsNot Nothing Then
            Dim cellCount As Integer = seurat.CellNames.Length
            If seurat.MetaData IsNot Nothing AndAlso seurat.MetaData.Count > 0 Then
                Dim metaCellCount As Integer = seurat.MetaData.Values.First().Length
                Check($"{source}: CellNames count = MetaData row count",
                      cellCount = metaCellCount,
                      $"CellNames={cellCount}, MetaData={metaCellCount}")
            End If
        End If
    End Sub

#End Region

    Private Sub Check(name As String, condition As Boolean, detail As String)
        If condition Then
            passCount += 1
            Console.WriteLine($"  [PASS] {name} - {detail}")
        Else
            failCount += 1
            Console.WriteLine($"  [FAIL] {name} - {detail}")
        End If
    End Sub

End Module
