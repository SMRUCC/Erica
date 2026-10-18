
Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.Json
Imports SMRUCC.genomics.Analysis.BNLearn.Core

''' <summary>预处理结果。</summary>
Public Class DBNPreprocessOutput
    ''' <summary>DBN 时间序列（基因 × Kbin，TimePoints = bin 平均伪时间），可直接喂 GeneRegulatoryNetwork.ToTimeSeries。</summary>
    Public Property timeSeries As GeneExpressionData
    ''' <summary>每个 bin 内聚合的基因伪速度矩阵（基因 × bin），供 DBN 因果方向先验。</summary>
    Public Property binVelocity As Double(,)
    ''' <summary>每基因沿轨迹的整体趋势方向（sign(mean bin velocity)）。</summary>
    Public Property trendSign As Double()
    ''' <summary>筛选后保留的基因名（与 timeSeries.Matrix 行序一致）。</summary>
    Public Property selectedGenes As String()
    ''' <summary>被剔除的基因名。</summary>
    Public Property geneExcluded As String()
    ''' <summary>每基因的速度幅度统计（与 geneNames 顺序一致）。</summary>
    Public Property speedStat As Double()
    ''' <summary>bin 标签（"bin_1".."bin_K"）。</summary>
    Public Property binLabels As String()
    ''' <summary>每个 bin 的伪时间点（平均伪时间）。</summary>
    Public Property binTimePoints As Double()
    ''' <summary>原始样本名（与输入一致）。</summary>
    Public Property sampleNames As String()
    ''' <summary>原始基因名（与输入一致）。</summary>
    Public Property geneNames As String()

    Public Overrides Function ToString() As String
        Dim k = If(binTimePoints Is Nothing, 0, binTimePoints.Length)
        Return $"DBNPreprocessOutput(genes={selectedGenes.Length}/{geneNames.Length}, bins={k})"
    End Function

    ''' <summary>zip 包条目名与格式常量（Save/Load 两端唯一事实源）。</summary>
    Private Const SchemaName As String = "DBNPreprocessOutput"
    Private Const FormatVersion As Integer = 1
    Private Const ManifestEntry As String = "manifest.json"
    Private Const TimeSeriesMatrixEntry As String = "timeseries.matrix.bin"
    Private Const TimeSeriesGenesEntry As String = "timeseries.genes.json"
    Private Const TimeSeriesSamplesEntry As String = "timeseries.samples.json"
    Private Const TimeSeriesTimePointsEntry As String = "timeseries.timepoints.bin"
    Private Const BinVelocityEntry As String = "binVelocity.bin"
    Private Const TrendSignEntry As String = "trendSign.bin"
    Private Const SpeedStatEntry As String = "speedStat.bin"
    Private Const BinTimePointsEntry As String = "binTimePoints.bin"
    Private Const BinLabelsEntry As String = "binLabels.json"
    Private Const SelectedGenesEntry As String = "selectedGenes.json"
    Private Const GeneExcludedEntry As String = "geneExcluded.json"
    Private Const SampleNamesEntry As String = "sampleNames.json"
    Private Const GeneNamesEntry As String = "geneNames.json"

    ''' <summary>
    ''' 将整个预处理结果保存为一个 zip 压缩包。
    ''' 格式约定：<c>manifest.json</c> 记录 schema/版本/维度与条目存在性（自描述，便于校验与诊断）；
    ''' 矩阵与向量以本机字节序（x86/x64 为小端）的原始 Double 二进制存放，保证精度无损往返；
    ''' 基因名/样本名等字符串以 UTF-8 JSON 数组存放。
    ''' 底层流由调用方管理，方法返回时流仍然打开（不会被 Dispose）。
    ''' </summary>
    ''' <param name="zip">可写流（如 FileStream、MemoryStream），内容将被覆盖为 zip 包。</param>
    Public Sub Save(zip As Stream)
        If zip Is Nothing Then Throw New ArgumentNullException(NameOf(zip))

        Using archive As New ZipArchive(zip, ZipArchiveMode.Create, leaveOpen:=True)
            Dim ts = Me.timeSeries
            Dim hasTs As Boolean = ts IsNot Nothing AndAlso ts.Matrix IsNot Nothing
            Dim nGene As Integer = If(hasTs, ts.Matrix.GetLength(0), 0)
            Dim nSample As Integer = If(hasTs, ts.Matrix.GetLength(1), 0)
            Dim nBins As Integer = If(Me.binTimePoints Is Nothing, nSample, Me.binTimePoints.Length)

            ' 1. manifest 自描述清单
            Dim manifest As New SaveManifest With {
                .schema = SchemaName,
                .formatVersion = FormatVersion,
                .created = DateTime.Now.ToString("O"),
                .hasTimeSeries = hasTs,
                .timeSeriesGenes = nGene,
                .timeSeriesSamples = nSample,
                .bins = nBins,
                .hasBinVelocity = Me.binVelocity IsNot Nothing,
                .hasTrendSign = Me.trendSign IsNot Nothing,
                .hasSpeedStat = Me.speedStat IsNot Nothing,
                .hasGeneExcluded = Me.geneExcluded IsNot Nothing
            }
            WriteText(archive, ManifestEntry, JsonSerializer.Serialize(manifest))

            ' 2. DBN 时间序列（基因 × bin）
            If hasTs Then
                WriteBytes(archive.CreateEntry(TimeSeriesMatrixEntry), DoublesToBytes(FlattenMatrix(ts.Matrix)))
                WriteText(archive, TimeSeriesGenesEntry, JsonSerializer.Serialize(ts.GeneNames))
                WriteText(archive, TimeSeriesSamplesEntry, JsonSerializer.Serialize(ts.SampleNames))
                If ts.TimePoints IsNot Nothing Then
                    WriteBytes(archive.CreateEntry(TimeSeriesTimePointsEntry), DoublesToBytes(ts.TimePoints))
                End If
            End If

            ' 3. bin 伪速度矩阵（基因 × bin）
            If Me.binVelocity IsNot Nothing Then
                WriteBytes(archive.CreateEntry(BinVelocityEntry), DoublesToBytes(FlattenMatrix(Me.binVelocity)))
            End If

            ' 4. 逐基因向量（trendSign / speedStat 与原始基因表同序，binTimePoints 与 bin 同序）
            If Me.trendSign IsNot Nothing Then
                WriteBytes(archive.CreateEntry(TrendSignEntry), DoublesToBytes(Me.trendSign))
            End If
            If Me.speedStat IsNot Nothing Then
                WriteBytes(archive.CreateEntry(SpeedStatEntry), DoublesToBytes(Me.speedStat))
            End If
            If Me.binTimePoints IsNot Nothing Then
                WriteBytes(archive.CreateEntry(BinTimePointsEntry), DoublesToBytes(Me.binTimePoints))
            End If

            ' 5. 字符串清单
            WriteText(archive, BinLabelsEntry, JsonSerializer.Serialize(Me.binLabels))
            WriteText(archive, SelectedGenesEntry, JsonSerializer.Serialize(Me.selectedGenes))
            If Me.geneExcluded IsNot Nothing Then
                WriteText(archive, GeneExcludedEntry, JsonSerializer.Serialize(Me.geneExcluded))
            End If
            WriteText(archive, SampleNamesEntry, JsonSerializer.Serialize(Me.sampleNames))
            WriteText(archive, GeneNamesEntry, JsonSerializer.Serialize(Me.geneNames))
        End Using
    End Sub

    ''' <summary>
    ''' 从 zip 压缩包还原 <see cref="DBNPreprocessOutput"/>（与 <see cref="Save"/> 对称）。
    ''' 缺少的可选条目还原为 Nothing，并用 timeSeries 自带元数据回填 selectedGenes / binLabels；
    ''' manifest 缺失、schema 不匹配或版本过高时抛出 <see cref="InvalidDataException"/>。
    ''' 底层流由调用方管理，方法返回时流仍然打开（不会被 Dispose）。
    ''' </summary>
    ''' <param name="zip">可读流（如 FileStream、MemoryStream），内容为本方法写出的 zip 包。</param>
    Public Shared Function Load(zip As Stream) As DBNPreprocessOutput
        If zip Is Nothing Then Throw New ArgumentNullException(NameOf(zip))

        Using archive As New ZipArchive(zip, ZipArchiveMode.Read, leaveOpen:=True)
            Dim manifestText = ReadText(archive, ManifestEntry)
            If String.IsNullOrEmpty(manifestText) Then
                Throw New InvalidDataException($"无效的 {SchemaName} zip 包：缺少条目 '{ManifestEntry}'")
            End If

            Dim manifest = JsonSerializer.Deserialize(Of SaveManifest)(manifestText)
            Dim manifestSchema = If(manifest Is Nothing, Nothing, manifest.schema)
            If manifestSchema Is Nothing OrElse Not String.Equals(manifestSchema, SchemaName, StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidDataException($"无效的 {SchemaName} zip 包：manifest.schema = '{manifestSchema}'")
            End If
            If manifest.formatVersion > FormatVersion Then
                Throw New InvalidDataException($"不支持的 {SchemaName} zip 包版本 {manifest.formatVersion}，当前最高支持 {FormatVersion}")
            End If

            Dim result As New DBNPreprocessOutput()
            Dim nGene = If(manifest.timeSeriesGenes > 0, manifest.timeSeriesGenes, 0)
            Dim nSample = If(manifest.timeSeriesSamples > 0, manifest.timeSeriesSamples, 0)
            Dim nBins = If(manifest.bins > 0, manifest.bins, nSample)

            ' timeSeries（基因 × bin 表达矩阵 + 基因名/样本名/时间点）
            If manifest.hasTimeSeries AndAlso nGene > 0 AndAlso nSample > 0 Then
                Dim tsFlat = BytesToDoubles(ReadBytes(archive, TimeSeriesMatrixEntry))
                If tsFlat Is Nothing OrElse tsFlat.Length <> nGene * nSample Then
                    Throw New InvalidDataException(
                        $"zip 包内时间序列矩阵损坏：期望 {nGene * nSample} 个元素，实际 {If(tsFlat Is Nothing, 0, tsFlat.Length)}")
                End If

                Dim ged As New GeneExpressionData With {
                    .GeneNames = ReadStrings(archive, TimeSeriesGenesEntry),
                    .SampleNames = ReadStrings(archive, TimeSeriesSamplesEntry),
                    .Matrix = InflateMatrix(tsFlat, nGene, nSample),
                    .TimePoints = BytesToDoubles(ReadBytes(archive, TimeSeriesTimePointsEntry))
                }
                result.timeSeries = ged
            End If

            ' bin 伪速度矩阵（维度不符时按损坏处理，直接跳过置 Nothing）
            If manifest.hasBinVelocity AndAlso nGene > 0 AndAlso nBins > 0 Then
                Dim velFlat = BytesToDoubles(ReadBytes(archive, BinVelocityEntry))
                If velFlat IsNot Nothing AndAlso velFlat.Length = nGene * nBins Then
                    result.binVelocity = InflateMatrix(velFlat, nGene, nBins)
                End If
            End If

            result.trendSign = BytesToDoubles(ReadBytes(archive, TrendSignEntry))
            result.speedStat = BytesToDoubles(ReadBytes(archive, SpeedStatEntry))
            result.binTimePoints = BytesToDoubles(ReadBytes(archive, BinTimePointsEntry))
            result.binLabels = ReadStrings(archive, BinLabelsEntry)
            result.selectedGenes = ReadStrings(archive, SelectedGenesEntry)
            result.geneExcluded = ReadStrings(archive, GeneExcludedEntry)
            result.sampleNames = ReadStrings(archive, SampleNamesEntry)
            result.geneNames = ReadStrings(archive, GeneNamesEntry)

            ' 兼容回填：条目缺失时用 timeSeries 自带的元数据
            If result.selectedGenes Is Nothing AndAlso result.timeSeries IsNot Nothing Then
                result.selectedGenes = result.timeSeries.GeneNames
            End If
            If result.binLabels Is Nothing AndAlso result.timeSeries IsNot Nothing Then
                result.binLabels = result.timeSeries.SampleNames
            End If

            Return result
        End Using
    End Function

    ' ==================== 内部实现 ====================

    ''' <summary>把 [n × m] 矩阵按行主序展平为一维数组。</summary>
    Private Shared Function FlattenMatrix(matrix As Double(,)) As Double()
        Dim n = matrix.GetLength(0)
        Dim m = matrix.GetLength(1)
        Dim flat(matrix.Length - 1) As Double
        Dim k As Integer = 0

        For i As Integer = 0 To n - 1
            For j As Integer = 0 To m - 1
                flat(k) = matrix(i, j)
                k += 1
            Next
        Next

        Return flat
    End Function

    ''' <summary>把行主序一维数组还原为 [n × m] 矩阵。</summary>
    Private Shared Function InflateMatrix(flat As Double(), n As Integer, m As Integer) As Double(,)
        Dim matrix(n - 1, m - 1) As Double
        Dim k As Integer = 0

        For i As Integer = 0 To n - 1
            For j As Integer = 0 To m - 1
                matrix(i, j) = flat(k)
                k += 1
            Next
        Next

        Return matrix
    End Function

    ''' <summary>Double() → 本机字节序原始字节（避免文本往返损失精度）。</summary>
    Private Shared Function DoublesToBytes(values As Double()) As Byte()
        If values Is Nothing Then Return Nothing
        Return MemoryMarshal.AsBytes(values.AsSpan()).ToArray()
    End Function

    ''' <summary>本机字节序原始字节 → Double()；长度非 8 的倍数视为数据损坏。</summary>
    Private Shared Function BytesToDoubles(data As Byte()) As Double()
        If data Is Nothing OrElse data.Length = 0 Then Return Nothing
        If data.Length Mod 8 <> 0 Then
            Throw New InvalidDataException($"二进制 Double 数据长度 {data.Length} 不是 8 的倍数，数据已损坏")
        End If
        Return MemoryMarshal.Cast(Of Byte, Double)(data).ToArray()
    End Function

    ''' <summary>写 UTF-8（无 BOM）文本条目。</summary>
    Private Shared Sub WriteText(archive As ZipArchive, entryName As String, text As String)
        Dim entry = archive.CreateEntry(entryName)

        Using s As Stream = entry.Open()
            Dim bytes = Encoding.UTF8.GetBytes(text)
            s.Write(bytes, 0, bytes.Length)
        End Using
    End Sub

    Private Shared Sub WriteBytes(entry As ZipArchiveEntry, data As Byte())
        Using s As Stream = entry.Open()
            s.Write(data, 0, data.Length)
        End Using
    End Sub

    Private Shared Function ReadText(archive As ZipArchive, entryName As String) As String
        Dim data = ReadBytes(archive, entryName)
        If data Is Nothing Then Return Nothing
        Return Encoding.UTF8.GetString(data)
    End Function

    ''' <summary>读取条目全部字节；条目不存在返回 Nothing。</summary>
    Private Shared Function ReadBytes(archive As ZipArchive, entryName As String) As Byte()
        Dim entry = archive.GetEntry(entryName)
        If entry Is Nothing Then Return Nothing

        Using s As Stream = entry.Open()
            Using ms As New MemoryStream()
                s.CopyTo(ms)
                Return ms.ToArray()
            End Using
        End Using
    End Function

    Private Shared Function ReadStrings(archive As ZipArchive, entryName As String) As String()
        Dim text = ReadText(archive, entryName)
        If String.IsNullOrEmpty(text) Then Return Nothing
        Return JsonSerializer.Deserialize(Of String())(text)
    End Function
End Class

''' <summary>zip 包清单（自描述格式，便于 Load 校验与诊断）。</summary>
Friend Class SaveManifest
    Public Property schema As String
    Public Property formatVersion As Integer
    Public Property created As String
    Public Property hasTimeSeries As Boolean
    Public Property timeSeriesGenes As Integer
    Public Property timeSeriesSamples As Integer
    Public Property bins As Integer
    Public Property hasBinVelocity As Boolean
    Public Property hasTrendSign As Boolean
    Public Property hasSpeedStat As Boolean
    Public Property hasGeneExcluded As Boolean
End Class