Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing2D.HeatMap
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports STRaid

Public Class Render

    Dim matrix As Dictionary(Of String, DataFrameRow)
    Dim pixels As Point()

    Public ReadOnly Property geneIDs As String()
        Get
            Return matrix.Keys.ToArray
        End Get
    End Property

    Sub New(anndat As AnnData)
        Dim matrix = anndat.ExportExpression.T

        Me.matrix = matrix.expression.ToDictionary(Function(a) a.geneID)
        Me.pixels = matrix.sampleID _
            .Select(Function(str)
                        Dim t As Integer() = str _
                            .Split(","c) _
                            .Select(AddressOf Integer.Parse) _
                            .ToArray
                        Dim p As New Point(t(0), t(1))

                        Return p
                    End Function) _
            .ToArray
    End Sub

    Public Iterator Function GetLayer(geneId As String) As IEnumerable(Of PixelData)
        Dim vec As DataFrameRow = matrix(geneId)

        For i As Integer = 0 To pixels.Length - 1
            Yield New PixelData(pixels(i), vec.experiments(i))
        Next
    End Function

End Class
