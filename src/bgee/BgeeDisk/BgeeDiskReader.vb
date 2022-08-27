Imports System.IO
Imports Microsoft.VisualBasic.Data.IO
Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem
Imports Microsoft.VisualBasic.Text

Public Class BgeeDiskReader

    ReadOnly anatomical_calls As Dictionary(Of Integer, BgeeVector())
    ReadOnly developmental_calls As Dictionary(Of Integer, BgeeVector())
    ReadOnly geneIDs As IDFactorEnums
    ReadOnly anatomicals As IDFactorEnums
    ReadOnly developmental_stage As IDFactorEnums

    Sub New(file As String)
        Dim disk As New StreamPack(file, [readonly]:=True)
        Dim vectors As BgeeVector() = readCallsVector(disk.OpenBlock(BgeeDiskWriter.callsVector)).ToArray

        geneIDs = readEnums(disk.OpenBlock(BgeeDiskWriter.geneIDFactorFile))
        anatomicals = readEnums(disk.OpenBlock(BgeeDiskWriter.anatomicalIDFactorFile))
        developmental_stage = readEnums(disk.OpenBlock(BgeeDiskWriter.developmentalIDFactorFile))

        anatomical_calls = vectors _
            .GroupBy(Function(v) v.anatomicalID) _
            .ToDictionary(Function(a) a.Key,
                          Function(a)
                              Return a.ToArray
                          End Function)
        developmental_calls = vectors _
            .GroupBy(Function(v) v.developmentalID) _
            .ToDictionary(Function(a) a.Key,
                          Function(a)
                              Return a.ToArray
                          End Function)
    End Sub

    Public Function Anatomical(model As String, geneSet As String()) As IEnumerable(Of String)
        Dim cluster_id As Integer = anatomicals(model)
        Dim calls = anatomical_calls(key:=cluster_id)
        Dim list As IEnumerable(Of String) = calls.Select(Function(v) geneIDs.ToString(v.geneID))

        Return geneSet.Intersect(list)
    End Function

    Public Function Developmental(model As String, geneSet As String()) As IEnumerable(Of String)
        Dim cluster_id As Integer = developmental_stage(model)
        Dim calls = developmental_calls(key:=cluster_id)
        Dim list As IEnumerable(Of String) = calls.Select(Function(v) geneIDs.ToString(v.geneID))

        Return geneSet.Intersect(list)
    End Function

    Private Iterator Function readCallsVector(buf As Stream) As IEnumerable(Of BgeeVector)
        Dim bin As New BinaryDataReader(buf) With {.ByteOrder = ByteOrder.BigEndian}

        Do While Not bin.EndOfStream
            Yield New BgeeVector With {
                .geneID = bin.ReadInt32,
                .anatomicalID = bin.ReadInt32,
                .developmentalID = bin.ReadInt32,
                .quality = bin.ReadByte,
                .expression = bin.ReadBoolean,
                .expression_rank = bin.ReadDouble
            }
        Loop
    End Function

    Private Function readEnums(buf As Stream) As IDFactorEnums
        Dim bin As New BinaryDataReader(buf, Encodings.ASCII) With {.ByteOrder = ByteOrder.BigEndian}
        Dim enums As New Dictionary(Of String, (index As Integer, name As String))

        Do While Not bin.EndOfStream
            Dim index As Integer = bin.ReadInt32
            Dim id As String = bin.ReadString(BinaryStringFormat.ZeroTerminated)
            Dim name As String = bin.ReadString(BinaryStringFormat.ZeroTerminated)

            enums.Add(id, (id, name))
        Loop

        Return New IDFactorEnums(enums)
    End Function
End Class
