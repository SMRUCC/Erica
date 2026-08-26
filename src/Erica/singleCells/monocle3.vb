
Imports Erica.Analysis.SingleCell.Monocle3
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
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
                Optional num_HVgenes As Integer = 3000) As Monocle3Options

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
            .numHVGenes = num_HVgenes
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
                Call $"[warn] HV 基因 {hvgenes(g)} 不在原始矩阵中，已跳过".debug
                Continue For
            End If
            Dim row = geneRow(hvgenes(g))
            For j As Integer = 0 To nSamples - 1
                ' log1p：与 Monocle3 内部 exprData 尺度一致
                sampleByGene(j, g) = std.Log(1 + matrix.expression(row).experiments(j))
            Next
        Next
        Call $"  HV 基因表达矩阵: 样本={nSamples} x 基因={nHV} (log1p)".info

        Return sampleByGene
    End Function
End Module
