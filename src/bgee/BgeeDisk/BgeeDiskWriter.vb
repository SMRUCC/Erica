Imports Microsoft.VisualBasic.DataStorage.HDSPack.FileSystem

Public Class BgeeDiskWriter : Implements IDisposable

    ReadOnly disk As StreamPack
    ReadOnly geneNames As New Dictionary(Of String, (Integer, String))
    ReadOnly anatomicalName As New Dictionary(Of String, (Integer, String))
    ReadOnly developmental_stage As New Dictionary(Of String, (Integer, String))
    ReadOnly calls As New List(Of BgeeVector)

    Private disposedValue As Boolean

    Sub New(file As String)
        disk = StreamPack.CreateNewStream(file, init_size:=4096 * 1024)
    End Sub

    Public Sub Push(expr As AdvancedCalls)
        Dim geneID As Integer
        Dim anatomicalID As Integer
        Dim stageID As Integer
        Dim quality As Quality = If(
            expr.call_quality = "gold quality",
            Quality.gold_quality,
            Quality.silver_quality
        )

        If Not geneNames.ContainsKey(expr.geneID) Then
            geneID = geneNames.Count + 1
            geneNames.Add(expr.geneID, (geneID, expr.gene_name))
        Else
            geneID = geneNames(expr.geneID).Item1
        End If
        If Not anatomicalName.ContainsKey(expr.anatomicalID) Then
            anatomicalID = anatomicalName.Count + 1
            anatomicalName.Add(expr.anatomicalID, (anatomicalID, expr.anatomicalName))
        Else
            anatomicalID = anatomicalName(expr.anatomicalID).Item1
        End If
        If Not developmental_stage.ContainsKey(expr.developmental_stageID) Then
            stageID = developmental_stage.Count + 1
            developmental_stage.Add(expr.developmental_stageID, (stageID, expr.developmental_stage))
        Else
            stageID = developmental_stage(expr.developmental_stageID).Item1
        End If

        Call calls.Add(New BgeeVector With {
            .anatomicalID = anatomicalID,
            .geneID = geneID,
            .developmentalID = stageID,
            .quality = quality
        })
    End Sub

    Public Sub Push(calls As IEnumerable(Of AdvancedCalls))
        For Each [call] As AdvancedCalls In calls
            Call Push([call])
        Next
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
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
