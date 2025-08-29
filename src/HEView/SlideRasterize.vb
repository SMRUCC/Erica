Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Language.Vectorization
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Parallel

Public Class SlideRasterize : Inherits VectorTask

    ReadOnly x As Vector
    ReadOnly y As Vector
    ReadOnly intensity As Vector()
    ReadOnly x_seq As Double()
    ReadOnly y_seq As Double()
    ReadOnly grid_matrix As Double()()()
    ReadOnly res As Single

    Private Sub New(slide As SlideSample,
                    x_seq As Double(),
                    y_seq As Double(),
                    res As Single,
                    Optional verbose As Boolean = False,
                    Optional workers As Integer? = Nothing)

        MyBase.New(x_seq.Length, verbose, workers)

        Me.x = slide.x
        Me.y = slide.y
        Me.intensity = slide.intensity
        Me.x_seq = x_seq
        Me.y_seq = y_seq
        Me.res = res

        grid_matrix = slide.intensity _
            .Select(Function(c)
                        Return RectangularArray.Matrix(Of Double)(y_seq.Length, x_seq.Length)
                    End Function) _
            .ToArray
    End Sub

    Protected Overrides Sub Solve(start As Integer, ends As Integer, cpu_id As Integer)
        For i As Integer = start To ends
            For j As Integer = 0 To y_seq.Length - 1
                Dim cell_x_min = x_seq(i)
                Dim cell_x_max = x_seq(i) + res
                Dim cell_y_min = y_seq(j)
                Dim cell_y_max = y_seq(j) + res
                ' 查找在当前单元格内的点
                Dim in_cell As BooleanVector = (x >= cell_x_min) & (x < cell_x_max) & (y >= cell_y_min) & (y < cell_y_max)

                If in_cell.Any Then
                    For k As Integer = 0 To intensity.Length - 1
                        Dim v = intensity(k)
                        Dim m = grid_matrix(k)

                        m(j)(i) = v(in_cell).Average  ' 取平均强度
                    Next
                End If
            Next
        Next
    End Sub

    Public Shared Function RasterizePoints(slide As SlideSample,
                                           xmin As Single,
                                           xmax As Single,
                                           ymin As Single,
                                           ymax As Single,
                                           res As Single) As Double()()()

        Dim x_seq As Double() = Vector.seq(xmin, xmax, by:=res)
        Dim y_seq As Double() = Vector.seq(ymin, ymax, by:=res)
        Dim raster As New SlideRasterize(slide, x_seq, y_seq, res)
        raster.Run()
        Return raster.grid_matrix
    End Function
End Class
