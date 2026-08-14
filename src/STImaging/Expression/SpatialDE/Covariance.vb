Imports Erica.Analysis.SpatialTissue.Imaging.SpatialOmics.Math
Imports std = System.Math

' ============================================================================
' Covariance.vb — 空间协方差核函数
' ----------------------------------------------------------------------------
' 实现 SpatialDE 所需的三种协方差核：
'   1. SquaredExponential (SE)  — 指数衰减型空间相关
'   2. Linear (LIN)             — 线性核（非平稳）
'   3. Periodic (PER)           — 周期核
' 每种核接受长度尺度 l，输出 N×N 协方差矩阵。
' 参考：Svensson et al., Nat Methods 2018, Eq. 2 及 Supplementary。
' ============================================================================

Namespace SpatialOmics.SpatialDE

    ''' <summary>协方差核类型枚举</summary>
    Public Enum KernelType
        ''' <summary>平方指数核（默认，各向同性）</summary>
        SquaredExponential
        ''' <summary>线性核</summary>
        Linear
        ''' <summary>周期核</summary>
        Periodic
    End Enum

    ''' <summary>空间协方差核函数集合</summary>
    Public Module CovarianceKernels

        ''' <summary>
        ''' 平方指数核（高斯核）
        ''' k(x_i, x_j) = exp(-||x_i - x_j||^2 / (2·l^2))
        ''' </summary>
        ''' <param name="coords">N×D 空间坐标矩阵</param>
        ''' <param name="lengthScale">长度尺度参数 l</param>
        Public Function SquaredExponential(coords As Matrix, lengthScale As Double) As Matrix
            Dim n = coords.Rows
            Dim K As New Matrix(n, n)
            Dim invL2 As Double = 1.0 / (2.0 * lengthScale * lengthScale)
            For I As Integer = 0 To n - 1
                For j = I To n - 1
                    Dim d2 As Double = 0.0
                    For d = 0 To coords.Cols - 1
                        Dim diff = coords(I, d) - coords(j, d)
                        d2 += diff * diff
                    Next
                    Dim val = std.Exp(-d2 * invL2)
                    K(I, j) = val
                    K(j, I) = val
                Next
            Next
            Return K
        End Function

        ''' <summary>
        ''' 线性核
        ''' k(x_i, x_j) = 1 + (x_i · x_j) / l^2
        ''' 注意：线性核产生非平稳协方差。
        ''' </summary>
        Public Function Linear(coords As Matrix, lengthScale As Double) As Matrix
            Dim n = coords.Rows
            Dim K As New Matrix(n, n)
            Dim invL2 As Double = 1.0 / (lengthScale * lengthScale)
            For I As Integer = 0 To n - 1
                For j = I To n - 1
                    Dim dot As Double = 0.0
                    For d = 0 To coords.Cols - 1
                        dot += coords(I, d) * coords(j, d)
                    Next
                    Dim val = 1.0 + dot * invL2
                    K(I, j) = val
                    K(j, I) = val
                Next
            Next
            Return K
        End Function

        ''' <summary>
        ''' 周期核
        ''' k(x_i, x_j) = exp(-2·sin^2(π||x_i - x_j|| / p) / l^2)
        ''' 其中 p 为周期参数，通常设为 l 的比例缩放。
        ''' </summary>
        ''' <param name="coords">N×D 空间坐标</param>
        ''' <param name="lengthScale">长度尺度参数 l</param>
        ''' <param name="period">周期参数 p（默认 = l）</param>
        Public Function Periodic(coords As Matrix, lengthScale As Double,
                                 Optional period As Double? = Nothing) As Matrix
            Dim n = coords.Rows
            Dim p As Double = If(period, lengthScale)
            Dim K As New Matrix(n, n)
            Dim invL2 As Double = 1.0 / (2.0 * lengthScale * lengthScale)
            Dim piOverP As Double = std.PI / p
            For I As Integer = 0 To n - 1
                For j = I To n - 1
                    Dim dist As Double = 0.0
                    For d = 0 To coords.Cols - 1
                        dist += (coords(I, d) - coords(j, d)) ^ 2
                    Next
                    dist = std.Sqrt(dist)
                    Dim sinVal = std.Sin(piOverP * dist)
                    Dim val = std.Exp(-sinVal * sinVal * invL2)
                    K(I, j) = val
                    K(j, I) = val
                Next
            Next
            Return K
        End Function

        ''' <summary>通用接口：按核类型计算协方差矩阵</summary>
        Public Function ComputeKernel(coords As Matrix, lengthScale As Double,
                                     kernel As KernelType) As Matrix
            Select Case kernel
                Case KernelType.SquaredExponential
                    Return SquaredExponential(coords, lengthScale)
                Case KernelType.Linear
                    Return Linear(coords, lengthScale)
                Case KernelType.Periodic
                    Return Periodic(coords, lengthScale)
                Case Else
                    Return SquaredExponential(coords, lengthScale)
            End Select
        End Function

    End Module

End Namespace
