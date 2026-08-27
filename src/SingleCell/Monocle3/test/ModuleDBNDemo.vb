Imports System.IO
Imports System.Math
Imports Erica.Analysis.SingleCell.Monocle3
Imports Erica.Analysis.SingleCell.VirtualGRN
Imports Microsoft.VisualBasic.Data.Framework.StorageProvider
Imports Microsoft.VisualBasic.Data.visualize.Network
Imports SMRUCC.genomics.Analysis.BNLearn
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports SMRUCC.genomics.Analysis.CellPhenotype
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner
Imports SMRUCC.genomics.InteractionModel

''' <summary>
''' 优化版演示：基于 WGCNA 共表达模块的 DBN 子网络训练 + 全局级联虚拟扰动。
'''
''' 复用 Program.vb 的端到端链路：
'''   WGCNA 网络 + TF 列表 → BuildPriorNetwork
'''   → Monocle3 伪时间排序 + PseudoVelo 伪速率 → DBN 时间序列分箱聚合
'''   → VelocityNetwork.BuildVelocityPrior 合并 wgcna 与伪速率先验
''' 之后新增步骤：
'''   读取 WGCNA 模块划分（gene_module_assignment.csv），调用
'''   GeneRegulatoryNetwork.TrainModularDBNIntervene：
'''     按模块划分时间序列 → 每模块单独训练 DynamicBayesianNetwork 子网络
'''     → 基于模块 eigengene 关联做级联虚拟扰动 → 导出全局响应结果。
'''
''' 相比 Program.vb 的全局 TrainAndIntervene，本 demo 在大型 WGCNA 网络下将训练代价
''' 由 O(N^2·样本) 降为 Σ O(模块规模^2·样本)，显著提速。
'''
''' 默认用 topGeneFraction 限制参与建模的基因规模，并仅演示少量扰动基因，
''' 以便快速验证；如需跑全量大盘，将 topGeneFraction 调大、knockGenes 取更多即可。
''' </summary>
Module ModuleDBNDemo

    Sub Run(args As String())
        ' ==================== 输入路径（沿用 WGCNADemo 的模块文件 + 本流程的表达矩阵） ====================
        Dim wgcnaEdges = "K:\hsa_grn\network-edges.csv"
        Dim exprFile = "K:\hsa\Homo_sapiens_expr_advanced_all_conditions.dat"
        Dim tfFile = "K:\hsa_grn\Homo_sapiens_TF.txt"
        Dim moduleFile = "K:\hsa\WGCNA_output-demo\gene_module_assignment.csv"

        If args.Length > 0 AndAlso args(0).Length > 0 Then wgcnaEdges = args(0)
        If args.Length > 1 AndAlso args(1).Length > 0 Then exprFile = args(1)
        If args.Length > 2 AndAlso args(2).Length > 0 Then moduleFile = args(2)

        If Not File.Exists(exprFile) Then
            Call Console.WriteLine($"[error] 原始表达矩阵不存在: {exprFile}")
            Return
        End If
        If Not File.Exists(moduleFile) Then
            Call Console.WriteLine($"[error] WGCNA 模块划分文件不存在: {moduleFile}")
            Return
        End If

        ' ==================== ① WGCNA 先验 + TF 注释 ====================
        Dim wgcna = NetworkFileIO.ReadEdges(Of RelationshipScore)(wgcnaEdges)
        Dim matrix = Matrix.LoadStreamData(exprFile)
        Dim hsaTF = DataFrameResolver.Load(tfFile, tsv:=True)("Ensembl")
        Call Console.WriteLine($"  基因={matrix.expression.Length} x 样本={matrix.sample_count}")
        Call Console.WriteLine($"  WGCNA 边={wgcna.Count}  TF={hsaTF.Length}")

        Dim prior = wgcna.BuildPriorNetwork(New HashSet(Of String)(hsaTF))

        ' ==================== ② Monocle3 伪时间排序 + PseudoVelo 伪速率 ====================
        Dim monoDir = "K:\hsa\monocle3_output_moduledbn"
        Dim grnDir = Path.Combine(monoDir, "dbn_grn")
        Dim dbnDir = Path.Combine(monoDir, "dbn_timeseries")
        If Not Directory.Exists(monoDir) Then Call Directory.CreateDirectory(monoDir)

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
            .useVelocityProjection = True,
            .numHVGenes = 3000
        }

        Call Console.WriteLine("运行 Monocle3（伪时间排序 + PseudoVelo 伪速率）...")
        Dim result = Erica.Analysis.SingleCell.Monocle3.Monocle3.Run(matrix, monoOpts)

        ' ==================== ③ HV 基因表达（log1p，与 Monocle3 内部尺度一致） ====================
        Dim hvGenes As String()
        If result.pseudoVelocity IsNot Nothing AndAlso
           result.pseudoVelocity.geneNames IsNot Nothing AndAlso
           result.pseudoVelocity.geneNames.Length > 0 Then
            hvGenes = result.pseudoVelocity.geneNames
        Else
            hvGenes = matrix.expression.Select(Function(r) r.geneID).ToArray()
        End If

        Dim nSamples = matrix.sampleID.Length
        Dim nHV = hvGenes.Length
        Dim geneRow As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For r As Integer = 0 To matrix.expression.Length - 1
            geneRow(matrix.expression(r).geneID) = r
        Next

        Dim sampleByGene(nSamples - 1, nHV - 1) As Double
        For g As Integer = 0 To nHV - 1
            If Not geneRow.ContainsKey(hvGenes(g)) Then Continue For
            Dim row = geneRow(hvGenes(g))
            For j As Integer = 0 To nSamples - 1
                sampleByGene(j, g) = Log(1 + matrix.expression(row).experiments(j))
            Next
        Next
        Call Console.WriteLine($"  HV 基因表达矩阵: 样本={nSamples} x 基因={nHV} (log1p)")

        ' ==================== ④ DBN 时间序列预处理（分箱聚合） ====================
        ' 默认限制基因规模以快速验证（大型 WGCNA 网络下全量会很慢）。
        ' 跑全量大盘：将 topGeneFraction 调大到 1.0。
        Dim dbnOpts = New DBNSampleOptions With {
            .method = "bins",
            .numBins = 300,
            .geneSelection = "top",
            .topGeneFraction = 0.3,
            .discretize = False
        }
        Dim dbnOut = DBNSampleProcessing.BuildFromMonocle3(result, sampleByGene, hvGenes, matrix.sampleID, dbnOpts)
        Call DBNSampleProcessing.SaveOutput(dbnOut, dbnDir)
        Call Console.WriteLine($"  DBN 时间序列: 基因={dbnOut.timeSeries.NGene} x 伪时间 bin={dbnOut.timeSeries.TimePoints.Length}")

        ' ==================== ⑤ 合并 wgcna 与伪速率先验 ====================
        prior = VelocityNetwork.BuildVelocityPrior(dbnOut, prior)
        Call Console.WriteLine($"  合并后方向先验边: {prior.Edges.Count}")

        ' ==================== ⑥ 读取 WGCNA 模块划分 ====================
        Dim modules = WGCNA.ReadModuleAssignment(moduleFile)
        Call Console.WriteLine($"  WGCNA 模块分配记录数: {modules.Length}")

        ' ==================== ⑦ 模块化 DBN 子网络训练 + 全局级联虚拟扰动 ====================
        ' 显式指定扰动基因：优先从 TF 中挑选落在模块里的代表基因，不足则用时间序列前若干基因补足。
        Dim knockGenes = SelectModuleKnockGenes(dbnOut, modules, hsaTF, 5)
        If knockGenes.Length = 0 Then
            Call Console.WriteLine("[warn] 未选出任何可扰动的模块基因，跳过虚拟扰动")
            Return
        End If
        Call Console.WriteLine($"  扰动演示基因: {String.Join(", ", knockGenes)}")

        Dim grn = GeneRegulatoryNetwork.TrainModularDBNIntervene(
            dbnOut.timeSeries, modules, prior, hsaTF, knockGenes,
            dynamicSteps:=10, crossModuleCorThreshold:=0.3, outputDir:=grnDir)

        Call Console.WriteLine($"  训练模块子网络数: {grn.moduleNets.Count}")
        Call Console.WriteLine($"  全局扰动响应矩阵维度: {grn.finalResponses.Count} 源 × {If(grn.finalResponses.Values.FirstOrDefault() Is Nothing, 0, grn.finalResponses.Values.First().Length)} 基因")

        ' ==================== ⑧ Summary ====================
        Call Console.WriteLine()
        Call Console.WriteLine("=== 模块化 DBN 全局虚拟扰动流程完成 ===")
        Call Console.WriteLine($"原始表达矩阵    : {exprFile}")
        Call Console.WriteLine($"样本数          : {matrix.sampleID.Length}")
        Call Console.WriteLine($"HV 基因数       : {nHV}")
        Call Console.WriteLine($"DBN 时间序列    : {dbnDir}\dbn_timeseries.csv")
        Call Console.WriteLine($"WGCNA 模块文件  : {moduleFile}")
        Call Console.WriteLine($"扰动结果目录    : {grnDir}\ (modular_global_perturbation_responses.tsv + modular_pert_*.tsv)")
        Call Console.WriteLine("Done.")
    End Sub

    ''' <summary>
    ''' 选择用于虚拟扰动的模块基因：优先取在 prior 中作为 TF 且属于某 WGCNA 模块的基因，
    ''' 不足时退回为时间序列前若干基因。
    ''' </summary>
    Private Function SelectModuleKnockGenes(dbnOut As DBNPreprocessOutput,
                                            modules As GeneModuleColor(),
                                            hsaTF As String(),
                                            n As Integer) As String()
        Dim inModules As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each mc In modules
            If String.Equals(mc.moduleColor, "grey", StringComparison.OrdinalIgnoreCase) Then Continue For
            inModules.Add(mc.geneID)
        Next

        Dim tsGenes As New HashSet(Of String)(dbnOut.geneNames, StringComparer.OrdinalIgnoreCase)
        Dim tfSet As New HashSet(Of String)(hsaTF, StringComparer.OrdinalIgnoreCase)

        Dim picked As New List(Of String)
        ' 优先：既是 TF 又在模块里
        For Each g In dbnOut.geneNames
            If picked.Count >= n Then Exit For
            If tfSet.Contains(g) AndAlso inModules.Contains(g) Then
                picked.Add(g)
            End If
        Next
        ' 补足：在模块里的时间序列基因
        If picked.Count < n Then
            For Each g In dbnOut.geneNames
                If picked.Count >= n Then Exit For
                If inModules.Contains(g) AndAlso Not picked.Contains(g) Then
                    picked.Add(g)
                End If
            Next
        End If
        Return picked.ToArray()
    End Function
End Module
