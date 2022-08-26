Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Module Export

    <Extension>
    Public Function ExportExpression(raw As AnnData) As Matrix
        Dim m As New List(Of DataFrameRow)
        Dim spatial = raw.obsm.spatial _
            .Select(Function(i) $"{i.X},{i.Y}") _
            .ToArray
        Dim cell As i32 = Scan0

        For Each row As Vector In raw.X.matrix.RowVectors
            m.Add(New DataFrameRow With {.geneID = spatial(++cell), .experiments = row.ToArray})
        Next

        Return New Matrix With {
            .expression = m.ToArray,
            .sampleID = raw.var.gene_ids,
            .tag = raw.source
        }
    End Function

End Module
