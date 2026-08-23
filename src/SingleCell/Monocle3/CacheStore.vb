Imports System.IO
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports stdNum = System.Math

Namespace SMRUCC.genomics.SingleCell.Monocle3

    ''' <summary>
    ''' 中间数据缓存基础设施。
    ''' 
    ''' 由于测试数据集较大（1800+ 样本），每个耗时步骤（PCA、UMAP、KNN 图、
    ''' 分群、MST、伪时间、PAGA、Moran）完成后会立即将结果落盘。重跑时若
    ''' 开启 useCache 且缓存文件存在，则直接反序列化跳过计算，仅重算断点之后
    ''' 的步骤，从而显著节省因异常中断而反复重跑全流程的调试时间。
    ''' 
    ''' 约定：
    ''' - 矩阵类数据（Double(,)、Double()、Integer()、String()）使用 CSV 文本缓存。
    ''' - 图与分群类数据使用 JSON 缓存（sciBASIC# DataContractJsonSerializer）。
    ''' </summary>
    Public Class CacheStore

        Public ReadOnly Property cacheDir As String

        Sub New(cacheDir As String)
            Me.cacheDir = cacheDir

            If Not Directory.Exists(cacheDir) Then
                Call Directory.CreateDirectory(cacheDir)
            End If
        End Sub

        ''' <summary>
        ''' 生成缓存文件路径（带步骤序号前缀，便于按步骤定位与人工检查）。
        ''' </summary>
        Public Function Path(key As String) As String
            Return IO.Path.Combine(cacheDir, key)
        End Function

        ''' <summary>
        ''' 缓存是否命中：文件存在且长度大于 0。
        ''' </summary>
        Public Function Hit(key As String) As Boolean
            Dim p = Path(key)
            Return File.Exists(p) AndAlso New FileInfo(p).Length > 0
        End Function

        ' ===================== CSV 矩阵类缓存 =====================

        ''' <summary>
        ''' 将 [n × m] 的 Double 矩阵写入 CSV（行=样本/观测，列=特征），无表头。
        ''' </summary>
        Public Sub SaveMatrix(key As String, matrix As Double(,))
            Dim n = matrix.GetLength(0)
            Dim m = matrix.GetLength(1)
            Dim lines(n - 1) As String

            For i As Integer = 0 To n - 1
                Dim parts(m - 1) As String
                For j As Integer = 0 To m - 1
                    parts(j) = matrix(i, j).ToString("G17", Globalization.CultureInfo.InvariantCulture)
                Next
                lines(i) = String.Join(",", parts)
            Next

            Call File.WriteAllLines(Path(key), lines, Text.Encoding.UTF8)
        End Sub

        ''' <summary>
        ''' 从 CSV 读取 [n × m] 的 Double 矩阵。
        ''' </summary>
        Public Function LoadMatrix(key As String) As Double(,)
            Dim lines = File.ReadAllLines(Path(key), Text.Encoding.UTF8)
            Dim n = lines.Length
            Dim m = lines(0).Split(","c).Length
            Dim matrix(n - 1, m - 1) As Double

            For i As Integer = 0 To n - 1
                Dim parts = lines(i).Split(","c)
                For j As Integer = 0 To m - 1
                    matrix(i, j) = Val(parts(j))
                Next
            Next

            Return matrix
        End Function

        ''' <summary>
        ''' 将 [n] 的 Double 向量以单列 CSV 形式缓存。
        ''' </summary>
        Public Sub SaveVector(key As String, vector As Double())
            Dim m(vector.Length - 1, 0) As Double
            For i As Integer = 0 To vector.Length - 1
                m(i, 0) = vector(i)
            Next
            Call SaveMatrix(key, m)
        End Sub

        Public Function LoadVector(key As String) As Double()
            Dim m = LoadMatrix(key)
            Dim v(m.GetLength(0) - 1) As Double
            For i As Integer = 0 To v.Length - 1
                v(i) = m(i, 0)
            Next
            Return v
        End Function

        ''' <summary>
        ''' 将 [n] 的 Integer 向量以单列 CSV 形式缓存。
        ''' </summary>
        Public Sub SaveIntVector(key As String, vector As Integer())
            Dim lines(vector.Length - 1) As String
            For i As Integer = 0 To vector.Length - 1
                lines(i) = vector(i).ToString
            Next
            Call File.WriteAllLines(Path(key), lines, Text.Encoding.UTF8)
        End Sub

        Public Function LoadIntVector(key As String) As Integer()
            Dim lines = File.ReadAllLines(Path(key), Text.Encoding.UTF8)
            Dim v(lines.Length - 1) As Integer
            For i As Integer = 0 To lines.Length - 1
                v(i) = CInt(Val(lines(i)))
            Next
            Return v
        End Function

        ''' <summary>
        ''' 将 [n] 的 String 标签（如样本名、基因名）以每行一个的 CSV 形式缓存。
        ''' </summary>
        Public Sub SaveLabels(key As String, labels As String())
            Call File.WriteAllLines(Path(key), labels, Text.Encoding.UTF8)
        End Sub

        Public Function LoadLabels(key As String) As String()
            Return File.ReadAllLines(Path(key), Text.Encoding.UTF8)
        End Function

        ' ===================== JSON 图/分群类缓存 =====================

        ''' <summary>
        ''' 将任意可序列化对象以 JSON 形式缓存（sciBASIC# DataContractJsonSerializer）。
        ''' </summary>
        Public Sub SaveJson(key As String, obj As Object)
            Call File.WriteAllText(Path(key), obj.GetJson, Text.Encoding.UTF8)
        End Sub

        ''' <summary>
        ''' 从 JSON 缓存反序列化对象。
        ''' </summary>
        Public Function LoadJson(Of T As New)(key As String) As T
            Return LoadJSON(Of T)(IO.File.ReadAllText(Path(key)))
        End Function

        ' ===================== 轻量图缓存（自定义边表） =====================

        ''' <summary>
        ''' 将轻量图数据结构以 JSON 形式缓存。
        ''' </summary>
        Public Sub SaveGraph(key As String, graph As GraphData)
            Call SaveJson(key, graph)
        End Sub

        Public Function LoadGraph(key As String) As GraphData
            Return LoadJson(Of GraphData)(key)
        End Function
    End Class
End Namespace
