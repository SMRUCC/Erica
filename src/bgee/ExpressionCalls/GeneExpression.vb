
Public Class GeneExpression

    Public Property data As String
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_high_quality As Integer
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_low_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_high_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_low_quality As Integer
    Public Property observed_data As String

End Class