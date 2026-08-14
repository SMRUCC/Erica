' ============================================================================
' Program.vb — SpatialDE + spatialGE 入口程序
' ----------------------------------------------------------------------------
' 演示如何使用 SpatialOmics 库的两个核心模块：
'   1. SpatialDE — 鉴定空间变异基因
'   2. spatialGE — 量化空间异质性 + 聚类
'
' 包含模拟数据生成、完整分析流程、结果输出。
' ============================================================================

Imports SpatialOmics.Math
Imports SpatialOmics.SpatialDE
Imports SpatialOmics.SpatialGE
Imports System

Namespace SpatialOmics

    Public Module Program

        Public Sub Main(args As String())
            Console.WriteLine("="c, 70)
            Console.WriteLine("  SpatialOmics: SpatialDE + spatialGE")
            Console.WriteLine("  空间转录组学差异基因分析模块")
            Console.WriteLine("="c, 70)
            Console.WriteLine()

            ' ---- 1. 生成模拟数据 ----
            Console.WriteLine("[1] 生成模拟空间转录组数据...")
            Dim nSamples As Integer = 50
            Dim nGenes As Integer = 100
            Dim nSpatialGenes As Integer = 30  ' 其中 30 个为空间变异基因

            Dim coords = GenerateGridCoordinates(nSamples)
            Dim (expression, geneNames) = GenerateSimulatedExpression(
                coords, nGenes, nSpatialGenes)

            Console.WriteLine($"    样本数: {nSamples}, 基因数: {nGenes}")
            Console.WriteLine($"    空间变异基因: {nSpatialGenes} / {nGenes}")
            Console.WriteLine()

            ' ---- 2. SpatialDE 分析 ----
            Console.WriteLine("[2] 运行 SpatialDE 分析...")
            Dim spatialDE As New SpatialDEModel(coords)
            Dim deResults = spatialDE.Analyze(expression, geneNames)

            ' 输出 Top-10 结果
            Console.WriteLine("    Top-10 空间变异基因:")
            Console.WriteLine($"    {"Gene",-12} {"FSV",-8:F4} {"LRStat",-10:F4} {"P-value",-12:E4} {"Q-value",-12:E4} {"Sig"}")
            Console.WriteLine("    " & "-"c, 65)

            Dim topResults = deResults.OrderBy(Function(r) r.PValue).Take(10)
            For Each r In topResults
                Dim sig = If(r.QValue < 0.05, "*", "")
                Console.WriteLine($"    {r.GeneName,-12} {r.FSV,-8:F4} {r.LRStat,-10:F4} {r.PValue,-12:E4} {r.QValue,-12:E4} {sig}")
            Next
            Console.WriteLine()

            Dim sigCount = deResults.Count(Function(r) r.QValue < 0.05)
            Console.WriteLine($"    显著基因数 (q < 0.05): {sigCount} / {nGenes}")
            Console.WriteLine()

            ' ---- 3. spatialGE 分析 ----
            Console.WriteLine("[3] 运行 spatialGE 分析...")

            Dim spatialGE As New SpatialGEModel(coords)

            ' 3a. 空间自相关统计
            Console.WriteLine("    [3a] 计算空间自相关统计量...")
            Dim spStats = spatialGE.ComputeSpatialStats(expression, geneNames)

            Console.WriteLine("    Top-5 按 Moran's I:")
            Console.WriteLine($"    {"Gene",-12} {"Moran's I",-12:F4} {"Geary's C",-12:F4} {"Gi*_max",-12:F4} {"I_pval",-12:E4}")
            Console.WriteLine("    " & "-"c, 60)
            For Each s In spStats.Take(5)
                Dim maxGi = If(s.GetisOrdGiZScore?.Max(), 0.0)
                Console.WriteLine($"    {s.GeneName,-12} {s.MoransI,-12:F4} {s.GearysC,-12:F4} {maxGi,-12:F4} {s.MoransIPValue,-12:E4}")
            Next
            Console.WriteLine()

            ' 3b. STclust 聚类
            Console.WriteLine("    [3b] STclust 空间感知聚类...")
            Dim clustResult = spatialGE.RunSTclust(
                expression, geneNames,
                nClusters:=4,
                nTopGenes:=50,
                spatialWeight:=0.5)

            Console.WriteLine($"    聚类数: {clustResult.K}")
            Console.WriteLine($"    各聚类样本数: {String.Join(", ", clustResult.ClusterSizes)}")
            Console.WriteLine($"    使用的 top-N 变异基因数: {clustResult.SelectedGenes.Length}")
            Console.WriteLine()

            Console.WriteLine("="c, 70)
            Console.WriteLine("  分析完成！")
            Console.WriteLine("="c, 70)
        End Sub

        ''' <summary>
        ''' 生成规则网格坐标
        ''' </summary>
        Private Function GenerateGridCoordinates(n As Integer) As Matrix
            Dim side = CInt(Math.Ceiling(Math.Sqrt(n)))
            Dim coords As New List(Of Double())
            For i = 0 To side - 1
                For j = 0 To side - 1
                    If coords.Count >= n Then Exit For
                    coords.Add({CDbl(i), CDbl(j)})
                Next
                If coords.Count >= n Then Exit For
            Next
            ' 补齐
            While coords.Count < n
                coords.Add({CDbl(coords.Count Mod side), CDbl(coords.Count \ side)})
            End While

            Dim data(n - 1, 1) As Double
            For i = 0 To n - 1
                data(i, 0) = coords(i)(0)
                data(i, 1) = coords(i)(1)
            Next
            Return New Matrix(data)
        End Function

        ''' <summary>
        ''' 生成模拟基因表达数据
        ''' 前 nSpatial 个基因为空间变异基因（在网格上呈梯度/斑块模式）
        ''' 其余为随机噪声基因
        ''' </summary>
        Private Function GenerateSimulatedExpression(
                coords As Matrix, nGenes As Integer,
                nSpatialGenes As Integer) As (expr As Matrix, names As String())

            Dim n = coords.Rows
            Dim rng As New Random(42)

            Dim expr As New Matrix(nGenes, n)
            Dim names(nGenes - 1) As String

            ' 空间变异基因
            For g = 0 To nSpatialGenes - 1
                names(g) = $"SpatialGene_{g + 1}"
                Dim pattern = g Mod 3  ' 3 种空间模式

                For i = 0 To n - 1
                    Dim x = coords(i, 0)
                    Dim y = coords(i, 1)
                    Dim spatial As Double

                    Select Case pattern
                        Case 0 ' 水平梯度
                            spatial = x / (Math.Max(1, coords.GetColumn(0).Max()))
                        Case 1 ' 径向梯度
                            Dim cx = coords.GetColumn(0).Average()
                            Dim cy = coords.GetColumn(1).Average()
                            spatial = Math.Sqrt((x - cx) ^ 2 + (y - cy) ^ 2)
                        Case 2 ' 正弦波
                            spatial = 0.5 + 0.5 * Math.Sin(x * 0.8) * Math.Cos(y * 0.8)
                    End Select

                    ' 空间分量 + 噪声
                    Dim noise = rng.NextDouble() * 0.3
                    expr(g, i) = spatial + noise
                Next
            Next

            ' 非空间变异基因（纯噪声）
            For g = nSpatialGenes To nGenes - 1
                names(g) = $"NoiseGene_{g - nSpatialGenes + 1}"
                For i = 0 To n - 1
                    expr(g, i) = rng.NextDouble()
                Next
            Next

            Return (expr, names)
        End Function

    End Module

End Namespace
