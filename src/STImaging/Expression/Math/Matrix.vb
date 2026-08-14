' ============================================================================
' Matrix.vb — 基于 BCL 的矩阵运算库
' ----------------------------------------------------------------------------
' 提供矩阵创建、算术运算、转置、求逆、行列式、Cholesky 分解等功能。
' 所有实现仅依赖 System.Math 和 System.Linq，不引入第三方数值库。
' ============================================================================

Imports System
Imports System.Linq

Namespace SpatialOmics.Math

    ''' <summary>
    ''' 二维实数矩阵，提供基础线性代数运算。
    ''' </summary>
    Public Class Matrix
        Implements ICloneable

        Private _data As Double(,)
        Private _rows As Integer
        Private _cols As Integer

        ''' <summary>行数</summary>
        Public ReadOnly Property Rows As Integer
            Get
                Return _rows
            End Get
        End Property

        ''' <summary>列数</summary>
        Public ReadOnly Property Cols As Integer
            Get
                Return _cols
            End Get
        End Property

        ''' <summary>按索引访问元素</summary>
        Default Public Property Item(r As Integer, c As Integer) As Double
            Get
                Return _data(r, c)
            End Get
            Set(value As Double)
                _data(r, c) = value
            End Set
        End Property

        ''' <summary>创建指定大小的零矩阵</summary>
        Public Sub New(rows As Integer, cols As Integer)
            If rows <= 0 OrElse cols <= 0 Then
                Throw New ArgumentException("Matrix dimensions must be positive.")
            End If
            _rows = rows
            _cols = cols
            _data = New Double(rows - 1, cols - 1) {}
        End Sub

        ''' <summary>从二维数组构造</summary>
        Public Sub New(data As Double(,))
            _rows = data.GetLength(0)
            _cols = data.GetLength(1)
            _data = CType(data.Clone(), Double(,))
        End Sub

        ''' <summary>创建单位矩阵</summary>
        Public Shared Function Identity(n As Integer) As Matrix
            Dim m As New Matrix(n, n)
            For i = 0 To n - 1
                m._data(i, i) = 1.0
            Next
            Return m
        End Function

        ''' <summary>从行优先一维数组构造</summary>
        Public Shared Function FromRows(rows As Integer, cols As Integer, values As Double()) As Matrix
            If values.Length <> rows * cols Then
                Throw New ArgumentException("Array length does not match dimensions.")
            End If
            Dim m As New Matrix(rows, cols)
            For i = 0 To rows - 1
                For j = 0 To cols - 1
                    m._data(i, j) = values(i * cols + j)
                Next
            Next
            Return m
        End Function

        ''' <summary>矩阵加法</summary>
        Public Shared Operator +(a As Matrix, b As Matrix) As Matrix
            If a._rows <> b._rows OrElse a._cols <> b._cols Then
                Throw New ArgumentException("Matrix dimensions must match for addition.")
            End If
            Dim result As New Matrix(a._rows, a._cols)
            For i = 0 To a._rows - 1
                For j = 0 To a._cols - 1
                    result._data(i, j) = a._data(i, j) + b._data(i, j)
                Next
            Next
            Return result
        End Operator

        ''' <summary>矩阵减法</summary>
        Public Shared Operator -(a As Matrix, b As Matrix) As Matrix
            If a._rows <> b._rows OrElse a._cols <> b._cols Then
                Throw New ArgumentException("Matrix dimensions must match for subtraction.")
            End If
            Dim result As New Matrix(a._rows, a._cols)
            For i = 0 To a._rows - 1
                For j = 0 To a._cols - 1
                    result._data(i, j) = a._data(i, j) - b._data(i, j)
                Next
            Next
            Return result
        End Operator

        ''' <summary>矩阵乘法</summary>
        Public Shared Operator *(a As Matrix, b As Matrix) As Matrix
            If a._cols <> b._rows Then
                Throw New ArgumentException(
                    $"Matrix multiply dimension mismatch: ({a._rows}x{a._cols}) * ({b._rows}x{b._cols})")
            End If
            Dim result As New Matrix(a._rows, b._cols)
            For i = 0 To a._rows - 1
                For j = 0 To b._cols - 1
                    Dim sum As Double = 0.0
                    For k = 0 To a._cols - 1
                        sum += a._data(i, k) * b._data(k, j)
                    Next
                    result._data(i, j) = sum
                Next
            Next
            Return result
        End Operator

        ''' <summary>标量乘法</summary>
        Public Shared Operator *(a As Matrix, s As Double) As Matrix
            Dim result As New Matrix(a._rows, a._cols)
            For i = 0 To a._rows - 1
                For j = 0 To a._cols - 1
                    result._data(i, j) = a._data(i, j) * s
                Next
            Next
            Return result
        End Operator

        ''' <summary>标量乘法（对称形式）</summary>
        Public Shared Operator *(s As Double, a As Matrix) As Matrix
            Return a * s
        End Operator

        ''' <summary>矩阵加常数</summary>
        Public Function AddScalar(s As Double) As Matrix
            Dim result As New Matrix(_rows, _cols)
            For i = 0 To _rows - 1
                For j = 0 To _cols - 1
                    result._data(i, j) = _data(i, j) + s
                Next
            Next
            Return result
        End Function

        ''' <summary>逐元素乘（Hadamard 积）</summary>
        Public Function ElementwiseMultiply(b As Matrix) As Matrix
            If _rows <> b._rows OrElse _cols <> b._cols Then
                Throw New ArgumentException("Dimensions must match for element-wise multiply.")
            End If
            Dim result As New Matrix(_rows, _cols)
            For i = 0 To _rows - 1
                For j = 0 To _cols - 1
                    result._data(i, j) = _data(i, j) * b._data(i, j)
                Next
            Next
            Return result
        End Function

        ''' <summary>矩阵乘法：Me × b</summary>
        Public Function Multiply(b As Matrix) As Matrix
            If _cols <> b._rows Then
                Throw New ArgumentException(
                    $"维度不匹配: ({_rows}x{_cols}) × ({b._rows}x{b._cols})")
            End If
            Dim result As New Matrix(_rows, b._cols)
            For i = 0 To _rows - 1
                For j = 0 To b._cols - 1
                    Dim s As Double = 0.0
                    For k = 0 To _cols - 1
                        s += _data(i, k) * b._data(k, j)
                    Next
                    result._data(i, j) = s
                Next
            Next
            Return result
        End Function

        ''' <summary>转置</summary>
        Public Function Transpose() As Matrix
            Dim result As New Matrix(_cols, _rows)
            For i = 0 To _rows - 1
                For j = 0 To _cols - 1
                    result._data(j, i) = _data(i, j)
                Next
            Next
            Return result
        End Function

        ''' <summary>提取列向量</summary>
        Public Function GetColumn(col As Integer) As Double()
            Dim v(_rows - 1) As Double
            For i = 0 To _rows - 1
                v(i) = _data(i, col)
            Next
            Return v
        End Function

        ''' <summary>提取行向量</summary>
        Public Function GetRow(row As Integer) As Double()
            Dim v(_cols - 1) As Double
            For j = 0 To _cols - 1
                v(j) = _data(row, j)
            Next
            Return v
        End Function

        ''' <summary>矩阵 × 向量</summary>
        Public Function MultiplyVector(v As Double()) As Double()
            If _cols <> v.Length Then
                Throw New ArgumentException("Vector length must match matrix columns.")
            End If
            Dim result(_rows - 1) As Double
            For i = 0 To _rows - 1
                Dim sum As Double = 0.0
                For j = 0 To _cols - 1
                    sum += _data(i, j) * v(j)
                Next
                result(i) = sum
            Next
            Return result
        End Function

        ''' <summary>行列式（通过 LU 分解）</summary>
        Public Function Determinant() As Double
            If _rows <> _cols Then
                Throw New InvalidOperationException("Determinant requires square matrix.")
            End If
            Dim n As Integer = _rows
            ' LU 分解（部分主元）
            Dim lu(n - 1, n - 1) As Double
            Array.Copy(_data, lu, _data.Length)
            Dim perm(n - 1) As Integer
            For i = 0 To n - 1
                perm(i) = i
            Next
            Dim det As Double = 1.0
            For k = 0 To n - 1
                ' 选主元
                Dim maxVal As Double = Math.Abs(lu(k, k))
                Dim maxRow As Integer = k
                For i = k + 1 To n - 1
                    If Math.Abs(lu(i, k)) > maxVal Then
                        maxVal = Math.Abs(lu(i, k))
                        maxRow = i
                    End If
                Next
                If maxVal < 1.0E-300 Then
                    Return 0.0 ' 奇异矩阵
                End If
                If maxRow <> k Then
                    For j = 0 To n - 1
                        Dim tmp = lu(k, j)
                        lu(k, j) = lu(maxRow, j)
                        lu(maxRow, j) = tmp
                    Next
                    Dim tmpP = perm(k)
                    perm(k) = perm(maxRow)
                    perm(maxRow) = tmpP
                    det = -det
                End If
                det *= lu(k, k)
                For i = k + 1 To n - 1
                    lu(i, k) /= lu(k, k)
                    For j = k + 1 To n - 1
                        lu(i, j) -= lu(i, k) * lu(k, j)
                    Next
                Next
            Next
            Return det
        End Function

        ''' <summary>对数行列式（通过 Cholesky 分解，要求正定矩阵）</summary>
        Public Function LogDetPosDef() As Double
            Dim L = Cholesky()
            Dim logDet As Double = 0.0
            For i = 0 To _rows - 1
                logDet += 2.0 * Math.Log(L._data(i, i))
            Next
            Return logDet
        End Function

        ''' <summary>
        ''' Cholesky 分解：A = L·Lᵀ（要求对称正定矩阵）
        ''' 返回下三角矩阵 L。
        ''' </summary>
        Public Function Cholesky() As Matrix
            If _rows <> _cols Then
                Throw New InvalidOperationException("Cholesky requires square matrix.")
            End If
            Dim n As Integer = _rows
            Dim L As New Matrix(n, n)
            For i = 0 To n - 1
                For j = 0 To i
                    Dim sum As Double = _data(i, j)
                    For k = 0 To j - 1
                        sum -= L._data(i, k) * L._data(j, k)
                    Next
                    If i = j Then
                        If sum <= 0 Then
                            Throw New InvalidOperationException(
                                $"Matrix is not positive definite at element ({i},{j}).")
                        End If
                        L._data(i, j) = Math.Sqrt(sum)
                    Else
                        L._data(i, j) = sum / L._data(j, j)
                    End If
                Next
            Next
            Return L
        End Function

        ''' <summary>
        ''' 通过 Cholesky 分解求解 Ax = b（要求正定对称矩阵）
        ''' </summary>
        Public Function SolveCholesky(b As Double()) As Double()
            If _rows <> _cols Then
                Throw New InvalidOperationException("Solve requires square matrix.")
            End If
            If b.Length <> _rows Then
                Throw New ArgumentException("Vector length must match matrix dimension.")
            End If
            Dim n As Integer = _rows
            Dim L = Cholesky()
            ' 前代：L y = b
            Dim y(n - 1) As Double
            For i = 0 To n - 1
                Dim sum As Double = b(i)
                For j = 0 To i - 1
                    sum -= L._data(i, j) * y(j)
                Next
                y(i) = sum / L._data(i, i)
            Next
            ' 回代：Lᵀ x = y
            Dim x(n - 1) As Double
            For i = n - 1 To 0 Step -1
                Dim sum As Double = y(i)
                For j = i + 1 To n - 1
                    sum -= L._data(j, i) * x(j)
                Next
                x(i) = sum / L._data(i, i)
            Next
            Return x
        End Function

        ''' <summary>求逆（通过 LU 分解 + 回代）</summary>
        Public Function Inverse() As Matrix
            If _rows <> _cols Then
                Throw New InvalidOperationException("Inverse requires square matrix.")
            End If
            Dim n As Integer = _rows
            ' 使用 Gauss-Jordan 消元
            Dim aug(n - 1, 2 * n - 1) As Double
            For i = 0 To n - 1
                For j = 0 To n - 1
                    aug(i, j) = _data(i, j)
                Next
                aug(i, n + i) = 1.0
            Next
            For k = 0 To n - 1
                ' 选主元
                Dim maxVal As Double = Math.Abs(aug(k, k))
                Dim maxRow As Integer = k
                For i = k + 1 To n - 1
                    If Math.Abs(aug(i, k)) > maxVal Then
                        maxVal = Math.Abs(aug(i, k))
                        maxRow = i
                    End If
                Next
                If maxVal < 1.0E-300 Then
                    Throw New InvalidOperationException("Matrix is singular, cannot invert.")
                End If
                If maxRow <> k Then
                    For j = 0 To 2 * n - 1
                        Dim tmp = aug(k, j)
                        aug(k, j) = aug(maxRow, j)
                        aug(maxRow, j) = tmp
                    Next
                End If
                ' 归一化主元行
                Dim pivot As Double = aug(k, k)
                For j = 0 To 2 * n - 1
                    aug(k, j) /= pivot
                Next
                ' 消元
                For i = 0 To n - 1
                    If i <> k Then
                        Dim factor = aug(i, k)
                        For j = 0 To 2 * n - 1
                            aug(i, j) -= factor * aug(k, j)
                        Next
                    End If
                Next
            Next
            Dim result As New Matrix(n, n)
            For i = 0 To n - 1
                For j = 0 To n - 1
                    result._data(i, j) = aug(i, n + j)
                Next
            Next
            Return result
        End Function

        ''' <summary>求迹</summary>
        Public Function Trace() As Double
            If _rows <> _cols Then
                Throw New InvalidOperationException("Trace requires square matrix.")
            End If
            Dim tr As Double = 0.0
            For i = 0 To _rows - 1
                tr += _data(i, i)
            Next
            Return tr
        End Function

        ''' <summary>返回原始二维数组副本</summary>
        Public Function ToArray() As Double(,)
            Return CType(_data.Clone(), Double(,))
        End Function

        ''' <summary>深拷贝</summary>
        Public Function Clone() As Object Implements ICloneable.Clone
            Return New Matrix(_data)
        End Function

        ''' <summary>字符串表示（截断显示）</summary>
        Public Overrides Function ToString() As String
            Dim maxRows As Integer = Math.Min(_rows, 6)
            Dim maxCols As Integer = Math.Min(_cols, 6)
            Dim sb As New Text.StringBuilder()
            sb.AppendLine($"Matrix [{_rows}x{_cols}]")
            For i = 0 To maxRows - 1
                For j = 0 To maxCols - 1
                    sb.Append($"{_data(i, j),12:F6} ")
                Next
                If _cols > maxCols Then sb.Append("...")
                sb.AppendLine()
            Next
            If _rows > maxRows Then sb.AppendLine("...")
            Return sb.ToString()
        End Function

    End Class

End Namespace
