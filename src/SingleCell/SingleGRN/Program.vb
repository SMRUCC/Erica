Imports System.IO

Namespace SingleGRN

    ''' <summary>
    ''' 演示入口：从 Monocle3 导出的 CSV 构建 DBN 时间序列数据并落盘。
    ''' 用法：SingleGRN.exe [monocle3_output_dir] [dbn_output_dir]
    '''   - monocle3_output_dir 默认 K:\hsa\monocle3_output
    '''   - dbn_output_dir 默认 &lt;monocle3_output_dir&gt;\dbn_timeseries
    ''' 依赖的 Monocle3 导出物：
    '''   02_gene_by_cell.csv      （基因×样本，首列基因名）
    '''   sampleinfo.csv           （含 ID, mon_pseudotime 等）
    '''   pseudovelo_velocity.csv  （基因×细胞伪速度，首列基因名）
    ''' </summary>
    Module Program
        Sub Main(args As String())
            Dim monoDir = If(args.Length > 0 AndAlso args(0).Length > 0, args(0), "K:\hsa\monocle3_output")
            Dim dbnDir = If(args.Length > 1 AndAlso args(1).Length > 0, args(1), Path.Combine(monoDir, "dbn_timeseries"))

            Dim exprCsv = Path.Combine(monoDir, "02_gene_by_cell.csv")
            Dim ptCsv = Path.Combine(monoDir, "sampleinfo.csv")
            If Not File.Exists(ptCsv) Then
                ptCsv = Path.Combine(monoDir, "07_pseudotime.csv")
            End If
            Dim velCsv = Path.Combine(monoDir, "pseudovelo_velocity.csv")

            For Each f In {exprCsv, ptCsv, velCsv}
                If Not File.Exists(f) Then
                    Call Console.WriteLine($"[error] 缺少 Monocle3 导出文件: {f}")
                    Call Console.WriteLine("请先运行 Monocle3 测试程序生成 02_gene_by_cell.csv / sampleinfo.csv / pseudovelo_velocity.csv")
                    Return
                End If
            Next

            Call Console.WriteLine($"Monocle3 产物目录: {monoDir}")
            Call Console.WriteLine($"DBN 输出目录    : {dbnDir}")

            Dim opts = New DBNSampleProcessing.DBNSampleOptions With {
                .method = "bins",
                .numBins = 30,
                .geneSelection = "top",
                .topGeneFraction = 0.3,
                .discretize = False
            }

            Dim result = DBNSampleProcessing.BuildFromFiles(exprCsv, ptCsv, velCsv, opts)
            Call DBNSampleProcessing.SaveOutput(result, dbnDir)

            Call Console.WriteLine($"=== DBN 时间序列构建完成: {result} ===")
            Call Console.WriteLine($"    选中基因 : {result.selectedGenes.Length}")
            Call Console.WriteLine($"    伪时间点 : {result.binTimePoints.Length}")
            Call Console.WriteLine($"    输出文件 : {dbnDir}\dbn_timeseries.csv")
        End Sub
    End Module
End Namespace
