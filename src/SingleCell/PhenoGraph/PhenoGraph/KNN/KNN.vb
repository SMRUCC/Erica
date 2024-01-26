Imports Microsoft.VisualBasic.Data.GraphTheory.KdTree.ApproximateNearNeighbor
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix

''' <summary>
''' KNN search handler for phenograph
''' </summary>
Friend Class KNN

    ReadOnly score As ScoreMetric
    ''' <summary>
    ''' knn score cutoff
    ''' </summary>
    ReadOnly cutoff As Double

    Sub New(metric As ScoreMetric, knn_cutoff As Double)
        Me.score = metric
        Me.cutoff = knn_cutoff
    End Sub

    ''' <summary>
    ''' the output keeps the same order as the given input <paramref name="data"/>
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="k"></param>
    ''' <returns></returns>
    Public Iterator Function FindNeighbors(data As GeneralMatrix, Optional k As Integer = 30) As IEnumerable(Of (size As Integer, indices As Integer(), weights As Double()))
        Dim matrix = data.PopulateVectors.ToArray
        Dim knnQuery = matrix _
            .AsParallel _
            .Select(Function(v) FindNeighbors(v, matrix, k)) _
            .ToArray

        For Each nn2 In knnQuery
            Dim index As Integer() = nn2.Select(Function(xi) xi.Item1.index).ToArray
            Dim weights As Double() = nn2.Select(Function(xi) xi.w).ToArray

            Yield (index.Length, index, weights)
        Next
    End Function

    Private Function FindNeighbors(v As TagVector, matrix As TagVector(), k As Integer) As (TagVector, w As Double)()
        Dim vec As Double() = v.vector.ToArray

        Return matrix _
            .Select(Function(i)
                        Dim w As Double = score.eval(vec, i.vector)
                        Return (i, w)
                    End Function) _
            .Where(Function(a) a.w > cutoff) _
            .OrderByDescending(Function(a) a.w) _
            .Take(k) _
            .ToArray
    End Function
End Class
