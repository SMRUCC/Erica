Imports System.Drawing

Public Class SingleExpression

    ''' <summary>
    ''' usually be the umap embedding, [x,y,z]
    ''' </summary>
    ''' <returns></returns>
    Public Property embedding As Double()
    Public Property label As String
    Public Property cluster As String
    Public Property expression As Double

    Public Function Get2dEmbedding() As PointF
        Return New PointF(_embedding(0), _embedding(1))
    End Function

End Class
