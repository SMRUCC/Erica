Imports System.IO
Imports std = System.Math

''' <summary>
''' PseudoVelo：在 Monocle3 伪时间的基础上近似计算单细胞“伪 RNA 速率”。
''' 方法学见 <c>PseudoVelo.md</c>：
'''   ① 以 Monocle3 的 pseudotime 作为时间轴；
'''   ② 对每个基因的表达-伪时间曲线按伪时间排序后做平滑（默认滑动窗口平均）；
'''   ③ 对平滑曲线关于伪时间求导 dE/dt，得到 基因×细胞 的伪速度矩阵；
'''   ④ 基于“下游关系 + 表达/PCA 相似度”的加权转移，把细胞速度投影到 UMAP2D 坐标，供流线/箭头可视化。
''' </summary>
Public Class PseudoVelo

    ''' <summary>
    ''' 基于 Monocle3 结果计算伪 RNA 速率。
    ''' </summary>
    ''' <param name="result">Monocle3 管线结果，提供 pseudotime / umap2d / pcaScore / clusters。</param>
    ''' <param name="sampleByGene">表达矩阵（样本 × 基因），应为 RunCore 经 HV 筛选后的矩阵。</param>
    ''' <param name="geneNames">基因名（与 sampleByGene 列序一致）。</param>
    ''' <param name="sampleNames">样本名（与 sampleByGene 行序、result.pseudotime 一致）。</param>
    ''' <param name="opts">算法开关（pseudoVeloEnabled/window/useVelocityProjection 等）。</param>
    ''' <param name="cache">缓存管理；为 Nothing 时跳过缓存。</param>
    Public Shared Function Compute(result As Monocle3Result,
                                       sampleByGene As Double(,),
                                       geneNames As String(),
                                       sampleNames As String(),
                                       opts As Monocle3Options,
                                       cache As CacheStore) As PseudoVelocityResult

        Dim nCells = sampleNames.Length
        Dim nGenes = geneNames.Length

        If result.pseudotime Is Nothing OrElse result.pseudotime.Length <> nCells Then
            Throw New ArgumentException($"PseudoVelo: result.pseudotime 长度({result.pseudotime?.Length})与样本数({nCells})不一致！")
        End If
        If sampleByGene Is Nothing OrElse sampleByGene.GetLength(0) <> nCells OrElse sampleByGene.GetLength(1) <> nGenes Then
            Throw New ArgumentException($"PseudoVelo: sampleByGene 维度({sampleByGene?.GetLength(0)}x{sampleByGene?.GetLength(1)})与样本数({nCells})/基因数({nGenes})不一致！")
        End If
        If result.umap2d Is Nothing OrElse result.umap2d.GetLength(0) <> nCells Then
            Throw New ArgumentException($"PseudoVelo: result.umap2d 行数({result.umap2d?.GetLength(0)})与样本数({nCells})不一致！")
        End If

        ' 窗口半宽下限裁剪（窗宽 = 2*window+1，至少 3）
        Dim window = opts.pseudoVeloWindow
        If window < 1 Then
            window = 1
            Call Console.WriteLine($"[PseudoVelo] pseudoVeloWindow 过小，已裁剪为 {window}")
        End If

        ' 1. 按伪时间升序排序（同伪时间保持稳定顺序）
        Dim t = result.pseudotime
        Dim order = Enumerable.Range(0, nCells).OrderBy(Function(i) t(i)).ToArray()
        Dim tSorted(nCells - 1) As Double
        For i As Integer = 0 To nCells - 1
            tSorted(i) = t(order(i))
        Next

        ' 2 & 3. 逐基因：取表达 → 排序 → 平滑 → 求导 → 还原回原始样本序
        Dim velocity(nGenes - 1, nCells - 1) As Double
        Dim timer = Stopwatch.StartNew()

        For g As Integer = 0 To nGenes - 1
            ' 按排序后的伪时间顺序取该基因表达
            Dim ySorted(nCells - 1) As Double
            For i As Integer = 0 To nCells - 1
                ySorted(i) = sampleByGene(order(i), g)
            Next

            ' 平滑（默认滑动窗口平均；可在此替换为 sciBASIC# LOESS：
            '   Dim loess = Microsoft.VisualBasic.Data.Bootstrapping.DataFittings.LOESS.FitLOESS(tSorted, ySorted, opts.pseudoVeloSpan, 2)
            '   smoothed(i) = Microsoft.VisualBasic.Data.Bootstrapping.DataFittings.LOESS.PredictLOESS(loess, tSorted(i))
            '   注意需确认线性-netcore5 项目已引用 LOESS 源）
            Dim smoothed = SmoothCurve(ySorted, window)

            ' 对平滑曲线求导 dE/dt
            Dim vSorted = Derivative(smoothed, tSorted)

            ' 还原回原始样本序
            For i As Integer = 0 To nCells - 1
                velocity(g, order(i)) = vSorted(i)
            Next
        Next

        timer.[Stop]()
        Call Console.WriteLine($"[PseudoVelo] 计算伪速度矩阵完成: {nGenes} 基因 × {nCells} 细胞, 耗时 {timer.Elapsed.TotalSeconds:0.00}s")

        ' 4. 可选：把细胞速度投影到 UMAP2D
        Dim velocityUMAP As Double(,) = Nothing
        If opts.useVelocityProjection Then
            velocityUMAP = ProjectToUMAP(result, sampleByGene, geneNames, velocity, order, tSorted, sampleNames)
        End If

        Dim outResult = New PseudoVelocityResult With {
                .velocity = velocity,
                .velocityUMAP = velocityUMAP,
                .geneNames = geneNames,
                .sampleNames = sampleNames,
                .orderIndex = order,
                .window = window,
                .useProjection = opts.useVelocityProjection
            }

        ' 缓存
        If cache IsNot Nothing Then
            Call cache.SaveMatrix(Path.Combine(cache.cacheDir, "10_pseudovelo_velocity.csv"), outResult.velocity)
            Call cache.SaveLabels(Path.Combine(cache.cacheDir, "10_pseudovelo_genes.txt"), geneNames)
            Call cache.SaveLabels(Path.Combine(cache.cacheDir, "10_pseudovelo_samples.txt"), sampleNames)
            If velocityUMAP IsNot Nothing Then
                Call cache.SaveMatrix(Path.Combine(cache.cacheDir, "10_pseudovelo_umap.csv"), velocityUMAP)
            End If
            Call Console.WriteLine($"[PseudoVelo] 已缓存到 {cache.cacheDir}\10_pseudovelo_*")
        End If

        Return outResult
    End Function

    ''' <summary>
    ''' 滑动窗口平均平滑（窗口半宽 window，实际窗宽 = 2*window+1）。
    ''' 边界处收缩窗口（取可用邻域），避免端点被截断。
    ''' </summary>
    Private Shared Function SmoothCurve(y As Double(), window As Integer) As Double()
        Dim n = y.Length
        Dim smoothed(n - 1) As Double
        For i As Integer = 0 To n - 1
            Dim lo = std.Max(0, i - window)
            Dim hi = std.Min(n - 1, i + window)
            Dim sum = 0.0
            Dim cnt = 0
            For k As Integer = lo To hi
                sum += y(k)
                cnt += 1
            Next
            smoothed(i) = sum / cnt
        Next
        Return smoothed
    End Function

    ''' <summary>
    ''' 对平滑后的曲线关于伪时间 t 求导：内部点中心差分，端点前向/后向差分。
    ''' 注意：pseudotime 已归一到 0-100，导数尺度随之缩放；符号与相对大小对生物学解释无影响。
    ''' </summary>
    Private Shared Function Derivative(y As Double(), t As Double()) As Double()
        Dim n = y.Length
        Dim d(n - 1) As Double
        If n = 1 Then
            d(0) = 0.0
            Return d
        End If

        For i As Integer = 0 To n - 1
            Dim dt, dy
            If i = 0 Then
                ' 前向差分
                dt = t(i + 1) - t(i)
                dy = y(i + 1) - y(i)
            ElseIf i = n - 1 Then
                ' 后向差分
                dt = t(i) - t(i - 1)
                dy = y(i) - y(i - 1)
            Else
                ' 中心差分
                dt = t(i + 1) - t(i - 1)
                dy = y(i + 1) - y(i - 1)
            End If

            If dt = 0.0 Then
                d(i) = 0.0
            Else
                d(i) = dy / dt
            End If
        Next
        Return d
    End Function

    ''' <summary>
    ''' 步骤④：把细胞的基因级伪速度投影到 UMAP2D 坐标。
    ''' 对每个细胞 i，在其下游细胞（t(j) > t(i)）上按 PCA/表达余弦相似度加权，
    ''' 取 (umap2d(j) - umap2d(i)) 的方向均值，得到 UMAP 空间速度向量（样本×2）。
    ''' </summary>
    Private Shared Function ProjectToUMAP(result As Monocle3Result,
                                            sampleByGene As Double(,),
                                            geneNames As String(),
                                            velocity As Double(,),
                                            order As Integer(),
                                            tSorted As Double(),
                                            sampleNames As String()) As Double(,)
        Dim n = sampleNames.Length
        Dim umap = result.umap2d
        Dim pca = result.pcaScore
        Dim velUMAP(n - 1, 1) As Double

        ' 预计算 PCA 向量的 L2 范数（用于余弦相似度）
        Dim pcaNorm(n - 1) As Double
        For i As Integer = 0 To n - 1
            Dim s = 0.0
            For c As Integer = 0 To pca.GetLength(1) - 1
                s += pca(i, c) * pca(i, c)
            Next
            pcaNorm(i) = std.Sqrt(s)
        Next

        For i As Integer = 0 To n - 1
            Dim wi = pcaNorm(i)
            Dim sumX = 0.0, sumY = 0.0, sumW = 0.0

            For j As Integer = 0 To n - 1
                If j = i Then Continue For
                ' 仅下游：t(j) > t(i)
                If tSorted(j) <= tSorted(i) Then Continue For

                Dim wj = pcaNorm(j)
                If wi = 0.0 OrElse wj = 0.0 Then Continue For

                ' PCA 余弦相似度
                Dim dot = 0.0
                For c As Integer = 0 To pca.GetLength(1) - 1
                    dot += pca(i, c) * pca(j, c)
                Next
                Dim sim = dot / (wi * wj)
                ' 相似度裁剪到 [0,1]，避免负相关细胞误导方向
                If sim <= 0.0 Then Continue For

                Dim w = sim
                sumX += w * (umap(j, 0) - umap(i, 0))
                sumY += w * (umap(j, 1) - umap(i, 1))
                sumW += w
            Next

            If sumW > 0.0 Then
                velUMAP(i, 0) = sumX / sumW
                velUMAP(i, 1) = sumY / sumW
            Else
                velUMAP(i, 0) = 0.0
                velUMAP(i, 1) = 0.0
            End If
        Next

        Call Console.WriteLine("[PseudoVelo] UMAP 速度向量投影完成")
        Return velUMAP
    End Function
End Class
