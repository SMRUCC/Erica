Public Class AnnData

    ''' <summary>
    ''' X contains the expression matrix.
    ''' </summary>
    ''' <returns></returns>
    Public Property X As X
    ''' <summary>
    ''' obsm contains the embeddings data.
    ''' </summary>
    ''' <returns></returns>
    Public Property obsm As Obsm
    ''' <summary>
    ''' obs contains the cell metadata.
    ''' </summary>
    ''' <returns></returns>
    Public Property obs As Obs
    ''' <summary>
    ''' var contains the gene metadata.
    ''' </summary>
    ''' <returns></returns>
    Public Property var As Var

    Public Property uns As Uns

    Public Property source As String

End Class
