Imports Erica.Analysis.SingleCell.Monocle3
Imports Erica.Analysis.SingleCell.VirtualGRN
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports SMRUCC.genomics.Analysis.CellPhenotype
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Vectorization

<Package("GRN")>
Module GRN

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
    Public Function make_grn(velocity_prior As DBNPreprocessOutput, prior As PriorNetwork, Optional maxIters As Integer = 500) As BNLearnWorkflow
        Dim expr = velocity_prior.timeSeries
        Dim workflow As BNLearnWorkflow = GeneRegulatoryNetwork.BuildExpressionGRN(expr, prior)

        workflow.StructureParams.MaxIterations = maxIters

        ' ① 结构学习（MMHC + 白名单先验）
        Call workflow.LearnStructure()
        ' ② 参数学习（高斯 BN MLE）
        Call workflow.LearnParameters()

        Call $"GRN.TrainAndIntervene: 网络训练完成（基因 {expr.NGene}, 伪时间点 {expr.TimePoints.Length}）".info

        Return workflow
    End Function
End Module
