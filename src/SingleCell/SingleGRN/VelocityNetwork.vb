Imports System.Runtime.CompilerServices
Imports SMRUCC.genomics.Analysis.BNLearn
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports std = System.Math

Public Module VelocityNetwork

    ' ==================== 调控网络 / 虚拟扰动辅助 ====================

    ''' <summary>
    ''' 由 DBN 预处理结果中的伪速率趋势（trendSign，按 geneNames 顺序）构造因果方向先验。
    ''' 启发式：取趋势幅度 |trend| 最大的 Top50 选中基因，按趋势正负分为上游（正）/下游（负），
    ''' 在上游→下游间连激活边（权重为两侧 |trend| 均值，evidence="PseudoVelo trend"）。
    ''' 该先验为弱方向约束，缺失（候选不足或全同号）时返回空网络，退化为纯数据驱动 MMHC。
    ''' </summary>
    <Extension>
    Public Function BuildVelocityPrior(dbnOut As DBNPreprocessOutput， prior As PriorNetwork) As PriorNetwork
        If dbnOut Is Nothing OrElse dbnOut.selectedGenes Is Nothing OrElse dbnOut.trendSign Is Nothing Then
            Return prior
        End If

        ' trendSign(i) 与 selectedGenes(i) 一一对应
        Dim genes = dbnOut.selectedGenes
        Dim trend = dbnOut.trendSign
        Dim pairs As New List(Of (gene As String, t As Double))
        For i As Integer = 0 To genes.Length - 1
            If i < trend.Length Then
                pairs.Add((gene:=genes(i), t:=trend(i)))
            End If
        Next

        ' 取趋势幅度 |t| 最大的 Top50 候选
        Dim sel = pairs.OrderByDescending(Function(x) std.Abs(x.t)).Take(50).ToArray()

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
                prior.AddEdge(p.gene, n.gene, Effector.Activator, (std.Abs(p.t) + std.Abs(n.t)) / 2.0, "PseudoVelo trend")
                edges += 1
                If edges >= maxEdges Then Exit For
            Next
            If edges >= maxEdges Then Exit For
        Next

        Call Console.WriteLine($"  [prior] 由伪速率趋势构造方向先验边 {prior.Edges.Count} (候选 {sel.Length}: 正 {pos.Length} / 负 {neg.Length})")
        Return prior
    End Function

    ''' <summary>
    ''' 由 DBN 预处理结果中的伪速率趋势（trendSign，按 geneNames 顺序）构造因果方向先验。
    ''' 启发式：取趋势幅度 |trend| 最大的 Top50 选中基因，按趋势正负分为上游（正）/下游（负），
    ''' 在上游→下游间连激活边（权重为两侧 |trend| 均值，evidence="PseudoVelo trend"）。
    ''' 该先验为弱方向约束，缺失（候选不足或全同号）时返回空网络，退化为纯数据驱动 MMHC。
    ''' </summary>
    <Extension>
    Public Function BuildVelocityPrior(dbnOut As DBNPreprocessOutput) As PriorNetwork
        Dim prior As New PriorNetwork()

        If dbnOut Is Nothing OrElse dbnOut.selectedGenes Is Nothing OrElse dbnOut.trendSign Is Nothing Then
            Return prior
        Else
            Return BuildVelocityPrior(dbnOut, prior)
        End If
    End Function
End Module
