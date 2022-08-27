Imports System.IO
Imports Microsoft.VisualBasic.Data.IO
Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem
Imports Microsoft.VisualBasic.Text

Public Class BgeeDiskWriter : Implements IDisposable

    ReadOnly disk As StreamPack
    ReadOnly geneNames As New IDFactorEnums
    ReadOnly anatomicalName As New IDFactorEnums
    ReadOnly developmental_stage As New IDFactorEnums
    ReadOnly calls As New List(Of BgeeVector)

    Private disposedValue As Boolean

    Sub New(file As String)
        disk = StreamPack.CreateNewStream(file, init_size:=4096 * 1024)
    End Sub

    Public Sub Push(expr As AdvancedCalls)
        Dim geneID As Integer = geneNames.EnumValue(expr.geneID, expr.gene_name)
        Dim anatomicalID As Integer = anatomicalName.EnumValue(expr.anatomicalID, expr.anatomicalName)
        Dim stageID As Integer = developmental_stage.EnumValue(expr.developmental_stageID, expr.developmental_stage)
        Dim quality As Quality = If(
            expr.call_quality = "gold quality",
            Quality.gold_quality,
            Quality.silver_quality
        )

        Call calls.Add(New BgeeVector With {
            .anatomicalID = anatomicalID,
            .geneID = geneID,
            .developmentalID = stageID,
            .quality = quality,
            .expression = expr.expression = "present",
            .expression_rank = expr.expression_rank
        })
    End Sub

    Public Sub Push(calls As IEnumerable(Of AdvancedCalls))
        For Each [call] As AdvancedCalls In calls
            Call Push([call])
        Next
    End Sub

    Private Sub writeFactorIndex(buf As Stream, index As IDFactorEnums)
        Dim bin As New BinaryDataWriter(buf, Encodings.ASCII) With {.ByteOrder = ByteOrder.BigEndian}

        For Each id In index.enums
            Call bin.Write(id.Value.index)
            Call bin.Write(id.Key, BinaryStringFormat.ZeroTerminated)
            Call bin.Write(id.Value.name, BinaryStringFormat.ZeroTerminated)
        Next
    End Sub

    Private Sub writeVectorData(buf As Stream)
        Dim bin As New BinaryDataWriter(buf) With {.ByteOrder = ByteOrder.BigEndian}

        For Each v As BgeeVector In calls
            Call bin.Write(v.geneID)
            Call bin.Write(v.anatomicalID)
            Call bin.Write(v.developmentalID)
            Call bin.Write(v.quality)
            Call bin.Write(v.expression)
            Call bin.Write(v.expression_rank)
        Next
    End Sub

    Private Sub writeDisk()
        Using buf As Stream = disk.OpenBlock("/factors/geneIDs.fac")
            Call writeFactorIndex(buf, geneNames)
        End Using
        Using buf As Stream = disk.OpenBlock("/factors/anatomicalIDs.fac")
            Call writeFactorIndex(buf, anatomicalName)
        End Using
        Using buf As Stream = disk.OpenBlock("/factors/developmental_stage.fac")
            Call writeFactorIndex(buf, developmental_stage)
        End Using
        Using buf As Stream = disk.OpenBlock("/bgee.vec")
            Call writeVectorData(buf)
        End Using
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
                Call writeDisk()
                Call disk.Dispose()
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    ' ' TODO: override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
    ' Protected Overrides Sub Finalize()
    '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
