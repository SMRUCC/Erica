Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap

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

Public Class SpaceSpot

    Public Property barcode As String
    Public Property flag As Integer

#Region "spot xy"
    ''' <summary>
    ''' the spot x
    ''' </summary>
    ''' <returns></returns>
    Public Property px As Integer
    ''' <summary>
    ''' the spot y
    ''' </summary>
    ''' <returns></returns>
    Public Property py As Integer
#End Region

#Region "slice physical xy"
    ''' <summary>
    ''' the slice physical x
    ''' </summary>
    ''' <returns></returns>
    Public Property x As Integer
    ''' <summary>
    ''' the slice physical y
    ''' </summary>
    ''' <returns></returns>
    Public Property y As Integer
#End Region

    Public Function GetPoint() As Point
        Return New Point(x, y)
    End Function

    Public Overrides Function ToString() As String
        Return $"[{x},{y}] {barcode}"
    End Function

End Class