Imports SMRUCC.genomics.Analysis.HTS.DataFrame

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
    Public Function GetSingleCellExpressionMatrix(raw As Matrix) As Matrix

    End Function

End Class
