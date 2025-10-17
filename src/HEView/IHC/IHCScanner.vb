Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel

Public Class IHCScanner

    ReadOnly antibody As NamedCollection(Of Double)()
    ReadOnly A As Double(,)

    Sub New(antibody As Dictionary(Of String, Color))
        Me.antibody = antibody _
            .Select(Function(a)
                        Return New NamedCollection(Of Double)(a.Key, New Double() {a.Value.R / 255, a.Value.G / 255, a.Value.B / 255})
                    End Function) _
            .ToArray
        Me.A = New Double(2, antibody.Count - 1) {}

        For i As Integer = 0 To antibody.Count - 1
            Dim vec As Double() = Me.antibody(i).value

            A(0, i) = vec(0)
            A(1, i) = vec(1)
            A(2, i) = vec(2)
        Next
    End Sub

End Class
