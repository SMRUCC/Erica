Imports System.IO
Imports Erica.Analysis.SingleCell.Monocle3
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner

Module test1
    Sub Main(args As String())
        ' 验证数据集（行=基因，列=样本）
        Dim exprFile = "K:\hsa\Homo_sapiens_expr_advanced_all_conditions.csv"
        If args.Length > 0 Then
            exprFile = args(0)
        End If

        Dim outDir = "K:\hsa\monocle3_output"
        If args.Length > 1 Then
            outDir = args(1)
        End If
        If Not Directory.Exists(outDir) Then
            Call Directory.CreateDirectory(outDir)
        End If

        Dim opts = New Monocle3Options With {
            .numPCA = 50,
            .umapDim = 3,
            .knnK = 15,
            .resolution = 1.0,
            .useLeiden = False,
            .useCache = True,
            .overwriteCache = False,
            .cacheDir = Path.Combine(outDir, "cache")
        }

        Dim cache = New CacheStore(opts.cacheDir)
        Dim sampleNames As String()
        Dim result As Monocle3Result

        ' 若 01 缓存（预处理后的 [样本 × 基因] 矩阵）命中，则跳过耗时的 Matrix.LoadData
        If opts.useCache AndAlso Not opts.overwriteCache AndAlso cache.Hit("01_expr_hv.csv") Then
            Call Console.WriteLine("[cache] hit 01_expr_hv.csv, skip Matrix.LoadData")
            Dim sampleByGene = cache.LoadMatrix("01_expr_hv.csv")
            Dim geneNames = cache.LoadLabels("01_genes_hv.txt")
            sampleNames = cache.LoadLabels("01_samples.txt")
            result = Monocle3.Run(sampleByGene, geneNames, sampleNames, opts)
        Else
            Call Console.WriteLine($"Loading expression matrix: {exprFile}")
            Dim swLoad = System.Diagnostics.Stopwatch.StartNew()
            Dim matrix As Matrix = Matrix.LoadData(exprFile)
            swLoad.Stop()
            Call Console.WriteLine($"Loaded {matrix.expression.Length} genes x {matrix.sampleID.Length} samples  (LoadData: {swLoad.Elapsed.TotalSeconds:F1}s)")
            sampleNames = matrix.sampleID
            result = Monocle3.Run(matrix, opts)
        End If

        ' 导出分群
        Call ExportVector(Path.Combine(outDir, "clusters.csv"),
                          sampleNames,
                          result.clusters.Select(Function(c) c.ToString).ToArray,
                          "sample", "cluster")

        ' 导出伪时间
        Call ExportVector(Path.Combine(outDir, "pseudotime.csv"),
                          sampleNames,
                          result.pseudotime.Select(Function(p) p.ToString("G17")).ToArray,
                          "sample", "pseudotime")

        ' 导出 MST 主图边
        Call ExportGraph(Path.Combine(outDir, "mst_graph.csv"), result.clusterGraph)

        ' 导出 PAGA 图边
        Call ExportGraph(Path.Combine(outDir, "paga_graph.csv"), result.pagaGraph)

        ' 回写样本级结果到 SampleInfo.metadata，并导出为 CSV
        Dim samples = result.ToSampleInfo()
        Call ExportSampleInfo(Path.Combine(outDir, "sampleinfo.csv"), samples)

        ' 导出 PseudoVelo 伪 RNA 速率
        If result.pseudoVelocity IsNot Nothing Then
            Call ExportVelocity(Path.Combine(outDir, "pseudovelo_velocity.csv"),
                                result.pseudoVelocity.geneNames,
                                sampleNames,
                                result.pseudoVelocity.velocity)
            If result.pseudoVelocity.velocityUMAP IsNot Nothing Then
                Call ExportUMAPVelocity(Path.Combine(outDir, "pseudovelo_umap.csv"),
                                         sampleNames, result.umap2d, result.pseudoVelocity.velocityUMAP)
            End If
        End If

        ' 导出 top 变化基因（按 |Moran I|）
        Using sw As New StreamWriter(Path.Combine(outDir, "moran_top_genes.csv"))
            Call sw.WriteLine("gene,moranI")
            For Each g In result.topVariableGenes
                Call sw.WriteLine($"{g.gene},{g.moranI:0.000000}")
            Next
        End Using

        Call Console.WriteLine()
        Call Console.WriteLine($"=== Summary ===")
        Call Console.WriteLine($"samples          : {sampleNames.Length}")
        Call Console.WriteLine($"num clusters     : {result.clusters.Distinct.Count}")
        Call Console.WriteLine($"global Moran I   : {result.moranGlobal:0.000000}")
        Call Console.WriteLine($"MST edges        : {result.clusterGraph.edges.Length}")
        Call Console.WriteLine($"PAGA edges       : {result.pagaGraph.edges.Length}")
        Call Console.WriteLine($"sample info rows : {samples.Length}  (sampleinfo.csv)")
        If result.pseudoVelocity IsNot Nothing Then
            Call Console.WriteLine($"pseudo-velocity  : {result.pseudoVelocity.geneNames.Length} genes x {sampleNames.Length} cells  (pseudovelo_velocity.csv)")
            Call Console.WriteLine($"velocity UMAP    : {If(result.pseudoVelocity.useProjection, "projected", "disabled")}  (pseudovelo_umap.csv)")
        Else
            Call Console.WriteLine($"pseudo-velocity  : disabled")
        End If
        Call Console.WriteLine($"outputs          : {outDir}")
        Call Console.WriteLine("Done.")
    End Sub

    Private Sub ExportVector(file As String, names As String(), values As String(), nameHeader As String, valueHeader As String)
        Using sw As New StreamWriter(file)
            Call sw.WriteLine($"{nameHeader},{valueHeader}")
            For i As Integer = 0 To names.Length - 1
                Call sw.WriteLine($"{names(i)},{values(i)}")
            Next
        End Using
    End Sub

    Private Sub ExportGraph(file As String, g As GraphData)
        Using sw As New StreamWriter(file)
            Call sw.WriteLine("source,target,weight")
            For Each e In g.edges
                Call sw.WriteLine($"{g.nodes(e.u)},{g.nodes(e.v)},{e.weight:0.000000}")
            Next
        End Using
    End Sub

    ''' <summary>
    ''' 把 ToSampleInfo 生成的 SampleInfo 集合导出为 CSV：固定列 ID, sample_name，
    ''' 其余列为各样本 metadata 字典的键（按排序保证列顺序稳定）。
    ''' </summary>
    Private Sub ExportSampleInfo(file As String, samples As SampleInfo())
        ' 收集所有 metadata 键的合集，排序以保证列顺序确定（mon_* 字段自然成块）
        Dim metaKeys As New SortedSet(Of String)
        For Each s In samples
            If s.metadata IsNot Nothing Then
                For Each key In s.metadata.Keys
                    Call metaKeys.Add(key)
                Next
            End If
        Next

        Using sw As New StreamWriter(file)
            Dim header = "ID,sample_name"
            For Each key In metaKeys
                header &= "," & key
            Next
            Call sw.WriteLine(header)

            For Each s In samples
                Dim line = $"{s.ID},{s.sample_name}"
                For Each key In metaKeys
                    Dim v = If(s.metadata IsNot Nothing AndAlso s.metadata.ContainsKey(key), s.metadata(key), "")
                    line &= "," & v
                Next
                Call sw.WriteLine(line)
            Next
        End Using
    End Sub

    ''' <summary>
    ''' 导出 PseudoVelo 伪速度矩阵（基因 × 细胞）：首列基因名，其余列为各样本伪速度值。
    ''' </summary>
    Private Sub ExportVelocity(file As String, geneNames As String(), sampleNames As String(), velocity As Double(,))
        Using sw As New StreamWriter(file)
            Dim header = "gene"
            For Each s In sampleNames
                header &= "," & s
            Next
            Call sw.WriteLine(header)

            Dim nGenes = velocity.GetLength(0)
            Dim nCells = velocity.GetLength(1)
            For g As Integer = 0 To nGenes - 1
                Dim line = geneNames(g)
                For j As Integer = 0 To nCells - 1
                    line &= "," & velocity(g, j).ToString("G17")
                Next
                Call sw.WriteLine(line)
            Next
        End Using
    End Sub

    ''' <summary>
    ''' 导出 UMAP 速度向量：每行一个样本，含 UMAP2D 坐标与其上的伪速度向量（供流线/箭头可视化）。
    ''' </summary>
    Private Sub ExportUMAPVelocity(file As String, sampleNames As String(), umap2d As Double(,), velUMAP As Double(,))
        Using sw As New StreamWriter(file)
            Call sw.WriteLine("sample,umap2d_x,umap2d_y,velo_umap_x,velo_umap_y")
            Dim n = sampleNames.Length
            For i As Integer = 0 To n - 1
                Call sw.WriteLine($"{sampleNames(i)},{umap2d(i, 0):G17},{umap2d(i, 1):G17},{velUMAP(i, 0):G17},{velUMAP(i, 1):G17}")
            Next
        End Using
    End Sub
End Module
