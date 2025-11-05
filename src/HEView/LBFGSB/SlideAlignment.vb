Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.Framework.Optimization.LBFGSB
Imports Microsoft.VisualBasic.Math.Scripting.BasicR
Imports Microsoft.VisualBasic.Serialization.JSON
Imports std = System.Math

Public Class SlideAlignment : Inherits IGradFunction

    Const Inf = Double.PositiveInfinity
    Const pi = std.PI

    ReadOnly df1 As SlideSample
    ReadOnly df2 As SlideSample
    ReadOnly res As Double

    Private Sub New(df1 As SlideSample, df2 As SlideSample, res As Double)
        Me.df1 = df1
        Me.df2 = df2
        Me.res = res
    End Sub

    Public Shared Function MakeAlignment(ref As SlideSample, target As SlideSample, Optional maxit As Integer = 50) As SlideSample
        ' 主优化部分
        ' 假设df1和df2已存在：dataframe With x, y, intensity
        ' 设置网格分辨率res（根据数据调整：例如取平均点距的一半）
        ' 计算df1和df2的平均点距作为参考
        Dim avg_dist = base.mean(base.c(base.diff(base.range(ref.x)), base.diff(base.range(ref.y)), base.diff(base.range(target.x)), base.diff(base.range(target.y)))) / 50
        Dim res = avg_dist  ' 默认分辨率，可根据需要调整
        ' 初始参数：无变换
        Dim initial_params = New Double() {0.0, 0.0, 0.0, 1.0, 1.0}  ' theta= 0, tx=0, ty=0, sx=1, sy=1
        ' 设置优化边界
        Dim lower_bounds = New Double() {0.0, -Inf, -Inf, 0.1, 0.1}  ' theta最小0, sx/sy最小0.1
        Dim upper_bounds = New Double() {2 * pi, Inf, Inf, 10.0, 10.0}  ' theta最大2π, sx/sy最大10
        Dim lbfgsb As New LBFGSB
        Dim optf As New SlideAlignment(ref, target, res)
        Dim result As Double() = lbfgsb.maxit(maxit).debug.minimize(optf, initial_params, lower_bounds, upper_bounds)

        Call ("!RESULT").debug
        Call ("k = " & lbfgsb.k.ToString()).debug
        Call ("x = " & result.GetJson).debug
        Call ("fx = " & lbfgsb.fx.ToString()).debug
        Call ("grad = " & lbfgsb.m_grad.GetJson).debug

        target = target.Transform(result(0), result(1), result(2), result(3), result(4))

        Return target
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Function evaluate(x() As Double) As Double
        Return ObjectiveFunction(x(0), x(1), x(2), x(3), x(4))
    End Function

    Public Function ObjectiveFunction(theta As Single, tx As Single, ty As Single, sx As Single, sy As Single) As Double
        Dim df2_transformed = df2.Transform(theta, tx, ty, sx, sy)
        ' 设置网格范围（基于df1和变换后df2的边界，并添加10%缓冲）
        Dim all_x = base.c(df1.x, df2_transformed.x)
        Dim all_y = base.c(df1.y, df2_transformed.y)
        Dim xmin = all_x.Min - 0.1 * base.diff(base.range(all_x))(0)
        Dim xmax = all_x.Max + 0.1 * base.diff(base.range(all_x))(0)
        Dim ymin = all_y.Min - 0.1 * base.diff(base.range(all_y))(0)
        Dim ymax = all_y.Max + 0.1 * base.diff(base.range(all_y))(0)

        ' 栅格化df1和变换后的df2
        Dim grid1 = df1.RasterizePoints(xmin, xmax, ymin, ymax, res)
        Dim grid2 = df2_transformed.RasterizePoints(xmin, xmax, ymin, ymax, res)

        ' 提取非NA的单元格（仅处理两者都有值的单元格）
        ' Dim valid_cells = !is.na(grid1) & !is.na(grid2)
        Dim intensity1 = grid1 '(valid_cells)
        Dim intensity2 = grid2 '(valid_cells)

        ' 如果有效点太少，返回高损失
        If (intensity1(0).Length < 10 OrElse intensity2(0).Length < 10) Then
            Return df1.intensity(0).Length * 1000 / (intensity1(0).Length + 1) ' 惩罚值
        End If

        Dim checkW = df2_transformed.x.Range.MinMax
        Dim checkH = df2_transformed.y.Range.MinMax

        ' 计算皮尔森相关性
        Dim cor_value = df1.intensity _
            .Select(Function(v, i)
                        Return cor(intensity1(i), intensity2(i), method:="pearson")
                    End Function) _
            .Select(Function(cor)
                        Return cor.IteratesALL.Average
                    End Function) _
            .ToArray

        ' 如果相关性为NA（如常数向量），返回高损失
        If ([is].na(cor_value).Any) Then
            Return 1000 * cor_value.Count(Function(xi) xi.IsNaNImaginary)
        End If

        If checkW.Any(Function(a) a < 0) Then
            Return -checkW.Min
        ElseIf checkH.Any(Function(a) a < 0) Then
            Return -checkH.Min
        End If

        ' 返回负相关性（因为optim最小化目标）
        Return -cor_value.Average
    End Function
End Class
