Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

''' <summary>
''' The spatial deconvolution result
''' </summary>
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
    ''' <remarks>
    ''' the single cell data will extends the gene features set with 
    ''' mutlipel cell layer id prefix.
    ''' </remarks>
    Public Function GetSingleCellExpressionMatrix(raw As Matrix, Optional prefix As String = "topic") As Matrix
        Dim expressions As New List(Of DataFrameRow)
        Dim newIds As New List(Of String)
        Dim i As i32 = 1
        Dim rawIds As Index(Of String) = raw.sampleID.Indexing
        Dim dist As New List(Of (p As Vector, subset As String()))

        For Each topic In topicMap
            Dim idprefix As String = $"{prefix}_{++i}"
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

    ''' <summary>
    ''' Only keeps the gene expression in the top cell class
    ''' </summary>
    ''' <param name="raw"></param>
    ''' <returns></returns>
    Public Function GetExpressionMatrix(raw As Matrix) As Matrix
        Dim expressions As New List(Of NamedValue(Of Dictionary(Of String, Double)))
        Dim rawIds As Index(Of String) = raw.sampleID.Indexing
        Dim dist As New List(Of (p As Vector, subset As String()))
        Dim layers As Dictionary(Of String, DataFrameRow) = theta.expression.ToDictionary(Function(g) g.geneID)

        For Each topic In topicMap
            Call dist.Add((topic.Values.AsVector, topic.Keys.ToArray))
        Next

        For Each spot As DataFrameRow In raw.expression
            Dim celltype As DataFrameRow = layers(spot.geneID)
            Dim i As Integer = which.Max(celltype.experiments)
            Dim topic = dist(i)
            Dim gi As Integer() = rawIds(topic.subset)
            Dim vi As Vector = spot(gi) * topic.p
            Dim exp As New Dictionary(Of String, Double)

            For i = 0 To vi.Dim - 1
                Call exp.Add(topic.subset(i), vi(i))
            Next

            Call expressions.Add(New NamedValue(Of Dictionary(Of String, Double)) With {
                .Name = spot.geneID,
                .Value = exp
            })
        Next

        Dim allFeatures As String() = expressions _
            .Select(Function(si) si.Value.Keys) _
            .IteratesALL _
            .Distinct _
            .ToArray
        Dim mat As New List(Of DataFrameRow)

        For Each expr In expressions
            Call mat.Add(New DataFrameRow With {
                .geneID = expr.Name,
                .experiments = allFeatures _
                    .Select(Function(xi) expr.Value.TryGetValue(xi, [default]:=0.0)) _
                    .ToArray
            })
        Next

        Return New Matrix With {
            .sampleID = allFeatures,
            .expression = mat.ToArray,
            .tag = $"GetExpressionMatrix({raw.tag})"
        }
    End Function

End Class
