''' <summary>
''' obs contains the cell metadata.
''' </summary>
Public Class Obs

    ''' <summary>
    ''' cell cluster index, use this index value to read 
    ''' the name label data in <see cref="class_labels"/>
    ''' .
    ''' </summary>
    ''' <returns></returns>
    Public Property clusters As Integer()
    Public Property class_labels As String()

End Class
