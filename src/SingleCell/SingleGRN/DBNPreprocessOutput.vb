
Imports SMRUCC.genomics.Analysis.BNLearn.Core

''' <summary>预处理结果。</summary>
Public Class DBNPreprocessOutput
    ''' <summary>DBN 时间序列（基因 × Kbin，TimePoints = bin 平均伪时间），可直接喂 GeneRegulatoryNetwork.ToTimeSeries。</summary>
    Public Property timeSeries As GeneExpressionData
    ''' <summary>每个 bin 内聚合的基因伪速度矩阵（基因 × bin），供 DBN 因果方向先验。</summary>
    Public Property binVelocity As Double(,)
    ''' <summary>每基因沿轨迹的整体趋势方向（sign(mean bin velocity)）。</summary>
    Public Property trendSign As Double()
    ''' <summary>筛选后保留的基因名（与 timeSeries.Matrix 行序一致）。</summary>
    Public Property selectedGenes As String()
    ''' <summary>被剔除的基因名。</summary>
    Public Property geneExcluded As String()
    ''' <summary>每基因的速度幅度统计（与 geneNames 顺序一致）。</summary>
    Public Property speedStat As Double()
    ''' <summary>bin 标签（"bin_1".."bin_K"）。</summary>
    Public Property binLabels As String()
    ''' <summary>每个 bin 的伪时间点（平均伪时间）。</summary>
    Public Property binTimePoints As Double()
    ''' <summary>原始样本名（与输入一致）。</summary>
    Public Property sampleNames As String()
    ''' <summary>原始基因名（与输入一致）。</summary>
    Public Property geneNames As String()

    Public Overrides Function ToString() As String
        Dim k = If(binTimePoints Is Nothing, 0, binTimePoints.Length)
        Return $"DBNPreprocessOutput(genes={selectedGenes.Length}/{geneNames.Length}, bins={k})"
    End Function
End Class