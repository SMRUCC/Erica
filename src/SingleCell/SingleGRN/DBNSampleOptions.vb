
''' <summary>DBN 时间序列预处理的可配置参数。</summary>
Public Class DBNSampleOptions
    ''' <summary>分箱模式："bins"（等宽分箱，默认）或 "sliding"（滑动窗口）。</summary>
    Public Property method As String = "bins"
    ''' <summary>等宽分箱数量（method="bins" 时有效）。默认 30。</summary>
    Public Property numBins As Integer = 30
    ''' <summary>滑动窗口宽度（method="sliding" 时有效）。默认 5。</summary>
    Public Property windowSize As Integer = 5
    ''' <summary>滑动窗口步长（method="sliding" 时有效）。默认 1。</summary>
    Public Property [step] As Integer = 1
    ''' <summary>基因筛选方式："top"（取速度幅度最高的 topGeneFraction 比例）或 "threshold"（速度幅度 > velocityThreshold）。默认 "top"。</summary>
    Public Property geneSelection As String = "top"
    ''' <summary>top 模式下保留的基因比例（0~1）。默认 0.3。</summary>
    Public Property topGeneFraction As Double = 0.3
    ''' <summary>threshold 模式的绝对阈值；设为 NaN 时自动取速度幅度中位数 × 2。</summary>
    Public Property velocityThreshold As Double = Double.NaN
    ''' <summary>是否对 bin 表达矩阵做分位数离散化（供离散 DBN）。默认 False。</summary>
    Public Property discretize As Boolean = False
    ''' <summary>离散化等级数（discretize=True 时有效）。默认 3。</summary>
    Public Property numLevels As Integer = 3
    ''' <summary>分支标签（每细胞所属 group）；为 Nothing 时按整体单轨迹分箱。预留（本次不启用分支）。</summary>
    Public Property groupBy As Integer() = Nothing
End Class