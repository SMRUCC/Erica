Imports System.Drawing
Imports System.IO
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Class STRaid

    ' spatial data
    Public Property matrix As Matrix
    ''' <summary>
    ''' the spatial index of each row in <see cref="matrix"/>
    ''' </summary>
    ''' <returns></returns>
    Public Property spots As Point()

    Public Shared Function Write(raid As STRaid, file As Stream) As Boolean

    End Function

    Public Shared Function Load(file As Stream) As STRaid

    End Function

End Class
