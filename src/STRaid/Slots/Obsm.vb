Imports System.Drawing

Namespace H5ad_data

    ''' <summary>
    ''' obsm contains the embeddings data.
    ''' </summary>
    Public Class Obsm

        Public Property X_pca As Single()()
        Public Property X_umap As PointF()
        Public Property spatial As PointF()

    End Class
End Namespace