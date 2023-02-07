Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.ApplicationServices
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

    ''' <summary>
    ''' join the <see cref="spots"/> [x,y] data to the gene expression <see cref="matrix"/>.
    ''' </summary>
    ''' <returns></returns>
    Public Function GetSpatialMatrix() As Matrix
        Dim stMatrix As New Matrix With {
            .sampleID = matrix.sampleID,
            .tag = matrix.tag,
            .expression = matrix.expression _
                .Select(Function(r, i)
                            Dim xy As Point = _spots(i)
                            Dim spot As New DataFrameRow With {
                                .geneID = $"{xy.X},{xy.Y}",
                                .experiments = r.experiments
                            }

                            Return spot
                        End Function) _
                .ToArray
        }

        Return stMatrix
    End Function

    Public Shared Function Write(raid As STRaid, file As Stream) As Boolean
        Dim pack As New StreamPack(file, meta_size:=8 * 1024 * 1024)
        Dim x As Integer() = raid.spots.Select(Function(p) p.X).ToArray
        Dim y As Integer() = raid.spots.Select(Function(p) p.Y).ToArray
        Dim barcodes As String() = raid.matrix.rownames
        Dim geneids As String() = raid.matrix.sampleID


        Call DirectCast(pack, IFileSystemEnvironment).Flush()
        Return True
    End Function

    Public Shared Function Load(file As Stream) As STRaid

    End Function

End Class
