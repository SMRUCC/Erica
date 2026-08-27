Imports System.IO
Imports Erica.Analysis.SingleCell.VirtualGRN

''' <summary>
''' 优化版演示：基于 WGCNA 共表达模块的 DBN 子网络训练 + 全局级联虚拟扰动。
'''
''' 复用 Program.BuildModel 的端到端链路：
'''   WGCNA 网络 + TF 列表 → BuildPriorNetwork
'''   → Monocle3 伪时间排序 + PseudoVelo 伪速率 → DBN 时间序列分箱聚合
'''   → VelocityNetwork.BuildVelocityPrior 合并 wgcna 与伪速率先验
''' 之后新增步骤：
'''   读取 WGCNA 模块划分（gene_module_assignment.csv），调用
'''   GeneRegulatoryNetwork.TrainModularDBNIntervene：
'''     按模块划分时间序列 → 每模块单独训练 DynamicBayesianNetwork 子网络
'''     → 基于模块 eigengene 关联做级联虚拟扰动 → 导出全局响应结果。
'''
''' 相比 Program.RunModel 的全局 TrainAndIntervene，本 demo 在大型 WGCNA 网络下将训练代价
''' 由 O(N^2·样本) 降为 Σ O(模块规模^2·样本)，显著提速。
'''
''' 默认用 topGeneFraction=0.3 限制参与建模的基因规模，并仅演示少量扰动基因，
''' 以便快速验证；如需跑全量大盘，将 topGeneFraction 调大到 1.0、knockGenes 取更多即可。
''' </summary>
Module ModuleDBNDemo

    Sub Run(args As String())
        ' ==================== 输入路径（沿用 WGCNADemo 的模块文件 + 本流程的表达矩阵） ====================
        Dim wgcnaEdges = "K:\hsa_grn\network-edges.csv"
        Dim exprFile = "K:\hsa\Homo_sapiens_expr_advanced_all_conditions.dat"
        Dim tfFile = "K:\hsa_grn\Homo_sapiens_TF.txt"
        Dim moduleFile = "K:\hsa\WGCNA_output-demo\gene_module_assignment.csv"
        Dim monoDir = "K:\hsa\monocle3_output_moduledbn"
        Dim grnDir = Path.Combine(monoDir, "dbn_grn")
        Dim dbnDir = Path.Combine(monoDir, "dbn_timeseries")

        If args.Length > 0 AndAlso args(0).Length > 0 Then wgcnaEdges = args(0)
        If args.Length > 1 AndAlso args(1).Length > 0 Then exprFile = args(1)
        If args.Length > 2 AndAlso args(2).Length > 0 Then moduleFile = args(2)

        If Not File.Exists(moduleFile) Then
            Call Console.WriteLine($"[error] WGCNA 模块划分文件不存在: {moduleFile}")
            Return
        End If

        ' ==================== ① 复用端到端链路构建 DBN 模型 ====================
        Call Console.WriteLine("运行 Monocle3（伪时间排序 + PseudoVelo 伪速率）并构建 DBN 时间序列...")
        Dim model = Program.BuildModel(wgcnaEdges, exprFile, tfFile, monoDir, 0.3)
        Dim dbnOut = model.dbnOut
        Dim prior = model.prior
        Dim hsaTF = model.hsaTF
        Call Console.WriteLine($"  DBN 时间序列: 基因={dbnOut.timeSeries.NGene} x 伪时间 bin={dbnOut.timeSeries.TimePoints.Length}")
        Call Console.WriteLine($"  合并后方向先验边: {prior.Edges.Count}")

        ' ==================== ② 读取 WGCNA 模块划分 ====================
        Dim modules = SMRUCC.genomics.Analysis.BNLearn.WGCNA.ReadModuleAssignment(moduleFile)
        Call Console.WriteLine($"  WGCNA 模块分配记录数: {modules.Length}")

        ' ==================== ③ 模块化 DBN 子网络训练 + 全局级联虚拟扰动 ====================
        ' 显式指定扰动基因：优先从 TF 中挑选落在模块里的代表基因，不足则用时间序列前若干基因补足。
        Dim knockGenes = SelectModuleKnockGenes(dbnOut, modules, hsaTF, 5)
        If knockGenes.Length = 0 Then
            Call Console.WriteLine("[warn] 未选出任何可扰动的模块基因，跳过虚拟扰动")
            Return
        End If
        Call Console.WriteLine($"  扰动演示基因: {String.Join(", ", knockGenes)}")

        Dim grn = SMRUCC.genomics.Analysis.CellPhenotype.GeneRegulatoryNetwork.TrainModularDBNIntervene(
            dbnOut.timeSeries, modules, prior, hsaTF, knockGenes,
            dynamicSteps:=10, crossModuleCorThreshold:=0.3, outputDir:=grnDir)

        Call Console.WriteLine($"  训练模块子网络数: {grn.moduleNets.Count}")
        Call Console.WriteLine($"  全局扰动响应矩阵维度: {grn.finalResponses.Count} 源 × {If(grn.finalResponses.Values.FirstOrDefault() Is Nothing, 0, grn.finalResponses.Values.First().Count)} 基因")

        ' ==================== ④ Summary ====================
        Call Console.WriteLine()
        Call Console.WriteLine("=== 模块化 DBN 全局虚拟扰动流程完成 ===")
        Call Console.WriteLine($"原始表达矩阵    : {exprFile}")
        Call Console.WriteLine($"样本数          : {dbnOut.timeSeries.TimePoints.Length}")
        Call Console.WriteLine($"HV 基因数       : {dbnOut.geneNames.Length}")
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
                                            modules As SMRUCC.genomics.Analysis.BNLearn.GeneModuleColor(),
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
