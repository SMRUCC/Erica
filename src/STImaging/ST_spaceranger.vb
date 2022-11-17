Imports System.Drawing
Imports System.Runtime.CompilerServices

Public Module ST_spaceranger

    <Extension>
    Public Iterator Function LoadTissueSpots(data As IEnumerable(Of String)) As IEnumerable(Of SpaceSpot)
        For Each line As String In data
            Dim t As String() = line.Split(","c)
            Dim spot As New SpaceSpot With {
                .barcode = t(0),
                .t1 = Integer.Parse(t(1)),
                .t2 = Integer.Parse(t(2)),
                .index = Integer.Parse(t(3)),
                .x = Integer.Parse(t(4)),
                .y = Integer.Parse(t(5))
            }

            Yield spot
        Next
    End Function

End Module

Public Class SpaceSpot

    Public Property barcode As String
    Public Property t1 As Integer
    Public Property t2 As Integer
    Public Property index As Integer
    Public Property x As Integer
    Public Property y As Integer

    Public Function GetPoint() As Point
        Return New Point(x, y)
    End Function

    Public Overrides Function ToString() As String
        Return $"[{x},{y}] {barcode}"
    End Function

End Class