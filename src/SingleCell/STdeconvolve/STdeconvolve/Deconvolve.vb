Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Class Deconvolve

    ''' <summary>
    ''' each pixel is defined as a mixture of 𝐾 cell types 
    ''' represented As a multinomial distribution Of cell-type 
    ''' probabilities
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' single cell class definition at here
    ''' </remarks>
    Public Property theta As Matrix

    ''' <summary>
    ''' each cell-type Is defined as a probability distribution 
    ''' over the genes (𝛽) present in the ST dataset.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' apply of this distribution for generates the deconv expression matrix
    ''' </remarks>
    Public Property topicMap As Dictionary(Of String, Double)()

    ''' <summary>
    ''' multiply the <paramref name="raw"/> <see cref="Matrix"/> with the 
    ''' <see cref="topicMap"/> percentage distribution.
    ''' </summary>
    ''' <param name="raw"></param>
    ''' <returns></returns>
    Public Function GetSingleCellExpressionMatrix(raw As Matrix) As Matrix
        Dim expressions As New List(Of DataFrameRow)
        Dim newIds As New List(Of String)
        Dim i As i32 = 1
        Dim rawIds As Index(Of String) = raw.sampleID.Indexing
        Dim dist As New List(Of (p As Vector, subset As String()))

        For Each topic In topicMap
            Dim idprefix As String = $"topic_{++i}"
            Dim newIdset = topic.Keys _
                .Select(Function(g) $"{idprefix}.{g}") _
                .ToArray

            Call dist.Add((topic.Values.AsVector, topic.Keys.ToArray))
            Call newIds.AddRange(newIdset)
        Next

        For Each spot As DataFrameRow In raw.expression
            Dim v As New List(Of Double)

            For Each topic In dist
                Dim gi As Integer() = rawIds(topic.subset)
                Dim vi As Vector = spot(gi)

                Call v.AddRange(vi * topic.p)
            Next

            Call expressions.Add(New DataFrameRow With {
                .geneID = spot.geneID,
                .experiments = v.ToArray
            })
        Next

        Return New Matrix With {
            .expression = expressions.ToArray,
            .sampleID = newIds.ToArray,
            .tag = $"GetSingleCellExpressionMatrix({raw.tag})"
        }
    End Function

End Class
