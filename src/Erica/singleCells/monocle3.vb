
Imports Erica.Analysis.SingleCell.Monocle3
Imports Erica.Analysis.SingleCell.VirtualGRN
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports SMRUCC.genomics.Analysis.CellPhenotype
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Vectorization
Imports std = System.Math

<Package("monocle3")>
Module monocle3Tool

    <ExportAPI("new")>
    Public Function monocle3_options(Optional numPCA As Integer = 10,
                Optional umapDim As Integer = 3,
                Optional knnK As Integer = 15,
                Optional resolution As Double = 1.0,
                Optional useLeiden As Boolean = False,
                Optional useCache As Boolean = True,
                Optional overwriteCache As Boolean = False,
                Optional cacheDir As String = "./cache",
                Optional pseudoVeloEnabled As Boolean = True,
                Optional pseudoVeloWindow As Integer = 2,
                Optional pseudoVeloSpan As Double = 0.3,
                Optional useVelocityProjection As Boolean = True,
                Optional numHVGenes As Integer = 3000) As Monocle3Options

        Return New Monocle3Options With {
            .numPCA = numPCA,
            .umapDim = umapDim,
            .knnK = knnK,
            .resolution = resolution,
            .useLeiden = useLeiden,
            .useCache = useCache,
            .overwriteCache = overwriteCache,
            .cacheDir = cacheDir,
            .pseudoVeloEnabled = pseudoVeloEnabled,
            .pseudoVeloWindow = pseudoVeloWindow,
            .pseudoVeloSpan = pseudoVeloSpan,
            .useVelocityProjection = useVelocityProjection,
            .numHVGenes = numHVGenes
        }
    End Function

    <ExportAPI("cell_rank")>
    Public Function cell_rank(x As Matrix, opts As Monocle3Options) As Monocle3Result
        Return Monocle3.Run(x, opts)
    End Function

    <ExportAPI("hvgenes")>
    Public Function get_hvgenes(x As Monocle3Result) As String()
        ' ==================== ③ 提取 HV 基因表达（log1p，与 Monocle3 内部尺度一致） ====================
        ' Monocle3.RunCore 内部对表达做 log1p 后再算伪时间/速度，故 DBN 时间序列也用 log1p 表达，
        ' 使分箱聚合的表达与 velocity 同源；velocity 缺失时回退为全基因表达。
        Dim hvGenes As String()

        If x.pseudoVelocity IsNot Nothing AndAlso
            x.pseudoVelocity.geneNames IsNot Nothing AndAlso
            x.pseudoVelocity.geneNames.Length > 0 Then

            hvGenes = x.pseudoVelocity.geneNames
        Else
            hvGenes = x.featureIds
        End If

        Return hvGenes
    End Function

    <ExportAPI("dbn_sample")>
    Public Function dbn_sample(matrix As Matrix, hvgenes As String()) As NumericMatrix
        Dim nSamples = matrix.sampleID.Length
        Dim nHV = hvgenes.Length
        Dim geneRow As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For r As Integer = 0 To matrix.expression.Length - 1
            geneRow(matrix.expression(r).geneID) = r
        Next

        Dim sampleByGene As New NumericMatrix(nSamples, nHV)
        For g As Integer = 0 To nHV - 1
            If Not geneRow.ContainsKey(hvgenes(g)) Then
                Call Console.WriteLine($"[warn] HV 基因 {hvgenes(g)} 不在原始矩阵中，已跳过")
                Continue For
            End If
            Dim row = geneRow(hvgenes(g))
            For j As Integer = 0 To nSamples - 1
                ' log1p：与 Monocle3 内部 exprData 尺度一致
                sampleByGene(j, g) = std.Log(1 + matrix.expression(row).experiments(j))
            Next
        Next
        Call Console.WriteLine($"  HV 基因表达矩阵: 样本={nSamples} x 基因={nHV} (log1p)")

        Return sampleByGene
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="monocle3"></param>
    ''' <param name="dbn_sample"></param>
    ''' <param name="hvgenes"></param>
    ''' <param name="method">分箱模式："bins"（等宽分箱，默认）或 "sliding"（滑动窗口）。</param>
    ''' <param name="numBins">等宽分箱数量（method="bins" 时有效）。默认 30。</param>
    ''' <param name="windowSize">滑动窗口宽度（method="sliding" 时有效）。默认 5。</param>
    ''' <param name="step">滑动窗口步长（method="sliding" 时有效）。默认 1。</param>
    ''' <param name="geneSelection">基因筛选方式："top"（取速度幅度最高的 topGeneFraction 比例）或 "threshold"（速度幅度 > velocityThreshold）。默认 "top"。</param>
    ''' <param name="topGeneFraction">top 模式下保留的基因比例（0~1）。默认 0.3。</param>
    ''' <param name="velocityThreshold">threshold 模式的绝对阈值；设为 NaN 时自动取速度幅度中位数 × 2。</param>
    ''' <param name="discretize">是否对 bin 表达矩阵做分位数离散化（供离散 DBN）。默认 False。</param>
    ''' <param name="numLevels">离散化等级数（discretize=True 时有效）。默认 3。</param>
    ''' <param name="groupBy">分支标签（每细胞所属 group）；为 Nothing 时按整体单轨迹分箱。预留（本次不启用分支）。</param>
    ''' <returns></returns>
    ''' 
    <ExportAPI("make_sample")>
    Public Function make_sample(monocle3 As Monocle3Result, dbn_sample As NumericMatrix, <RRawVectorArgument(TypeCodes.string)> hvgenes As Object,
                                Optional method As String = "bins",
                                Optional numBins As Integer = 30,
                                Optional windowSize As Integer = 5,
                                Optional [step] As Integer = 1,
                                Optional geneSelection As String = "top",
                                Optional topGeneFraction As Double = 0.3,
                                Optional velocityThreshold As Double = Double.NaN,
                                Optional discretize As Boolean = False,
                                Optional numLevels As Integer = 3,
                                Optional groupBy As Integer() = Nothing) As DBNPreprocessOutput

        Dim dbnOpts As New DBNSampleOptions With {
            .discretize = discretize,
            .geneSelection = geneSelection,
            .groupBy = groupBy,
            .method = method,
            .numBins = numBins,
            .numLevels = numLevels,
            .[step] = [step],
            .topGeneFraction = topGeneFraction,
            .velocityThreshold = velocityThreshold,
            .windowSize = windowSize
        }
        Dim sampleByGene As Double(,) = dbn_sample.ToMatrix
        Dim hvGeneIds As String() = CLRVector.asCharacter(hvgenes)
        Dim dbnOut As DBNPreprocessOutput = DBNSampleProcessing.BuildFromMonocle3(monocle3, sampleByGene, hvGeneIds, monocle3.sampleNames, dbnOpts)
        Return dbnOut
    End Function

    <ExportAPI("merge_prior")>
    Public Function merge_prior(velocity_prior As DBNPreprocessOutput, prior As PriorNetwork) As PriorNetwork
        Return VelocityNetwork.BuildVelocityPrior(velocity_prior, prior)
    End Function

    <ExportAPI("learn_grn")>
    Public Function make_grn(velocity_prior As DBNPreprocessOutput, prior As PriorNetwork) As BNLearnWorkflow
        Dim expr = velocity_prior.timeSeries
        Dim workflow As BNLearnWorkflow = GeneRegulatoryNetwork.BuildExpressionGRN(expr, prior)

        ' ① 结构学习（MMHC + 白名单先验）
        Call workflow.LearnStructure()
        ' ② 参数学习（高斯 BN MLE）
        Call workflow.LearnParameters()

        Call $"GRN.TrainAndIntervene: 网络训练完成（基因 {expr.NGene}, 伪时间点 {expr.TimePoints.Length}）".info

        Return workflow
    End Function
End Module
