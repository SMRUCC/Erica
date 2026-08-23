Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports std = System.Math


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
                                         Optional logNormalize As Boolean = True) As Double(,)
        Dim genes = matrix.expression
        Dim nGenes = genes.Length
        Dim nSamples = matrix.sampleID.Length

        ' 低表达过滤：统计每个基因在多少样本中非零
        Dim kept As New List(Of Integer)
        For g As Integer = 0 To nGenes - 1
            Dim expr = genes(g).experiments
            Dim nonzero = 0
            For s As Integer = 0 To nSamples - 1
                If expr(s) > 0 Then
                    nonzero += 1
                    If nonzero >= minSamples Then Exit For
                End If
            Next
            If nonzero >= minSamples Then
                kept.Add(g)
            End If
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
    Public Function KeptGeneNames(matrix As Matrix, Optional minSamples As Integer = 1) As String()
        Dim genes = matrix.expression
        Dim nGenes = genes.Length
        Dim nSamples = matrix.sampleID.Length
        Dim kept As New List(Of String)

        For g As Integer = 0 To nGenes - 1
            Dim expr = genes(g).experiments
            Dim nonzero = 0
            For s As Integer = 0 To nSamples - 1
                If expr(s) > 0 Then
                    nonzero += 1
                    If nonzero >= minSamples Then Exit For
                End If
            Next
            If nonzero >= minSamples Then
                kept.Add(genes(g).geneID)
            End If
        Next

        Return kept.ToArray
    End Function

    ''' <summary>
    ''' 将 [样本 × 基因] 矩阵还原为按样本组织的向量集合（Double()()）。
    ''' </summary>
    Public Function ToRowVectors(matrix As Double(,)) As Double()()
        Dim n = matrix.GetLength(0)
        Dim m = matrix.GetLength(1)
        Dim rows As Double()() = New Double(n - 1)() {}
        For i As Integer = 0 To n - 1
            Dim row(m - 1) As Double
            For j As Integer = 0 To m - 1
                row(j) = matrix(i, j)
            Next
            rows(i) = row
        Next
        Return rows
    End Function
End Module

