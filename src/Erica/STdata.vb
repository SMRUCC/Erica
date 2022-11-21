Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports STImaging
Imports STRaid

<Package("STdata")>
Public Module STdata

    <ExportAPI("read.ST_spacerangerH5Matrix")>
    Public Function ReadST_spacerangerH5Matrix(h5ad As String) As Matrix
        Return LoadDisk.ReadST_spacerangerH5Matrix(h5ad)
    End Function

    <ExportAPI("read.spatial_spots")>
    Public Function ReadSpatialSpots(file As String) As SpaceSpot()
        Return ST_spaceranger _
            .LoadTissueSpots(file.SolveStream.LineTokens) _
            .ToArray
    End Function

    <ExportAPI("as.STRaid")>
    Public Function CombineSTRaid(h5Matrix As Matrix, spots As SpaceSpot()) As STRaid.STRaid
        Dim spotIndex As New Dictionary(Of String, Point)

        For i As Integer = 0 To spots.Length - 1
            Call spotIndex.Add(spots(i).barcode, spots(i).GetPoint)
        Next

        Return New STRaid.STRaid With {
            .matrix = h5Matrix,
            .spots = h5Matrix.expression _
                .Select(Function(r) spotIndex(r.geneID)) _
                .ToArray
        }
    End Function

    <ExportAPI("as.STmatrix")>
    Public Function CreateSpatialMatrix(h5Matrix As Matrix, spots As SpaceSpot()) As Matrix
        Return CombineSTRaid(h5Matrix, spots).GetSpatialMatrix
    End Function

    <ExportAPI("write.straid")>
    Public Function WriteMatrix(straid As STRaid.STRaid, file As String) As Object
        Using buffer As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)
            Return Global.STRaid.STRaid.Write(straid, file:=buffer)
        End Using
    End Function
End Module