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
End Module