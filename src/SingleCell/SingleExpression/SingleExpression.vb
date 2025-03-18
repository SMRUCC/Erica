Imports System.Drawing

Public Class SingleExpression : Implements IEmbeddingScatter

    ''' <summary>
    ''' usually be the umap embedding, [x,y,z]
    ''' </summary>
    ''' <returns></returns>
    Public Property embedding As Double()
    Public Property label As String Implements IEmbeddingScatter.label
    Public Property cluster As String Implements IEmbeddingScatter.cluster
    Public Property expression As Double

    Private ReadOnly Property x As Double Implements IEmbeddingScatter.x
        Get
            Return _embedding(0)
        End Get
    End Property

    Private ReadOnly Property y As Double Implements IEmbeddingScatter.y
        Get
            Return _embedding(1)
        End Get
    End Property

    Private ReadOnly Property z As Double Implements IEmbeddingScatter.z
        Get
            Return _embedding(2)
        End Get
    End Property

    Public Function Get2dEmbedding() As PointF
        Return New PointF(_embedding(0), _embedding(1))
    End Function

End Class

Public Interface IEmbeddingScatter

    ReadOnly Property label As String
    ReadOnly Property cluster As String

    ReadOnly Property x As Double
    ReadOnly Property y As Double
    ReadOnly Property z As Double

End Interface