Imports System.IO
Imports SMRUCC.genomics.SingleCell.Monocle3
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Module Program
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
End Module
