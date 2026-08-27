Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports std = System.Math
Imports System.Threading.Tasks

''' <summary>
''' 针对 <see cref="Matrix"/> 的预处理与坐标转换辅助方法。
''' 
''' 约定：GCModeller 的 <see cref="Matrix"/> 内部以“行=基因、列=样本”存储，
''' 而 PCA / UMAP 等降维模块要求“行=样本、列=特征（基因）”。本模块统一在
''' 此完成转置，避免维度错配散落在各步骤中。
''' </summary>
Public Module MatrixExtensions

    ''' <summary>
    ''' 将表达矩阵转置为 [样本 × 基因] 的 Double 矩阵（行=样本，列=基因）。
    ''' 仅保留在至少 <paramref name="minSamples"/> 个样本中表达量 > 0 的基因
    ''' （低表达过滤），并可选做 log1p 归一化（log(1 + x)）。
    ''' </summary>
    Public Function ToSampleByGeneMatrix(matrix As Matrix,
                                         Optional minSamples As Integer = 1,
                                         Optional logNormalize As Boolean = True,
                                         Optional opts As Monocle3Options = Nothing) As Double(,)
        Dim genes = matrix.expression
        Dim nGenes = genes.Length
        Dim nSamples = matrix.sampleID.Length

        ' 低表达过滤：统计每个基因在多少样本中非零（按基因独立，可并行）。
        ' 用布尔标志数组收集结果，避免并行写共享 List 的竞争。
        Dim keepFlag(nGenes - 1) As Boolean
        Dim checkGene = Sub(g As Integer)
            Dim expr = genes(g).experiments
            Dim nonzero = 0
            For s As Integer = 0 To nSamples - 1
                If expr(s) > 0 Then
                    nonzero += 1
                    If nonzero >= minSamples Then Exit For
                End If
            Next
            keepFlag(g) = nonzero >= minSamples
        End Sub
        If opts Is Nothing OrElse opts.parallelEnabled Then
            Parallel.For(0, nGenes, checkGene)
        Else
            For g As Integer = 0 To nGenes - 1
                checkGene(g)
            Next
        End If

        Dim kept As New List(Of Integer)
        For g As Integer = 0 To nGenes - 1
            If keepFlag(g) Then kept.Add(g)
        Next

        Dim keepIdx = kept.ToArray
        Dim m = keepIdx.Length
        Dim out(nSamples - 1, m - 1) As Double

        For s As Integer = 0 To nSamples - 1
            For j As Integer = 0 To m - 1
                Dim v = genes(keepIdx(j)).experiments(s)
                If logNormalize Then
                    v = std.Log(1.0 + v)
                End If
                out(s, j) = v
            Next
        Next

        Return out
    End Function

    ''' <summary>
    ''' 返回保留下来的基因名（与 <see cref="ToSampleByGeneMatrix"/> 的列顺序一致）。
    ''' </summary>
    Public Function KeptGeneNames(matrix As Matrix, Optional minSamples As Integer = 1, Optional opts As Monocle3Options = Nothing) As String()
        Dim genes = matrix.expression
        Dim nGenes = genes.Length
        Dim nSamples = matrix.sampleID.Length
        Dim keepFlag(nGenes - 1) As Boolean
        Dim checkGene = Sub(g As Integer)
            Dim expr = genes(g).experiments
            Dim nonzero = 0
            For s As Integer = 0 To nSamples - 1
                If expr(s) > 0 Then
                    nonzero += 1
                    If nonzero >= minSamples Then Exit For
                End If
            Next
            keepFlag(g) = nonzero >= minSamples
        End Sub
        If opts Is Nothing OrElse opts.parallelEnabled Then
            Parallel.For(0, nGenes, checkGene)
        Else
            For g As Integer = 0 To nGenes - 1
                checkGene(g)
            Next
        End If

        Dim kept As New List(Of String)
        For g As Integer = 0 To nGenes - 1
            If keepFlag(g) Then kept.Add(genes(g).geneID)
        Next

        Return kept.ToArray
    End Function

    ''' <summary>
    ''' 将 [样本 × 基因] 矩阵还原为按样本组织的向量集合（Double()()）。
    ''' </summary>
    Public Function ToRowVectors(matrix As Double(,), Optional opts As Monocle3Options = Nothing) As Double()()
        Dim n = matrix.GetLength(0)
        Dim m = matrix.GetLength(1)
        Dim rows As Double()() = New Double(n - 1)() {}
        Dim buildRow = Sub(i As Integer)
            Dim row(m - 1) As Double
            For j As Integer = 0 To m - 1
                row(j) = matrix(i, j)
            Next
            rows(i) = row
        End Sub
        If opts Is Nothing OrElse opts.parallelEnabled Then
            Parallel.For(0, n, buildRow)
        Else
            For i As Integer = 0 To n - 1
                buildRow(i)
            Next
        End If
        Return rows
    End Function

    ''' <summary>
    ''' 按每基因（列）的表达方差筛选 top <paramref name="topN"/> 高变基因（highly variable genes）。
    ''' PCA 在全基因（数万维）上计算量爆炸，先降维到高变基因可使其保持高效，亦符合 Monocle3 标准流程。
    ''' </summary>
    Public Function SelectHighlyVariableGenes(matrix As Double(,),
                                              geneNames As String(),
                                              topN As Integer,
                                              Optional opts As Monocle3Options = Nothing) As (matrix As Double(,), names As String())
        Dim n = matrix.GetLength(0)
        Dim m = matrix.GetLength(1)
        Dim k = If(topN < m, topN, m)

        ' 每列（基因）方差（按列独立，可并行）
        Dim variance(m - 1) As Double
        Dim computeVar = Sub(j As Integer)
            Dim sum = 0.0, sumSq = 0.0
            For i As Integer = 0 To n - 1
                Dim v = matrix(i, j)
                sum += v
                sumSq += v * v
            Next
            Dim mean = sum / n
            variance(j) = sumSq / n - mean * mean
        End Sub
        If opts Is Nothing OrElse opts.parallelEnabled Then
            Parallel.For(0, m, computeVar)
        Else
            For j As Integer = 0 To m - 1
                computeVar(j)
            Next
        End If

        ' 按方差降序取前 k 个基因索引
        Dim order = Enumerable.Range(0, m).OrderByDescending(Function(j) variance(j)).Take(k).ToArray

        Dim out(n - 1, k - 1) As Double
        Dim names(k - 1) As String
        For j2 As Integer = 0 To k - 1
            Dim src = order(j2)
            For i As Integer = 0 To n - 1
                out(i, j2) = matrix(i, src)
            Next
            names(j2) = geneNames(src)
        Next

        Return (out, names)
    End Function
End Module

