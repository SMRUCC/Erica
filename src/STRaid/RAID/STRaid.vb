Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem
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
        Dim pack As New StreamPack(file, meta_size:=8 * 1024 * 1024)
        Dim x As Integer() = raid.spots.Select(Function(p) p.X).ToArray
        Dim y As Integer() = raid.spots.Select(Function(p) p.Y).ToArray
        Dim barcodes As String() = raid.matrix.rownames
        Dim geneids As String() = raid.matrix.sampleID



        Call pack.Flush()
        Return True
    End Function

    Public Shared Function Load(file As Stream) As STRaid

    End Function

End Class
