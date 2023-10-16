Imports Microsoft.VisualBasic.Emit.Marshal
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.My
Imports Microsoft.VisualBasic.My.FrameworkInternal

Namespace H5ad_data

    ''' <summary>
    ''' X contains the expression matrix.
    ''' </summary>
    Public Class X

        Public Property matrix As GeneralMatrix

        Public Overrides Function ToString() As String
            Return matrix.ToString
        End Function

        ''' <summary>
        ''' 构架出一个稀疏矩阵
        ''' </summary>
        ''' <param name="xdata"></param>
        ''' <param name="xindices"></param>
        ''' <param name="xindptr"></param>
        ''' <returns></returns>
        Friend Shared Function ShapeMatrix(xdata As Single(),
                                           xindices As Integer(),
                                           xindptr As Integer(),
                                           geneIdsize As Integer) As X

            Dim memConfig = FrameworkInternal.ConfigMemory
            Dim expr As GeneralMatrix

            ' 20220830
            ' the index of the element is ZERO-based
            ' no needs to minus 1
            '
            ' data has been checked!
            ' xindices = xindices _
            '    .Select(Function(i) i - 1) _
            '    .ToArray

            If memConfig = MemoryLoads.Light Then
                ' sparse matrix
                expr = SparseMatrix.UnpackData(xdata, xindices, xindptr, maxColumns:=geneIdsize)
            Else
                ' raw matrix
                ' in max memory
                expr = ExtractRawMatrix(xdata, xindices, xindptr, geneIdsize)
            End If

            Return New X With {
                .matrix = expr
            }
        End Function

        Private Shared Function ExtractRawMatrix(xdata As Single(),
                                                 xindices As Integer(),
                                                 xindptr As Integer(),
                                                 geneIdsize As Integer) As NumericMatrix
            Dim mat As New List(Of Double())
            Dim left As Integer = xindptr(Scan0)

            For Each idx As Integer In xindptr.Skip(1)
                Dim blocksize = idx - left
                Dim subsetData As Single() = New Single(blocksize - 1) {}
                Dim subsetIndex As Integer() = New Integer(blocksize - 1) {}
                Dim row As Double() = New Double(geneIdsize - 1) {}

                Call Array.ConstrainedCopy(xdata, left, subsetData, Scan0, blocksize)
                Call Array.ConstrainedCopy(xindices, left, subsetIndex, Scan0, blocksize)

                For j As Integer = 0 To blocksize - 1
                    row(subsetIndex(j)) = subsetData(j)
                Next

                left = idx
                mat.Add(row)
            Next

            Return New NumericMatrix(mat)
        End Function
    End Class
End Namespace