Imports System.IO
Imports System.Math
Imports Erica.Analysis.SingleCell.Monocle3
Imports SMRUCC.genomics.Analysis.BNLearn
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports SMRUCC.genomics.Analysis.CellPhenotype
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner

Namespace SingleGRN

    ''' <summary>
    ''' 端到端演示入口：从原始表达矩阵出发，串联
    '''   Monocle3（伪时间排序 / 轨迹推断）
    '''   → PseudoVelo（伪 RNA 速率，内置于 Monocle3.Run）
    '''   → DBNSampleProcessing（按伪时间分箱聚合成 K 个离散伪时间点）
    ''' 生成动态贝叶斯网络（DBN）所需的 GeneExpressionData 时间序列并落盘。
    '''
    ''' 用法：SingleGRN.exe [exprFile] [monocle3OutDir] [dbnOutDir]
    '''   - exprFile        原始表达矩阵（行=基因，列=样本）。默认 K:\hsa\Homo_sapiens_expr_advanced_all_conditions.csv
    '''   - monocle3OutDir  Monocle3 / PseudoVelo 中间产物与对照 CSV 输出目录。默认 K:\hsa\monocle3_output
    '''   - dbnOutDir       DBN 时间序列输出目录。默认 &lt;monocle3OutDir&gt;\dbn_timeseries
    ''' </summary>
    Module Program
        Sub Main(args As String())
            Dim exprFile = If(args.Length > 0 AndAlso args(0).Length > 0, args(0), "K:\hsa\Homo_sapiens_expr_advanced_all_conditions.csv")
            Dim monoDir = If(args.Length > 1 AndAlso args(1).Length > 0, args(1), "K:\hsa\monocle3_output")
            Dim dbnDir = If(args.Length > 2 AndAlso args(2).Length > 0, args(2), Path.Combine(monoDir, "dbn_timeseries"))

            If Not File.Exists(exprFile) Then
                Call Console.WriteLine($"[error] 原始表达矩阵不存在: {exprFile}")
                Return
            End If
            If Not Directory.Exists(monoDir) Then
                Call Directory.CreateDirectory(monoDir)
            End If
            Call Directory.CreateDirectory(dbnDir)

            ' ==================== ① 加载原始表达矩阵 ====================
            Call Console.WriteLine($"加载原始表达矩阵: {exprFile}")
            Dim swLoad = Diagnostics.Stopwatch.StartNew()
            Dim matrix As Matrix = Matrix.LoadData(exprFile)
            swLoad.Stop()
            Dim sampleNames = matrix.sampleID
            Call Console.WriteLine($"  基因={matrix.expression.Length} x 样本={sampleNames.Length}  (LoadData: {swLoad.Elapsed.TotalSeconds:F1}s)")

            ' ==================== ② Monocle3 伪时间排序 + PseudoVelo 伪速率 ====================
            Dim monoOpts = New Monocle3Options With {
                .numPCA = 10,
                .umapDim = 3,
                .knnK = 15,
                .resolution = 1.0,
                .useLeiden = False,
                .useCache = True,
                .overwriteCache = False,
                .cacheDir = Path.Combine(monoDir, "cache"),
                .pseudoVeloEnabled = True,
                .pseudoVeloWindow = 2,
                .pseudoVeloSpan = 0.3,
                .useVelocityProjection = True
            }

            Call Console.WriteLine("运行 Monocle3（伪时间排序 + PseudoVelo 伪速率）...")
            Dim swMono = Diagnostics.Stopwatch.StartNew()
            Dim result = Erica.Analysis.SingleCell.Monocle3.Monocle3.Run(matrix, monoOpts)
            swMono.Stop()
            Call Console.WriteLine($"  Monocle3 完成 (伪时间/速率计算: {swMono.Elapsed.TotalSeconds:F1}s)")

            ' ==================== ③ 提取 HV 基因表达（log1p，与 Monocle3 内部尺度一致） ====================
            ' Monocle3.RunCore 内部对表达做 log1p 后再算伪时间/速度，故 DBN 时间序列也用 log1p 表达，
            ' 使分箱聚合的表达与 velocity 同源；velocity 缺失时回退为全基因表达。
            Dim hvGenes As String()
            If result.pseudoVelocity IsNot Nothing AndAlso
               result.pseudoVelocity.geneNames IsNot Nothing AndAlso
               result.pseudoVelocity.geneNames.Length > 0 Then
                hvGenes = result.pseudoVelocity.geneNames
            Else
                hvGenes = matrix.expression.Select(Function(r) r.geneID).ToArray()
            End If

            Dim nSamples = sampleNames.Length
            Dim nHV = hvGenes.Length
            Dim geneRow As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For r As Integer = 0 To matrix.expression.Length - 1
                geneRow(matrix.expression(r).geneID) = r
            Next

            Dim sampleByGene(nSamples - 1, nHV - 1) As Double
            For g As Integer = 0 To nHV - 1
                If Not geneRow.ContainsKey(hvGenes(g)) Then
                    Call Console.WriteLine($"[warn] HV 基因 {hvGenes(g)} 不在原始矩阵中，已跳过")
                    Continue For
                End If
                Dim row = geneRow(hvGenes(g))
                For j As Integer = 0 To nSamples - 1
                    ' log1p：与 Monocle3 内部 exprData 尺度一致
                    sampleByGene(j, g) = Log(1 + matrix.expression(row).experiments(j))
                Next
            Next
            Call Console.WriteLine($"  HV 基因表达矩阵: 样本={nSamples} x 基因={nHV} (log1p)")

            ' ==================== ④ DBN 时间序列预处理（分箱聚合） ====================
            Dim dbnOpts = New DBNSampleProcessing.DBNSampleOptions With {
                .method = "bins",
                .numBins = 300,
                .geneSelection = "top",
                .topGeneFraction = 0.3,
                .discretize = False
            }

            Call Console.WriteLine("构建 DBN 时间序列（按伪时间分箱聚合）...")
            Dim swDbn = Diagnostics.Stopwatch.StartNew()
            Dim dbnOut = DBNSampleProcessing.BuildFromMonocle3(result, sampleByGene, hvGenes, sampleNames, dbnOpts)
            swDbn.Stop()

            Call DBNSampleProcessing.SaveOutput(dbnOut, dbnDir)
            Call Console.WriteLine($"  DBN 预处理完成 ({swDbn.Elapsed.TotalSeconds:F1}s)")

            ' ==================== ⑤ 导出对照产物（与 Monocle3 test 格式一致） ====================
            ' sampleinfo（含 mon_pseudotime 等样本级结果）
            Dim samples = result.ToSampleInfo(sampleNames)
            Call ExportSampleInfo(Path.Combine(monoDir, "sampleinfo.csv"), samples)

            ' 伪速率矩阵（基因 × 细胞）
            If result.pseudoVelocity IsNot Nothing AndAlso result.pseudoVelocity.velocity IsNot Nothing Then
                Call ExportVelocity(Path.Combine(monoDir, "pseudovelo_velocity.csv"),
                                    result.pseudoVelocity.geneNames, sampleNames, result.pseudoVelocity.velocity)
            End If

            ' ==================== ⑥ 基因调控网络构建与虚拟扰动分析 ====================
            ' 基于 DBN 时间序列（GeneExpressionData）构建 BNLearn 高斯贝叶斯网络，融合伪速率趋势方向先验，
            ' 训练后做虚拟敲除 / 过表达 / 动态级联敲除 / 批量敲除，并导出扰动对比结果。
            Dim grnDir = Path.Combine(monoDir, "dbn_grn")
            If Not Directory.Exists(grnDir) Then Call Directory.CreateDirectory(grnDir)

            Call Console.WriteLine("构建基因表达调控网络并虚拟扰动分析...")
            Dim prior = BuildVelocityPrior(dbnOut)
            Dim knockGenes = SelectDemoGenes(dbnOut, 3)
            Dim overExprList As New List(Of (Gene As String, Fold As Double))
            If knockGenes.Length > 0 Then
                overExprList.Add((Gene:=knockGenes(0), Fold:=3.0))
            End If

            Dim grn = GeneRegulatoryNetwork.TrainAndIntervene(
                dbnOut.timeSeries, prior, knockGenes,
                overExprList.ToArray, dynamicSteps:=10, outputDir:=grnDir)

            Call Console.WriteLine($"  调控网络节点    : {dbnOut.timeSeries.NGene}  (伪时间 bin={dbnOut.timeSeries.TimePoints.Length})")
            Call Console.WriteLine($"  方向先验边      : {prior.Edges.Count}  (PseudoVelo trend)")
            Call Console.WriteLine($"  扰动演示基因    : {String.Join(", ", knockGenes)}")
            Call Console.WriteLine($"  扰动结果目录    : {grnDir}\")

            ' ==================== ⑦ Summary ====================
            Call Console.WriteLine()
            Call Console.WriteLine("=== SingleGRN 端到端流程完成 ===")
            Call Console.WriteLine($"原始表达矩阵    : {exprFile}")
            Call Console.WriteLine($"样本数          : {sampleNames.Length}")
            Call Console.WriteLine($"全局 Moran I    : {result.moranGlobal:0.000000}")
            Call Console.WriteLine($"分群数          : {result.clusters.Distinct.Count}")
            Call Console.WriteLine($"HV 基因数       : {nHV}  (伪速率基因集)")
            If result.pseudoVelocity IsNot Nothing Then
                Call Console.WriteLine($"伪速率矩阵      : {result.pseudoVelocity.geneNames.Length} x {nSamples}  (pseudovelo_velocity.csv)")
            Else
                Call Console.WriteLine($"伪速率矩阵      : 未启用")
            End If
            Call Console.WriteLine($"选中基因        : {dbnOut.selectedGenes.Length} / {dbnOut.geneNames.Length}")
            Call Console.WriteLine($"伪时间点(bin)   : {dbnOut.binTimePoints.Length}")
            Call Console.WriteLine($"DBN 时间序列    : {dbnDir}\dbn_timeseries.csv")
            Call Console.WriteLine($"对照产物        : {monoDir}\sampleinfo.csv, {monoDir}\pseudovelo_velocity.csv")
            Call Console.WriteLine("Done.")
        End Sub

        ' ==================== 对照 CSV 导出辅助 ====================

        Private Sub ExportSampleInfo(file As String, samples As SampleInfo())
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

        ' ==================== 调控网络 / 虚拟扰动辅助 ====================

        ''' <summary>
        ''' 由 DBN 预处理结果中的伪速率趋势（trendSign，按 geneNames 顺序）构造因果方向先验。
        ''' 启发式：取趋势幅度 |trend| 最大的 Top50 选中基因，按趋势正负分为上游（正）/下游（负），
        ''' 在上游→下游间连激活边（权重为两侧 |trend| 均值，evidence="PseudoVelo trend"）。
        ''' 该先验为弱方向约束，缺失（候选不足或全同号）时返回空网络，退化为纯数据驱动 MMHC。
        ''' </summary>
        Private Function BuildVelocityPrior(dbnOut As DBNSampleProcessing.DBNPreprocessOutput) As PriorNetwork
            Dim prior As New PriorNetwork()

            If dbnOut Is Nothing OrElse dbnOut.selectedGenes Is Nothing OrElse dbnOut.trendSign Is Nothing Then
                Return prior
            End If

            ' trendSign(i) 与 selectedGenes(i) 一一对应
            Dim genes = dbnOut.selectedGenes
            Dim trend = dbnOut.trendSign
            Dim pairs As New List(Of (gene As String, t As Double))
            For i As Integer = 0 To genes.Length - 1
                If i < trend.Length Then
                    pairs.Add((gene := genes(i), t := trend(i)))
                End If
            Next

            ' 取趋势幅度 |t| 最大的 Top50 候选
            Dim sel = pairs.OrderByDescending(Function(x) Abs(x.t)).Take(50).ToArray()

            If sel.Length < 2 Then
                Return prior
            End If

            Dim pos = sel.Where(Function(x) x.t >= 0).ToArray()
            Dim neg = sel.Where(Function(x) x.t < 0).ToArray()
            If pos.Length = 0 OrElse neg.Length = 0 Then
                Return prior
            End If

            Dim maxEdges = 200
            Dim edges = 0
            For Each p In pos
                For Each n In neg
                    prior.AddEdge(p.gene, n.gene, Effector.Activator, (Abs(p.t) + Abs(n.t)) / 2.0, "PseudoVelo trend")
                    edges += 1
                    If edges >= maxEdges Then Exit For
                Next
                If edges >= maxEdges Then Exit For
            Next

            Call Console.WriteLine($"  [prior] 由伪速率趋势构造方向先验边 {prior.Edges.Count} (候选 {sel.Length}: 正 {pos.Length} / 负 {neg.Length})")
            Return prior
        End Function

        ''' <summary>
        ''' 选取演示虚拟扰动的目标基因：按伪速率趋势幅度 |trend| 降序取前 n 个选中基因
        ''' （趋势幅度大者代表性更强）；若缺少趋势数据则退化为前 n 个选中基因。
        ''' </summary>
        Private Function SelectDemoGenes(dbnOut As DBNSampleProcessing.DBNPreprocessOutput, n As Integer) As String()
            If dbnOut Is Nothing OrElse dbnOut.selectedGenes Is Nothing OrElse dbnOut.selectedGenes.Length = 0 Then
                Return {}
            End If
            If dbnOut.trendSign Is Nothing Then
                Return dbnOut.selectedGenes.Take(n).ToArray()
            End If

            ' trendSign(i) 与 selectedGenes(i) 一一对应
            Dim genes = dbnOut.selectedGenes
            Dim trend = dbnOut.trendSign
            Dim idx = Enumerable.Range(0, genes.Length) _
                .Where(Function(i) i < trend.Length) _
                .OrderByDescending(Function(i) Abs(trend(i))) _
                .Take(n) _
                .ToArray()

            Return idx.Select(Function(i) genes(i)).ToArray()
        End Function
    End Module
End Namespace
