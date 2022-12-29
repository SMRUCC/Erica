Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap

''' <summary>
''' parse the spot table file
''' </summary>
Public Module ST_spaceranger

    <Extension>
    Public Iterator Function GetPixels(spots As IEnumerable(Of SpaceSpot)) As IEnumerable(Of PixelData)
        For Each spot As SpaceSpot In spots
            Yield New PixelData(spot.px, spot.py, spot.flag)
        Next
    End Function

    <Extension>
    Public Iterator Function LoadTissueSpots(data As IEnumerable(Of String)) As IEnumerable(Of SpaceSpot)
        For Each line As String In data
            Dim t As String() = line.Split(","c)
            Dim spot As New SpaceSpot With {
                .barcode = t(0),
                .flag = Integer.Parse(t(1)),
                .px = Integer.Parse(t(2)),
                .py = Integer.Parse(t(3)),
                .x = Integer.Parse(t(4)),
                .y = Integer.Parse(t(5))
            }

            Yield spot
        Next
    End Function

End Module
