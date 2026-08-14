' ============================================================================
' Statistics.vb — 统计分布与辅助函数（纯 BCL 实现）
' ----------------------------------------------------------------------------
' 提供 Gamma 函数、不完全 Gamma 函数、卡方分布 CDF / 生存函数、
' Benjamini-Hochberg FDR 校正等。
' ============================================================================

Imports System

Namespace SpatialOmics.Math

    ''' <summary>统计分布与多重检验校正工具</summary>
    Public Class Statistics

        ' ---- 常量 ----
        Private Const LogSqrt2Pi As Double = 0.91893853320467274178

        ' ---- Lanczos 近似系数（g=7, n=9）----
        Private Shared ReadOnly LanczosG As Double = 7.0
        Private Shared ReadOnly LanczosP As Double() = {
            0.99999999999980993, 676.5203681218851, -1259.1392167224028,
            771.32342877752699, -176.6150291621406, 12.507343278686905,
            -0.13857109526572012, 9.9843695780195716E-6, 1.5056327351493116E-7
        }

        ''' <summary>
        ''' Gamma 函数 Γ(x)（Lanczos 近似）
        ''' </summary>
        Public Shared Function Gamma(x As Double) As Double
            If x < 0.5 Then
                ' 反射公式：Γ(x)Γ(1-x) = π / sin(πx)
                Return Math.PI / (Math.Sin(Math.PI * x) * Gamma(1.0 - x))
            End If
            x -= 1.0
            Dim a = LanczosP(0)
            Dim t = x + LanczosG + 0.5
            For i = 1 To LanczosP.Length - 1
                a += LanczosP(i) / (x + i)
            Next
            Return Math.Sqrt(2.0 * Math.PI) * Math.Pow(t, x + 0.5) * Math.Exp(-t) * a
        End Function

        ''' <summary>
        ''' 对数 Gamma 函数 log Γ(x)
        ''' </summary>
        Public Shared Function LogGamma(x As Double) As Double
            If x < 0.5 Then
                Return Math.Log(Math.PI / Math.Sin(Math.PI * x)) - LogGamma(1.0 - x)
            End If
            x -= 1.0
            Dim a = LanczosP(0)
            Dim t = x + LanczosG + 0.5
            For i = 1 To LanczosP.Length - 1
                a += LanczosP(i) / (x + i)
            Next
            Return LogSqrt2Pi + (x + 0.5) * Math.Log(t) - t + Math.Log(a)
        End Function

        ''' <summary>
        ''' 正则化的下不完全 Gamma 函数 P(a, x) = γ(a, x) / Γ(a)
        ''' 使用级数展开（x < a+1）和连分数（x ≥ a+1）。
        ''' </summary>
        Public Shared Function LowerIncompleteGamma(a As Double, x As Double) As Double
            If x < 0 Then Throw New ArgumentException("x must be non-negative.")
            If x = 0 Then Return 0.0
            If x < a + 1.0 Then
                ' 级数展开
                Dim term As Double = 1.0 / a
                Dim sum As Double = term
                For n = 1 To 200
                    term *= x / (a + n)
                    sum += term
                    If Math.Abs(term) < Math.Abs(sum) * 1.0E-15 Then Exit For
                Next
                Return sum * Math.Exp(-x + a * Math.Log(x) - LogGamma(a))
            Else
                ' 连分数（Lentz 算法）
                Dim tiny As Double = 1.0E-300
                Dim b = x + 1.0 - a
                Dim c = 1.0 / tiny
                Dim d = 1.0 / b
                Dim h = d
                For i = 1 To 200
                    Dim an = -i * (i - a)
                    b += 2.0
                    d = an * d + b
                    If Math.Abs(d) < tiny Then d = tiny
                    c = b + an / c
                    If Math.Abs(c) < tiny Then c = tiny
                    d = 1.0 / d
                    Dim del = d * c
                    h *= del
                    If Math.Abs(del - 1.0) < 1.0E-15 Then Exit For
                Next
                Return 1.0 - Math.Exp(-x + a * Math.Log(x) - LogGamma(a)) * h
            End If
        End Function

        ''' <summary>
        ''' 卡方分布 CDF（自由度 k）
        ''' P(X ≤ x) = P(k/2, x/2)
        ''' </summary>
        Public Shared Function ChiSquaredCDF(x As Double, k As Integer) As Double
            If x <= 0 Then Return 0.0
            If k <= 0 Then Throw New ArgumentException("Degrees of freedom must be positive.")
            Return LowerIncompleteGamma(k / 2.0, x / 2.0)
        End Function

        ''' <summary>
        ''' 卡方分布生存函数（上尾概率）
        ''' P(X > x) = 1 - CDF
        ''' </summary>
        Public Shared Function ChiSquaredSF(x As Double, k As Integer) As Double
            Return 1.0 - ChiSquaredCDF(x, k)
        End Function

        ''' <summary>
        ''' Benjamini-Hochberg FDR 校正
        ''' 输入 p 值数组，返回校正后的 q 值数组（与原顺序一致）。
        ''' </summary>
        Public Shared Function BenjaminiHochberg(pValues As Double()) As Double()
            Dim n As Integer = pValues.Length
            If n = 0 Then Return New Double(-1) {}

            ' 创建 (原始索引, p值) 对并按 p 值升序排列
            Dim indexed = pValues.Select(
                Function(p, i) New With {Key .Idx = i, Key .P = p}).ToArray()
            Array.Sort(indexed, Function(a, b) a.P.CompareTo(b.P))

            ' 从后往前累积校正
            Dim q(n - 1) As Double
            Dim prevQ As Double = Double.MaxValue
            For i = n - 1 To 0 Step -1
                Dim rawQ = indexed(i).P * n / (i + 1)
                If rawQ < prevQ Then
                    prevQ = rawQ
                End If
                q(i) = Math.Min(prevQ, 1.0)
            Next

            ' 还原原始顺序
            Dim result(n - 1) As Double
            For i = 0 To n - 1
                result(indexed(i).Idx) = q(i)
            Next
            Return result
        End Function

        ''' <summary>样本均值</summary>
        Public Shared Function Mean(values As Double()) As Double
            If values.Length = 0 Then Return 0.0
            Dim sum As Double = 0.0
            For Each v In values
                sum += v
            Next
            Return sum / values.Length
        End Function

        ''' <summary>样本方差（无偏估计，除以 n-1）</summary>
        Public Shared Function Variance(values As Double()) As Double
            If values.Length < 2 Then Return 0.0
            Dim m = Mean(values)
            Dim ss As Double = 0.0
            For Each v In values
                ss += (v - m) * (v - m)
            Next
            Return ss / (values.Length - 1)
        End Function

        ''' <summary>样本标准差</summary>
        Public Shared Function StdDev(values As Double()) As Double
            Return Math.Sqrt(Variance(values))
        End Function

        ''' <summary>标准化（z-score）</summary>
        Public Shared Function Standardize(values As Double()) As Double()
            Dim m = Mean(values)
            Dim s = StdDev(values)
            If s = 0 Then
                Return values.Select(Function(v) 0.0).ToArray()
            End If
            Return values.Select(Function(v) (v - m) / s).ToArray()
        End Function

    End Class

End Namespace
