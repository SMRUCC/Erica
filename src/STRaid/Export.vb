Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Module Export

    <Extension>
    Public Function ExportExpression(raw As AnnData) As Matrix
        Dim spatial = raw.obsm.spatial _
            .Select(Function(i) $"{i.X},{i.Y}") _
            .ToArray
        Dim mat As Matrix = raw.X.ExportExpression(
            spotId:=spatial,
            geneID:=raw.var.gene_ids,
            source:=raw.source
        )

        Return mat
    End Function

    <Extension>
    Friend Function ExportExpression(X As X,
                                     spotId As String(),
                                     geneID As String(),
                                     source As String) As Matrix

        Dim m As New List(Of DataFrameRow)
        Dim cell As i32 = Scan0

        For Each row As Vector In X.matrix.RowVectors
            Call m.Add(New DataFrameRow With {.geneID = spotId(++cell), .experiments = row.ToArray})
        Next

        Return New Matrix With {
            .expression = m.ToArray,
            .sampleID = geneID,
            .tag = source
        }
    End Function

    <Extension>
    Public Function ExpressionList(raw As AnnData, Optional q As Double = 0.2) As Dictionary(Of String, String())
        Dim mat = raw.ExportExpression
        Dim clusters = raw.obs.class_labels

        clusters = raw.obs.clusters _
            .Select(Function(i) clusters(i)) _
            .ToArray

        For i As Integer = 0 To clusters.Length - 1
            mat(i).geneID = clusters(i)
        Next

        Dim spatials = mat.expression _
            .GroupBy(Function(i) i.geneID) _
            .ToDictionary(Function(a) a.Key,
                          Function(a)
                              Return expressionList(a.ToArray, mat.sampleID, q)
                          End Function)

        Return spatials
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="spots"></param>
    ''' <param name="geneIDs"></param>
    ''' <param name="q">
    ''' 具有表达值的斑点数量少于 20% 则认为基因不表达，处于缺失值状态
    ''' </param>
    ''' <returns></returns>
    Private Function expressionList(spots As DataFrameRow(), geneIDs As String(), q As Double) As String()
        Dim list As New List(Of String)

        For i As Integer = 0 To geneIDs.Length - 1
            Dim v = spots.Select(Function(s) s.experiments(i)).AsVector
            Dim non_zero = v(v > 0.0).Length
            Dim p As Double = non_zero / v.Length

            If p > q Then
                Call list.Add(geneIDs(i))
            End If
        Next

        Return list.ToArray
    End Function

End Module
