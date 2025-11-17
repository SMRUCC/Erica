Imports Microsoft.VisualBasic.Serialization.JSON

Public Class IHCCellScan : Inherits CellScan

    ''' <summary>
    ''' the antibody name used in IHC staining
    ''' </summary>
    ''' <returns></returns>
    Public Property antibody As Dictionary(Of String, Double)

    ''' <summary>
    ''' get the antibody weight data of this cell object 
    ''' </summary>
    ''' <param name="name"></param>
    ''' <returns></returns>
    Default Public ReadOnly Property data(name As String) As Double
        Get
            If antibody.ContainsKey(name) Then
                Return antibody(name)
            Else
                Return 0.0
            End If
        End Get
    End Property

    Sub New()
    End Sub

    Friend Overrides Function Clone(Optional ByRef cell As CellScan = Nothing) As CellScan
        If cell Is Nothing Then
            cell = New IHCCellScan
        End If

        Call MyBase.Clone(cell)

        If TypeOf cell Is IHCCellScan AndAlso Not antibody Is Nothing Then
            DirectCast(cell, IHCCellScan).antibody = New Dictionary(Of String, Double)(antibody)
        End If

        Return cell
    End Function

    Public Overrides Function ToString() As String
        Return MyBase.ToString() & "; antibody=" & antibody.GetJson
    End Function

End Class
