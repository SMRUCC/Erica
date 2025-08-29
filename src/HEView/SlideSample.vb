Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Language.Vectorization
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Math.Scripting.BasicR
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
            .intensity = intensity
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

    Public Function ObjectiveFunction(theta As Single, tx As Single, ty As Single, sx As Single, sy As Single, res As Single)
        Dim df2_transformed = Transform(theta, tx, ty, sx, sy)
        ' 设置网格范围（基于df1和变换后df2的边界，并添加10%缓冲）
        Dim all_x = base.c(x, df2_transformed.x)
        Dim all_y = base.c(y, df2_transformed.y)
        Dim xmin = all_x.Min - 0.1 * diff(Range(all_x))
        Dim xmax = all_x.Max + 0.1 * diff(Range(all_x))
        Dim ymin = all_y.Min - 0.1 * diff(Range(all_y))
        Dim ymax = all_y.Max + 0.1 * diff(Range(all_y))

        ' 栅格化df1和变换后的df2
        Dim grid1 = RasterizePoints(xmin, xmax, ymin, ymax, res)
        Dim grid2 = df2_transformed.RasterizePoints(xmin, xmax, ymin, ymax, res)

        ' 提取非NA的单元格（仅处理两者都有值的单元格）
        Dim valid_cells = !Is.na(grid1) & !Is.na(grid2)
        Dim intensity1 = grid1(valid_cells)
        Dim intensity2 = grid2(valid_cells)

        ' 如果有效点太少，返回高损失
        If (intensity1.Length < 10 OrElse intensity2.Length < 10) Then
            Return (1000)  ' 惩罚值
        End If

        ' 计算皮尔森相关性
        Dim cor_value = cor(intensity1, intensity2, method = "pearson")
        ' 如果相关性为NA（如常数向量），返回高损失
        If (Is .na(cor_value)) Then
            Return (1000)
        End If

        ' 返回负相关性（因为optim最小化目标）
        Return (-cor_value)
    End Function
End Class
