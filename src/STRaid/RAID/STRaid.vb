Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.Data.IO
Imports Microsoft.VisualBasic.DataStorage.HDSPack
Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

''' <summary>
''' Data structure for unify the data storage of the spatial data
''' </summary>
Public Class STRaid

    ''' <summary>
    ''' the spatial expression data, gene features in columns and spatial spots in rows
    ''' </summary>
    ''' <returns></returns>
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

    Const meta_size_MB As Long = 16 * 1024 * 1024

    ''' <summary>
    ''' Write the spatial matrix data to file
    ''' </summary>
    ''' <param name="raid"></param>
    ''' <param name="file"></param>
    ''' <returns></returns>
    Public Shared Function Write(raid As STRaid, file As Stream) As Boolean
        Dim pack As New StreamPack(file, meta_size:=meta_size_MB)
        Dim x As Integer() = raid.spots.Select(Function(p) p.X).ToArray
        Dim y As Integer() = raid.spots.Select(Function(p) p.Y).ToArray
        Dim barcodes As String() = raid.matrix.rownames
        Dim geneids As String() = raid.matrix.sampleID

        Using data As Stream = pack.OpenFile("/spatial/x", FileMode.OpenOrCreate, FileAccess.Write)
            Call New BinaryDataWriter(data) With {.ByteOrder = ByteOrder.BigEndian}.Write(x)
        End Using
        Using data As Stream = pack.OpenFile("/spatial/y", FileMode.OpenOrCreate, FileAccess.Write)
            Call New BinaryDataWriter(data) With {.ByteOrder = ByteOrder.BigEndian}.Write(y)
        End Using
        Using data As Stream = pack.OpenFile("/spatial/barcodes.txt", FileMode.OpenOrCreate, FileAccess.Write)
            Call New BinaryDataWriter(data).Write(barcodes.JoinBy(vbCrLf))
        End Using
        Using data As Stream = pack.OpenFile("/expression/features.txt", FileMode.OpenOrCreate, FileAccess.Write)
            Call New BinaryDataWriter(data).Write(geneids.JoinBy(vbCrLf))
        End Using

        For Each row In raid.matrix.expression
            Using data As Stream = pack.OpenFile($"/expression/matrix/{row.geneID}.vec")
                Call New BinaryDataWriter(data) With {.ByteOrder = ByteOrder.BigEndian}.Write(row.experiments)
            End Using
        Next

        Call pack.WriteText(barcodes.Length, "/expression/dim_h")
        Call pack.WriteText(geneids.Length, "/expression/dim_w")

        Call DirectCast(pack, IFileSystemEnvironment).Flush()

        Return True
    End Function

    Public Shared Function Load(file As Stream) As STRaid
        Dim pack As New StreamPack(file, meta_size:=meta_size_MB, [readonly]:=True)
        Dim barcode_size As Integer = pack.ReadText("/expression/dim_h").TrimNewLine.Trim.DoCall(AddressOf Integer.Parse)
        Dim geneSet_size As Integer = pack.ReadText("/expression/dim_w").TrimNewLine.Trim.DoCall(AddressOf Integer.Parse)
        Dim x As Integer() = New BinaryDataReader(pack.OpenFile("/spatial/x"), byteOrder:=ByteOrder.BigEndian).ReadInt32s(barcode_size)
        Dim y As Integer() = New BinaryDataReader(pack.OpenFile("/spatial/y"), byteOrder:=ByteOrder.BigEndian).ReadInt32s(geneSet_size)
        Dim barcodes As String() = pack.ReadText("/spatial/barcodes.txt").Trim.LineTokens
        Dim geneids As String() = pack.ReadText("/expression/features.txt").Trim.LineTokens
        Dim expressions As New List(Of DataFrameRow)
        Dim spatial As Point() = x.Select(Function(xi, i) New Point(xi, y(i))).ToArray

        For Each id As String In barcodes
            Dim path As String = $"/expression/matrix/{id}.vec"
            Dim v As Double() = New BinaryDataReader(pack.OpenFile(path), byteOrder:=ByteOrder.BigEndian).ReadDoubles(geneSet_size)
            Dim spot As New DataFrameRow With {
                .geneID = id,
                .experiments = v
            }

            Call expressions.Add(spot)
        Next

        Return New STRaid With {
            .spots = spatial,
            .matrix = New Matrix With {
                .sampleID = geneids,
                .expression = expressions.ToArray
            }
        }
    End Function

End Class
