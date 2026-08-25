Imports System.IO
Imports Erica.Analysis.SingleCell.Monocle3
Imports SMRUCC.genomics.Analysis.BNLearn.Core
Imports std = System.Math

''' <summary>
''' 把 Monocle3 伪时间排序结果 + PseudoVelo 伪速率作为"准时间轴/因果先验"，
''' 对单细胞表达矩阵做预处理，构造动态贝叶斯网络（DBN）所需的时间序列数据。
''' 方法学见 <c>DBN-PreProcessing.md</c>：
'''   ① 基因预筛选（按伪速率幅度/沿轨迹方差筛选有动力学变化的基因）
'''   ② 分箱/滑动窗口聚合（连续伪时间 → K 个离散"伪时间点"）
'''   ③ 离散化（可选，供离散 DBN）
'''   ④ 输出每个 bin 的基因伪速度聚合值与趋势方向（供 DBN 因果白名单/方向先验）
'''   ⑤ 分支（本次整体单轨迹，不分支）
''' </summary>
Public Module DBNSampleProcessing

    ' ==================== 公开入口 ====================

    ''' <summary>
    ''' 从 Monocle3 内存对象直接构建 DBN 时间序列。
    ''' </summary>
    ''' <param name="result">Monocle3 结果（提供 pseudotime 与 pseudoVelocity.velocity）。</param>
    ''' <param name="sampleByGene">表达矩阵（样本 × 基因）。</param>
    ''' <param name="geneNames">基因名（与 sampleByGene 列序一致）。</param>
    ''' <param name="sampleNames">样本名（与 sampleByGene 行序、result.pseudotime 一致）。</param>
    Public Function BuildFromMonocle3(result As Monocle3Result,
                                      sampleByGene As Double(,),
                                      geneNames As String(),
                                      sampleNames As String(),
                                      opts As DBNSampleOptions) As DBNPreprocessOutput
        Dim velocity = If(result.pseudoVelocity Is Nothing, Nothing, result.pseudoVelocity.velocity)
        Return BuildCore(sampleByGene, geneNames, sampleNames, result.pseudotime, velocity, opts)
    End Function

    ''' <summary>
    ''' 从 Monocle3 导出的 CSV 文件构建 DBN 时间序列。
    ''' </summary>
    ''' <param name="expressionCsv">基因×样本表达矩阵（首列基因名，其余列为样本）。默认 02_gene_by_cell.csv。</param>
    ''' <param name="pseudotimeCsv">伪时间文件：sampleinfo.csv（含 mon_pseudotime 列）或 07_pseudotime.csv（含 sample + mon_pseudotime）。</param>
    ''' <param name="velocityCsv">基因×细胞伪速度矩阵（首列基因名，其余列为样本）。默认 pseudovelo_velocity.csv。</param>
    Public Function BuildFromFiles(expressionCsv As String,
                                   pseudotimeCsv As String,
                                   velocityCsv As String,
                                   opts As DBNSampleOptions) As DBNPreprocessOutput
        ' ByRef 输出参数需先声明局部变量
        Dim geneNames As String() = Nothing
        Dim sampleNames As String() = Nothing
        Dim velGenes As String() = Nothing
        Dim velSamples As String() = Nothing

        ' 1. 读表达矩阵（基因 × 样本）→ 转 样本 × 基因
        Dim exprMat = ReadGeneBySampleMatrix(expressionCsv, geneNames, sampleNames)

        ' 2. 读伪时间（按样本名对齐）
        Dim pseudotime = ReadPseudotime(pseudotimeCsv, sampleNames)

        ' 3. 读伪速度（基因 × 细胞，列序=样本名顺序）→ 已与 sampleNames 对齐
        Dim velocity = If(File.Exists(velocityCsv), ReadGeneBySampleMatrix(velocityCsv, velGenes, velSamples), Nothing)
        If velocity IsNot Nothing Then
            ' 用样本名对齐（velocity 的列可能是 sampleNames 顺序；若不一致则按 velSamples 重排）
            velocity = AlignColumns(velocity, velGenes, velSamples, geneNames, sampleNames)
        End If

        Return BuildCore(exprMat, geneNames, sampleNames, pseudotime, velocity, opts)
    End Function

    ' ==================== 核心算法 ====================

    ''' <summary>
    ''' 核心：基因预筛选 + 分箱/滑动窗口聚合 + 可选离散化 + 速度方向先验。
    ''' </summary>
    ''' <param name="exprCellByGene">表达矩阵（样本 × 基因）。</param>
    ''' <param name="pseudotime">每个样本的伪时间（与样本序一致）。</param>
    ''' <param name="velocityGeneByCell">伪速度矩阵（基因 × 细胞）；可空。</param>
    Private Function BuildCore(exprCellByGene As Double(,),
                               geneNames As String(),
                               sampleNames As String(),
                               pseudotime As Double(),
                               velocityGeneByCell As Double(,),
                               opts As DBNSampleOptions) As DBNPreprocessOutput

        Dim nSamples = sampleNames.Length
        Dim nGenes = geneNames.Length

        If exprCellByGene.GetLength(0) <> nSamples OrElse exprCellByGene.GetLength(1) <> nGenes Then
            Throw New ArgumentException($"DBNSampleProcessing: exprCellByGene 维度({exprCellByGene.GetLength(0)}x{exprCellByGene.GetLength(1)})与样本数({nSamples})/基因数({nGenes})不一致！")
        End If
        If pseudotime.Length <> nSamples Then
            Throw New ArgumentException($"DBNSampleProcessing: pseudotime 长度({pseudotime.Length})与样本数({nSamples})不一致！")
        End If
        If velocityGeneByCell IsNot Nothing AndAlso
           (velocityGeneByCell.GetLength(0) <> nGenes OrElse velocityGeneByCell.GetLength(1) <> nSamples) Then
            Throw New ArgumentException($"DBNSampleProcessing: velocity 维度({velocityGeneByCell.GetLength(0)}x{velocityGeneByCell.GetLength(1)})与基因数({nGenes})/样本数({nSamples})不一致！")
        End If

        ' ---- 步骤① 基因预筛选 ----
        Dim speedStat(nGenes - 1) As Double
        For g As Integer = 0 To nGenes - 1
            If velocityGeneByCell IsNot Nothing Then
                ' 速度幅度均值
                Dim s = 0.0
                For j As Integer = 0 To nSamples - 1
                    s += std.Abs(velocityGeneByCell(g, j))
                Next
                speedStat(g) = If(nSamples > 0, s / nSamples, 0.0)
            Else
                ' 速度不可用：用表达沿伪时间的方差替代动力学幅度
                speedStat(g) = VarianceAlongPseudotime(exprCellByGene, g, pseudotime, nSamples)
            End If
        Next

        Dim selectedIdx = SelectGenes(speedStat, opts)
        Dim selectedGenes = selectedIdx.Select(Function(i) geneNames(i)).ToArray()

        ' ---- 步骤② 分箱：确定每个样本落入的 bin 索引 ----
        Dim binOfSample = AssignBins(pseudotime, opts)
        Dim nBins = binOfSample.Max() + 1

        ' 统计每个 bin 的样本集合
        Dim binMembers(nBins - 1) As List(Of Integer)
        For b As Integer = 0 To nBins - 1
            binMembers(b) = New List(Of Integer)()
        Next
        For j As Integer = 0 To nSamples - 1
            binMembers(binOfSample(j)).Add(j)
        Next

        ' bin 时间标签 = 组内平均伪时间
        Dim binTimePoints(nBins - 1) As Double
        Dim binLabels(nBins - 1) As String
        For b As Integer = 0 To nBins - 1
            Dim avg = 0.0
            For Each j In binMembers(b)
                avg += pseudotime(j)
            Next
            binTimePoints(b) = If(binMembers(b).Count > 0, avg / binMembers(b).Count, 0.0)
            binLabels(b) = $"bin_{b + 1}"
        Next

        ' 聚合表达（仅选中基因）：binMatrix(选中基因, bin)
        Dim nSel = selectedGenes.Length
        Dim binMatrix(nSel - 1, nBins - 1) As Double
        Dim binVelocityMat As Double(,) = Nothing
        If velocityGeneByCell IsNot Nothing Then
            binVelocityMat = New Double(nSel - 1, nBins - 1) {}
        End If

        For b As Integer = 0 To nBins - 1
            Dim members = binMembers(b)
            If members.Count = 0 Then
                ' 空 bin：回退为 0 并告警（不影响 TimePoints 连续性）
                Call Console.WriteLine($"[DBN-Pre] warn: bin {b} 无样本，表达/速度置 0")
                Continue For
            End If
            For si As Integer = 0 To nSel - 1
                Dim g = selectedIdx(si)
                Dim sumE = 0.0, sumV = 0.0
                For Each j In members
                    sumE += exprCellByGene(j, g)
                    If velocityGeneByCell IsNot Nothing Then
                        sumV += velocityGeneByCell(g, j)
                    End If
                Next
                binMatrix(si, b) = sumE / members.Count
                If velocityGeneByCell IsNot Nothing Then
                    binVelocityMat(si, b) = sumV / members.Count
                End If
            Next
        Next

        ' ---- 步骤③ 离散化（可选） ----
        If opts.discretize Then
            DiscretizeMatrix(binMatrix, opts.numLevels)
        End If

        ' ---- 步骤④ 速度方向先验 ----
        Dim trendSign(nSel - 1) As Double
        If binVelocityMat IsNot Nothing Then
            For si As Integer = 0 To nSel - 1
                Dim s = 0.0
                For b As Integer = 0 To nBins - 1
                    s += binVelocityMat(si, b)
                Next
                trendSign(si) = If(s > 0, 1.0, If(s < 0, -1.0, 0.0))
            Next
        Else
            For si As Integer = 0 To nSel - 1
                trendSign(si) = 0.0
            Next
        End If

        ' 组装 GeneExpressionData（基因 × bin）
        Dim ged As New GeneExpressionData With {
            .GeneNames = selectedGenes,
            .SampleNames = binLabels,
            .Matrix = binMatrix,
            .TimePoints = binTimePoints
        }

        Dim excludedIdx = Enumerable.Range(0, nGenes).Except(selectedIdx).ToArray()
        Dim out = New DBNPreprocessOutput With {
            .timeSeries = ged,
            .binVelocity = binVelocityMat,
            .trendSign = trendSign,
            .selectedGenes = selectedGenes,
            .geneExcluded = excludedIdx.Select(Function(i) geneNames(i)).ToArray(),
            .speedStat = speedStat,
            .binLabels = binLabels,
            .binTimePoints = binTimePoints,
            .sampleNames = sampleNames,
            .geneNames = geneNames
        }

        Call Console.WriteLine($"[DBN-Pre] 完成: {out}, method={opts.method}")
        Return out
    End Function

    ' ==================== 内部辅助 ====================

    ''' <summary>按速度幅度/方差筛选基因，返回选中基因的下标集合。</summary>
    Private Function SelectGenes(speedStat As Double(), opts As DBNSampleOptions) As Integer()
        Dim n = speedStat.Length
        Dim idx = Enumerable.Range(0, n).ToArray()

        If opts.geneSelection = "threshold" Then
            Dim thr = opts.velocityThreshold
            If Double.IsNaN(thr) Then
                ' 自动：速度幅度中位数 × 2
                Dim sorted = CType(speedStat.Clone(), Double())
                Array.Sort(sorted)
                Dim median = If(n Mod 2 = 1, sorted(n \ 2), (sorted(n \ 2 - 1) + sorted(n \ 2)) / 2.0)
                thr = median * 2.0
                Call Console.WriteLine($"[DBN-Pre] 自动速度阈值 = {thr:G6} (中位数的2倍)")
            End If
            Return idx.Where(Function(i) speedStat(i) > thr).ToArray()
        Else
            ' 默认 top：按速度幅度降序取前 topGeneFraction 比例
            Dim frac = std.Max(0.0, std.Min(1.0, opts.topGeneFraction))
            Dim k = std.Max(1, CInt(std.Round(n * frac)))
            Dim ordered = idx.OrderByDescending(Function(i) speedStat(i)).ToArray()
            Return ordered.Take(k).ToArray()
        End If
    End Function

    ''' <summary>将样本分配到 bin（整体单轨迹）。返回每个样本落入的 bin 下标。</summary>
    Private Function AssignBins(pseudotime As Double(), opts As DBNSampleOptions) As Integer()
        Dim n = pseudotime.Length
        Dim binOf(n - 1) As Integer

        If opts.method = "sliding" Then
            ' 滑动窗口：按伪时间升序，窗口宽 windowSize，步长 step；每个样本归入其所在窗口的"窗口序号"
            Dim order = Enumerable.Range(0, n).OrderBy(Function(i) pseudotime(i)).ToArray()
            Dim w = std.Max(1, opts.windowSize)
            Dim stepSize = std.Max(1, opts.[step])
            Dim winIdx = 0
            Dim start = 0
            While start < n
                Dim [end] = std.Min(n - 1, start + w - 1)
                For p As Integer = start To [end]
                    binOf(order(p)) = winIdx
                Next
                winIdx += 1
                start += stepSize
            End While
        Else
            ' 等宽分箱
            Dim k = std.Max(1, opts.numBins)
            Dim tmin = pseudotime.Min()
            Dim tmax = pseudotime.Max()
            Dim width = (tmax - tmin) / k
            If width = 0.0 Then
                ' 所有伪时间相同：全部归入 bin 0
                For i As Integer = 0 To n - 1
                    binOf(i) = 0
                Next
            Else
                For i As Integer = 0 To n - 1
                    Dim b = CInt(std.Floor((pseudotime(i) - tmin) / width))
                    If b < 0 Then b = 0
                    If b > k - 1 Then b = k - 1
                    binOf(i) = b
                Next
            End If
        End If

        Return binOf
    End Function

    ''' <summary>基因表达沿伪时间的方差（速度不可用时的动力学幅度代理）。</summary>
    Private Function VarianceAlongPseudotime(exprCellByGene As Double(,), g As Integer, pseudotime As Double(), nSamples As Integer) As Double
        If nSamples = 0 Then Return 0.0
        Dim mean = 0.0
        For j As Integer = 0 To nSamples - 1
            mean += exprCellByGene(j, g)
        Next
        mean /= nSamples
        Dim v = 0.0
        For j As Integer = 0 To nSamples - 1
            Dim d = exprCellByGene(j, g) - mean
            v += d * d
        Next
        Return v / nSamples
    End Function

    ''' <summary>对矩阵每基因按分位数做 numLevels 级离散化（0..numLevels-1）。</summary>
    Private Sub DiscretizeMatrix(matrix As Double(,), numLevels As Integer)
        Dim nG = matrix.GetLength(0)
        Dim nB = matrix.GetLength(1)
        If numLevels < 2 Then Return
        For g As Integer = 0 To nG - 1
            Dim col(nB - 1) As Double
            For b As Integer = 0 To nB - 1
                col(b) = matrix(g, b)
            Next
            Array.Sort(col)
            Dim edges(numLevels - 2) As Double
            For l As Integer = 1 To numLevels - 1
                edges(l - 1) = col(CInt((l * nB) / numLevels) - 1)
            Next
            For b As Integer = 0 To nB - 1
                Dim x = matrix(g, b)
                Dim lvl = 0
                For l As Integer = 0 To numLevels - 2
                    If x > edges(l) Then lvl += 1
                Next
                matrix(g, b) = lvl
            Next
        Next
    End Sub

    ' ==================== CSV 读写辅助 ====================

    ''' <summary>
    ''' 读取"基因 × 样本"矩阵 CSV（首列基因名，其余列=样本；首行=表头，首列外为样本名）。
    ''' 返回 样本 × 基因 矩阵（转置）。geneNames/sampleNames 通过 ByRef 输出。
    ''' </summary>
    Private Function ReadGeneBySampleMatrix(csvPath As String, ByRef geneNames As String(), ByRef sampleNames As String()) As Double(,)
        Dim lines = File.ReadAllLines(csvPath)
        If lines.Length < 2 Then
            Throw New ArgumentException($"DBNSampleProcessing: CSV 行数不足: {csvPath}")
        End If

        ' 首行表头：第一格通常是占位（如 "gene"），其余为样本名
        Dim header = lines(0).Split(","c)
        sampleNames = header.Skip(1).ToArray()

        Dim nGenes = lines.Length - 1
        Dim nSamples = sampleNames.Length
        Dim expr(nSamples - 1, nGenes - 1) As Double
        Dim genes(nGenes - 1) As String

        For r As Integer = 1 To lines.Length - 1
            Dim parts = lines(r).Split(","c)
            genes(r - 1) = parts(0)
            For c As Integer = 1 To nSamples
                Dim v As Double
                If Not Double.TryParse(parts(c), v) Then v = 0.0
                ' 写入 样本×基因：样本 j = c-1，基因 g = r-1
                expr(c - 1, r - 1) = v
            Next
        Next

        geneNames = genes
        Return expr
    End Function

    ''' <summary>从伪时间 CSV 读取并按 sampleNames 顺序对齐（支持 sampleinfo.csv 或 07_pseudotime.csv）。</summary>
    Private Function ReadPseudotime(pseudotimeCsv As String, sampleNames As String()) As Double()
        Dim lines = File.ReadAllLines(pseudotimeCsv)
        If lines.Length < 2 Then
            Throw New ArgumentException($"DBNSampleProcessing: 伪时间 CSV 行数不足: {pseudotimeCsv}")
        End If

        Dim header = lines(0).Split(","c)
        ' 找到样本名列与伪时间列（mon_pseudotime 或 pseudotime）
        Dim sampleCol = -1
        Dim timeCol = -1
        For i As Integer = 0 To header.Length - 1
            Dim h = header(i).Trim().ToLower()
            If h = "sample" OrElse h = "id" OrElse h = "sample_name" Then sampleCol = i
            If h = "mon_pseudotime" OrElse h = "pseudotime" Then timeCol = i
        Next
        If sampleCol < 0 OrElse timeCol < 0 Then
            Throw New ArgumentException($"DBNSampleProcessing: 伪时间 CSV 表头缺少 sample/ID 与 mon_pseudotime 列: {lines(0)}")
        End If

        ' 建立 样本名 → 伪时间 映射
        Dim map As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        For r As Integer = 1 To lines.Length - 1
            Dim parts = lines(r).Split(","c)
            Dim name = parts(sampleCol).Trim()
            Dim v As Double
            If Not Double.TryParse(parts(timeCol), v) Then v = 0.0
            map(name) = v
        Next

        Dim pseudotime(sampleNames.Length - 1) As Double
        For j As Integer = 0 To sampleNames.Length - 1
            If Not map.TryGetValue(sampleNames(j), pseudotime(j)) Then
                Throw New ArgumentException($"DBNSampleProcessing: 样本 '{sampleNames(j)}' 在伪时间文件中缺失")
            End If
        Next
        Return pseudotime
    End Function

    ''' <summary>用样本名把基因×样本 速度矩阵重排为与 (geneNames, sampleNames) 完全对齐。</summary>
    Private Function AlignColumns(velocity As Double(,),
                                 velGenes As String(),
                                 velSamples As String(),
                                 geneNames As String(),
                                 sampleNames As String()) As Double(,)
        ' 基因顺序：velocity 行序应=geneNames（二者均来自相同表达矩阵基因集）；做映射保险
        Dim gIndex As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For i As Integer = 0 To velGenes.Length - 1
            gIndex(velGenes(i)) = i
        Next
        Dim sIndex As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For j As Integer = 0 To velSamples.Length - 1
            sIndex(velSamples(j)) = j
        Next

        Dim out(geneNames.Length - 1, sampleNames.Length - 1) As Double
        For gi As Integer = 0 To geneNames.Length - 1
            If Not gIndex.ContainsKey(geneNames(gi)) Then Continue For
            Dim gv = gIndex(geneNames(gi))
            For sj As Integer = 0 To sampleNames.Length - 1
                If sIndex.ContainsKey(sampleNames(sj)) Then
                    out(gi, sj) = velocity(gv, sIndex(sampleNames(sj)))
                End If
            Next
        Next
        Return out
    End Function

    ' ==================== 落盘 ====================

    ''' <summary>把预处理结果写出为 DBN 时间序列 CSV、bin 速度 CSV、基因筛选 CSV。</summary>
    Public Sub SaveOutput(out As DBNPreprocessOutput, dir As String)
        Call Directory.CreateDirectory(dir)

        ' 1. DBN 时间序列（基因 × bin，首列基因名）
        Dim tsPath = Path.Combine(dir, "dbn_timeseries.csv")
        Using sw As New StreamWriter(tsPath)
            Dim header = "gene"
            For Each b In out.binLabels
                header &= "," & b
            Next
            Call sw.WriteLine(header)
            Dim m = out.timeSeries.Matrix
            For gi As Integer = 0 To out.selectedGenes.Length - 1
                Dim line = out.selectedGenes(gi)
                For b As Integer = 0 To out.binLabels.Length - 1
                    line &= "," & m(gi, b).ToString("G17")
                Next
                Call sw.WriteLine(line)
            Next
        End Using

        ' 2. bin 伪速度（基因 × bin）
        If out.binVelocity IsNot Nothing Then
            Dim vPath = Path.Combine(dir, "dbn_velocity_bins.csv")
            Using sw As New StreamWriter(vPath)
                Dim header = "gene"
                For Each b In out.binLabels
                    header &= "," & b
                Next
                Call sw.WriteLine(header)
                For gi As Integer = 0 To out.selectedGenes.Length - 1
                    Dim line = out.selectedGenes(gi)
                    For b As Integer = 0 To out.binLabels.Length - 1
                        line &= "," & out.binVelocity(gi, b).ToString("G17")
                    Next
                    Call sw.WriteLine(line)
                Next
            End Using
        End If

        ' 3. 基因筛选与趋势方向
        Dim gPath = Path.Combine(dir, "dbn_gene_selection.csv")
        Using sw As New StreamWriter(gPath)
            Call sw.WriteLine("gene,selected,speedStat,trendSign")
            For gi As Integer = 0 To out.geneNames.Length - 1
                Dim sel = Array.IndexOf(out.selectedGenes, out.geneNames(gi)) >= 0
                Dim trend = If(gi < out.trendSign.Length, out.trendSign(gi), 0.0)
                Call sw.WriteLine($"{out.geneNames(gi)},{sel},{out.speedStat(gi):G6},{trend}")
            Next
        End Using

        Call Console.WriteLine($"[DBN-Pre] 已写出: {tsPath}")
    End Sub
End Module
