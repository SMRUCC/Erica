Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Language.Vectorization
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports std = System.Math

Public Class SlideSample

    Public Property x As Vector
    Public Property y As Vector
    Public Property intensity As Vector()

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="theta">旋转角度（弧度）</param>
    ''' <param name="tx">平移量</param>
    ''' <param name="ty">平移量</param>
    ''' <param name="sx">缩放因子</param>
    ''' <param name="sy">缩放因子</param>
    ''' <returns></returns>
    Public Function Transform(theta As Single, tx As Single, ty As Single, sx As Single, sy As Single) As SlideSample
        Dim x_scaled = x * sx
        Dim y_scaled = y * sy
        ' 旋转（逆时针旋转theta弧度）
        Dim x_rot = x_scaled * std.Cos(theta) - y_scaled * std.Sin(theta)
        Dim y_rot = x_scaled * std.Sin(theta) + y_scaled * std.Cos(theta)
        ' 平移
        Dim x_new = x_rot + tx
        Dim y_new = y_rot + ty

        Return New SlideSample With {
            .x = x_new,
            .y = y_new,
            .intensity = intensity.ToArray
        }
    End Function

    Public Function RasterizePoints(xmin As Single, xmax As Single, ymin As Single, ymax As Single, res As Single) As Double()()()
        Dim x_seq As Double() = Vector.seq(xmin, xmax, by:=res)
        Dim y_seq As Double() = Vector.seq(ymin, ymax, by:=res)
        Dim grid_matrix As Double()()() = intensity _
            .Select(Function(c)
                        Return RectangularArray.Matrix(Of Double)(y_seq.Length, x_seq.Length)
                    End Function) _
            .ToArray

        For i As Integer = 0 To x_seq.Length - 1
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

        Return grid_matrix
    End Function
End Class
