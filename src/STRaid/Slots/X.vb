''' <summary>
''' X contains the expression matrix.
''' </summary>
Public Class X

    ''' <summary>
    ''' 构架出一个稀疏矩阵
    ''' </summary>
    ''' <param name="xdata"></param>
    ''' <param name="xindices"></param>
    ''' <param name="xindptr"></param>
    ''' <returns></returns>
    Friend Shared Function ShapeMatrix(xdata As Single(), xindices As Integer(), xindptr As Integer()) As X
        Dim left As Integer = Scan0

        For Each idx As Integer In xindptr.Skip(1)

        Next
    End Function
End Class
