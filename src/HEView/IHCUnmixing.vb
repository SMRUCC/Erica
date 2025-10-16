Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Data.Bootstrapping
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Scripting.Runtime

Public Class IHCUnmixing
    ''' <summary>
    ''' 获取IHC1实验的参考颜色矩阵（4色+DAPI）
    ''' </summary>
    Public Shared Function GetReferenceMatrixIHC1() As Double(,)
        ' IHC1: 成分顺序: CD11b, CD11c, CD8, PanCK, Dapi
        ' 基于颜色名称假设的归一化RGB值（0-1范围）
        Dim A As Double(,) = New Double(2, 4) {} ' 3行(RGB), 5列

        ' CD11b: SpGr (绿色)
        A(0, 0) = 0.0   ' R
        A(1, 0) = 1.0   ' G  
        A(2, 0) = 0.0   ' B

        ' CD11c: SpOr (橙色)
        A(0, 1) = 1.0   ' R
        A(1, 1) = 0.647 ' G (165/255)
        A(2, 1) = 0.0   ' B

        ' CD8: SPRed (红色)
        A(0, 2) = 1.0   ' R
        A(1, 2) = 0.0   ' G
        A(2, 2) = 0.0   ' B

        ' PanCK: CY5.5 (品红色)
        A(0, 3) = 1.0   ' R
        A(1, 3) = 0.0   ' G
        A(2, 3) = 1.0   ' B

        ' Dapi (蓝色)
        A(0, 4) = 0.0   ' R
        A(1, 4) = 0.0   ' G
        A(2, 4) = 1.0   ' B

        Return A
    End Function

    ''' <summary>
    ''' 获取IHC2实验的参考颜色矩阵（5色+DAPI）
    ''' </summary>
    Public Shared Function GetReferenceMatrixIHC2() As Double(,)
        ' IHC2: 成分顺序: Lg6G, CiH3, p16, CD11b, PanCK, Dapi
        Dim A As Double(,) = New Double(2, 5) {} ' 3行(RGB), 6列

        ' Lg6G: SpGr (绿色)
        A(0, 0) = 0.0   ' R
        A(1, 0) = 1.0   ' G
        A(2, 0) = 0.0   ' B

        ' CiH3: SpOr (橙色)
        A(0, 1) = 1.0   ' R
        A(1, 1) = 0.647 ' G
        A(2, 1) = 0.0   ' B

        ' p16: SPRed (红色)
        A(0, 2) = 1.0   ' R
        A(1, 2) = 0.0   ' G
        A(2, 2) = 0.0   ' B

        ' CD11b: SpAqua (青色)
        A(0, 3) = 0.0   ' R
        A(1, 3) = 1.0   ' G
        A(2, 3) = 1.0   ' B

        ' PanCK: CY5.5 (品红色)
        A(0, 4) = 1.0   ' R
        A(1, 4) = 0.0   ' G
        A(2, 4) = 1.0   ' B

        ' Dapi (蓝色)
        A(0, 5) = 0.0   ' R
        A(1, 5) = 0.0   ' G
        A(2, 5) = 1.0   ' B

        Return A
    End Function

    ''' <summary>
    ''' 对单个像素进行抗体组成求解
    ''' </summary>
    ''' <param name="pixelColor">像素颜色</param>
    ''' <param name="A">参考颜色矩阵</param>
    ''' <param name="tolerance">容差</param>
    ''' <returns>各抗体贡献系数数组</returns>
    Public Shared Function UnmixPixel(pixelColor As Color, A As Double(,), Optional tolerance As Double = 0.00000001) As Double()
        ' 将RGB值归一化到0-1范围
        Dim b As Double() = New Double(2) {}
        b(0) = pixelColor.R / 255.0
        b(1) = pixelColor.G / 255.0
        b(2) = pixelColor.B / 255.0

        ' 调用NNLS算法求解
        Return NonNegativeLeastSquares.Solve(A, b, tolerance:=tolerance)
    End Function

    Public Shared Function Unmix(mixed As BitmapBuffer, A As Double(,), n As Integer) As BitmapBuffer()
        Dim layers As Color()() = RectangularArray.Matrix(Of Color)(n, mixed.Width * mixed.Height)
        Dim offset As Integer = 0

        For Each pixel As Color In mixed.GetPixelsAll
            Dim vec As Double() = UnmixPixel(pixel, A)
            Dim bytes As Integer() = SIMD.Multiply.f64_scalar_op_multiply_f64(255.0, vec).AsInteger

            For i As Integer = 0 To n - 1
                layers(i)(offset) = Color.FromArgb(bytes(i), bytes(i), bytes(i))
            Next

            offset += 1
        Next

        Return layers _
            .Select(Function(pixels)
                        Return New BitmapBuffer(pixels.Split(mixed.Width).ToMatrix, mixed.Size)
                    End Function) _
            .ToArray
    End Function

    ''' <summary>
    ''' 获取IHC1实验的抗体名称列表
    ''' </summary>
    Public Shared Function GetAntibodyNamesIHC1() As String()
        Return {"CD11b", "CD11c", "CD8", "PanCK", "Dapi"}
    End Function

    ''' <summary>
    ''' 获取IHC2实验的抗体名称列表
    ''' </summary>
    Public Shared Function GetAntibodyNamesIHC2() As String()
        Return {"Lg6G", "CiH3", "p16", "CD11b", "PanCK", "Dapi"}
    End Function
End Class

