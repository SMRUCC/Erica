
Imports System.Drawing

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

    Public Function GetSpotPoint() As Point
        Return New Point(px, py)
    End Function

    ''' <summary>
    ''' get physical point
    ''' </summary>
    ''' <returns></returns>
    Public Function GetPoint() As Point
        Return New Point(x, y)
    End Function

    Public Overrides Function ToString() As String
        Return $"[{x},{y}] {barcode}"
    End Function

End Class