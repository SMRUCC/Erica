Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

''' <summary>
''' SVG identification (Moran's I)
''' 
''' spatial variable genes, svg
''' </summary>
Public Module MoransI

    Public Function I(X As Matrix, Optional W As GeneralMatrix = Nothing) As Double()
        ' N is the total number of spot samples
        Dim N As Integer = X.sampleID.Length
        Dim dims As New Polygon2D(X.sampleID.Select(Function(si) si.Split(","c).Select(Function(ti) Integer.Parse(ti)).ToArray).Select(Function(ints) New PointF(ints(0), ints(1))).ToArray)

        If W Is Nothing Then
            W = NumericMatrix.One(dims.width, dims.height)
        End If

        ' W_sum is the sum of matrix W
        Dim W_sum As Double = Aggregate r In W.RowVectors Into Sum(r.Sum)
        ' Xu is the mean expression of the gene
        Dim Xu As Double = Aggregate r In X.expression Into Average(r.experiments.Average)
        Dim Ia As New Vector(ia_jobParallel(X, W, Xu))
        Dim Ib As New Vector(From gene As DataFrameRow In X.expression Select ((gene - Xu) ^ 2).Sum)
        Dim moransI = (N / W_sum) * (Ia / Ib)

        Return moransI
    End Function

    Private Function ia_jobParallel(X As Matrix, W As GeneralMatrix, Xu As Double) As IEnumerable(Of Double)
        Dim jobs = From gene_i As SeqValue(Of DataFrameRow)
                   In X.expression.SeqIterator.AsParallel
                   Let i As Integer = gene_i
                   Select i, Ij = geneI_a(gene_i, i, X, W, Xu)
                   Order By i

        Return jobs.Select(Function(j) j.Ij)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Xi"></param>
    ''' <param name="xj"></param>
    ''' <param name="Wij">
    ''' Wij is spatial weight between spots i and j calculated using the 2D spatial coordinates of the spots
    ''' </param>
    ''' <param name="Xu"></param>
    ''' <returns></returns>
    Private Function geneI_a(Xi As DataFrameRow, i As Integer, X As Matrix, W As GeneralMatrix, Xu As Double) As Double
        Dim Isum As Double() = New Double(X.size - 1) {}

        For j As Integer = 0 To X.size - 1
            Dim Xj As DataFrameRow = X.expression(j)
            Dim Ij As Double() = W(i, j) * (Xi - Xu) * (Xj - Xu)

            Isum(j) = Ij.Sum
        Next

        Return Isum.Sum
    End Function
End Module
