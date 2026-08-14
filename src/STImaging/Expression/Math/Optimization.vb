' ============================================================================
' Optimization.vb — 1-D / 多维优化器（纯 BCL 实现）
' ----------------------------------------------------------------------------
' 提供 Brent 一维搜索（用于 SpatialDE 的 length scale 网格优化后的精调）
' 和坐标下降法（用于多参数优化）。
' ============================================================================

Imports System

Namespace SpatialOmics.Math

    ''' <summary>数值优化工具</summary>
    Public Class Optimization

        ''' <summary>
        ''' Brent 一维最小化方法（不使用导数）
        ''' 在 [a, b] 上寻找 f(x) 的最小值。
        ''' </summary>
        ''' <param name="f">目标函数</param>
        ''' <param name="a">区间左端</param>
        ''' <param name="b">区间右端</param>
        ''' <param name="tol">收敛容差（默认 1e-6）</param>
        ''' <param name="maxIter">最大迭代次数（默认 200）</param>
        ''' <returns>(最优 x, 最优 f(x))</returns>
        Public Shared Function BrentMinimize(
                f As Func(Of Double, Double),
                a As Double, b As Double,
                Optional tol As Double = 0.000001,
                Optional maxIter As Integer = 200) As (x As Double, fx As Double)

            Const golden As Double = 0.3819660112501051 ' (3 - √5) / 2
            Const c As Double = 1.0 - golden           ' (3 - √5) / 2 的补
            Const eps As Double = 2.220446049250313E-16  ' 机器 ε

            Dim x = a + golden * (b - a) ' 第一个内点
            Dim w = x, v = x             ' 前面两个点
            Dim fx = f(x)
            Dim fw = fx, fv = fx
            Dim d As Double = 0.0, e As Double = 0.0
            Dim u As Double, fu As Double

            For iter = 1 To maxIter
                Dim m = 0.5 * (a + b)
                Dim tol1 = tol * Math.Abs(x) + eps
                Dim tol2 = 2.0 * tol1

                ' 收敛检查
                If Math.Abs(x - m) <= tol2 - 0.5 * (b - a) Then
                    Return (x, fx)
                End If

                Dim useParabolic As Boolean = False

                ' 尝试抛物线拟合
                If Math.Abs(e) > tol1 Then
                    Dim r = (x - w) * (fx - fv)
                    Dim q = (x - v) * (fx - fw)
                    Dim p = (x - v) * q - (x - w) * r
                    q = 2.0 * (q - r)
                    If q > 0 Then p = -p
                    q = Math.Abs(q)
                    Dim etemp = e
                    e = d

                    If Math.Abs(p) < Math.Abs(0.5 * q * etemp) AndAlso
                       p > q * (a - x) AndAlso p < q * (b - x) Then
                        ' 抛物线插值可行
                        d = p / q
                        u = x + d
                        If u - a < tol2 OrElse b - u < tol2 Then
                            d = If(x < m, tol1, -tol1)
                        End If
                        useParabolic = True
                    End If
                End If

                If Not useParabolic Then
                    ' 黄金分割
                    e = If(x >= m, a - x, b - x)
                    d = c * e
                End If

                u = x + If(Math.Abs(d) >= tol1, d, If(d > 0, tol1, -tol1))
                fu = f(u)

                If fu <= fx Then
                    If u < x Then b = x Else a = x
                    v = w : fv = fw
                    w = x : fw = fx
                    x = u : fx = fu
                Else
                    If u < x Then a = u Else b = u
                    If fu <= fw OrElse w = x Then
                        v = w : fv = fw
                        w = u : fw = fu
                    ElseIf fu <= fv OrElse v = x OrElse v = w Then
                        v = u : fv = fu
                    End If
                End If
            Next

            Return (x, fx)
        End Function

        ''' <summary>
        ''' 坐标下降法：逐维度用 Brent 一维搜索优化。
        ''' 适用于可分离性较强的目标函数。
        ''' </summary>
        ''' <param name="f">目标函数：接受参数向量，返回标量</param>
        ''' <param name="x0">初始点</param>
        ''' <param name="bounds">各维度的上下界 [(lo, hi), ...]</param>
        ''' <param name="tol">收敛容差</param>
        ''' <param name="maxIter">最大外循环次数</param>
        ''' <returns>最优参数向量</returns>
        Public Shared Function CoordinateDescent(
                f As Func(Of Double(), Double),
                x0 As Double(),
                bounds As (lo As Double, hi As Double)(),
                Optional tol As Double = 0.000001,
                Optional maxIter As Integer = 50) As Double()

            Dim n = x0.Length
            Dim x = CType(x0.Clone(), Double())
            Dim fPrev = f(x)

            For iter = 1 To maxIter
                Dim improved As Boolean = False
                For i = 0 To n - 1
                    ' 一维搜索：固定其他维度，优化第 i 维
                    Dim xi = x(i)
                    Dim f1D As Func(Of Double, Double) = Function(t)
                                                                 x(i) = t
                                                                 Return f(x)
                                                             End Function
                    Dim (bestT, bestF) = BrentMinimize(f1D, bounds(i).lo, bounds(i).hi, tol)
                    x(i) = bestT
                    If bestF < fPrev - tol Then improved = True
                Next
                Dim fNew = f(x)
                If Not improved AndAlso Math.Abs(fPrev - fNew) < tol Then Exit For
                fPrev = fNew
            Next
            Return x
        End Function

    End Class

End Namespace
