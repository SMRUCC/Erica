Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix

''' <summary>
''' X contains the expression matrix.
''' </summary>
Public Class X

    Public Property matrix As SparseMatrix

    ''' <summary>
    ''' 构架出一个稀疏矩阵
    ''' </summary>
    ''' <param name="xdata"></param>
    ''' <param name="xindices"></param>
    ''' <param name="xindptr"></param>
    ''' <returns></returns>
    Friend Shared Function ShapeMatrix(xdata As Single(), xindices As Integer(), xindptr As Integer()) As X
        Return New X With {.matrix = SparseMatrix.UnpackData(xdata, xindices, xindptr)}
    End Function
End Class
