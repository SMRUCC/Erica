Imports Erica.Analysis.SingleCell.Expression.MachineLearning.CVAE
Imports std = System.Math

''' <summary>
''' CVAE演示模块
''' 生成合成单细胞数据并演示完整的训练和插值流程
''' </summary>
Public Module CVAEDemo

    ''' <summary>
    ''' 运行完整演示
    ''' </summary>
    Public Sub RunDemo()
        Console.WriteLine("="c, 70)
        Console.WriteLine("CVAE单细胞转录组时间序列插值演示")
        Console.WriteLine("="c, 70)
        Console.WriteLine()

        ' ============ 1. 生成合成数据 ============
        Console.WriteLine("[1/5] 生成合成单细胞数据...")
        Dim numTimePoints = 12  ' 12个时间点（0-11小时）
        Dim cellsPerTime = 80   ' 每个时间点80个细胞
        Dim numGenes = 100      ' 100个基因
        Dim numCells = numTimePoints * cellsPerTime

        Dim rawData = New Double(numCells - 1, numGenes - 1) {}
        Dim timeLabels = New Double(numCells - 1) {}

        Dim rand As New Random(42)
        For t = 0 To numTimePoints - 1
            For c = 0 To cellsPerTime - 1
                Dim cellIdx = t * cellsPerTime + c
                timeLabels(cellIdx) = t  ' 时间标签：0, 1, 2, ..., 11小时

                ' 基因表达随时间变化（正弦波 + 噪声）
                For g = 0 To numGenes - 1
                    Dim phase = g * 0.3  ' 不同基因有不同的相位
                    Dim amplitude = 5.0 + rand.NextDouble() * 10.0
                    Dim baseExpr = amplitude * (1 + std.Sin(t * std.PI / 6 + phase))
                    Dim noise = rand.NextDouble() * 2.0
                    rawData(cellIdx, g) = std.Max(0, baseExpr + noise)
                Next
            Next
        Next

        Console.WriteLine($"  - 时间点数: {numTimePoints} (0-{numTimePoints - 1}小时)")
        Console.WriteLine($"  - 每个时间点细胞数: {cellsPerTime}")
        Console.WriteLine($"  - 基因数: {numGenes}")
        Console.WriteLine($"  - 总细胞数: {numCells}")
        Console.WriteLine()

        ' ============ 2. 数据预处理 ============
        Console.WriteLine("[2/5] 数据预处理...")
        Dim preprocessor As New DataPreprocessor()

        ' 对数归一化
        Dim normalizedData = preprocessor.NormalizeAndLog(rawData)
        Console.WriteLine("  - 对数归一化完成")

        ' 选择高变基因
        Dim numHVG = std.Min(50, numGenes)
        Dim selectedData = preprocessor.SelectHVG(normalizedData, numHVG)
        Console.WriteLine($"  - 选择高变基因: {numHVG}/{numGenes}")

        ' 基因标准化
        Dim standardizedData = preprocessor.StandardizeGenes(selectedData)
        Console.WriteLine("  - 基因Z-score标准化完成")

        ' 归一化时间标签
        Dim normTimeLabels = preprocessor.NormalizeTimeLabels(timeLabels)
        Console.WriteLine($"  - 时间标签归一化: [{normTimeLabels.Min():F3}, {normTimeLabels.Max():F3}]")
        Console.WriteLine()

        ' ============ 3. 创建并训练CVAE ============
        Console.WriteLine("[3/5] 创建并训练CVAE模型...")
        Dim cvae As New CVAE(
            inputDim:=numHVG,
            latentDim:=16,
            conditionDim:=1,
            seed:=42)

        Dim trainer As New CVAETrainer(
            model:=cvae,
            batchSize:=64,
            epochs:=50,
            learningRate:=0.005,
            beta:=0.5)

        trainer.Train(standardizedData, normTimeLabels, verbose:=True)
        Console.WriteLine()

        ' ============ 4. 评估重建质量 ============
        Console.WriteLine("[4/7] 评估CVAE重建质量...")
        Dim evalResult = trainer.EvaluateReconstruction(standardizedData, normTimeLabels)
        Console.WriteLine($"  - 平均MSE: {evalResult.meanMSE:F6}")
        Console.WriteLine($"  - 整体R²: {evalResult.meanR2:F4}")
        Console.WriteLine()

        ' ============ 5. 时间序列插值（双向合并策略） ============
        Console.WriteLine("[5/7] 执行时间序列插值 - 策略3（双向合并，15分钟分辨率）...")
        Dim interpolator As New TimeSeriesInterpolator(cvae, preprocessor)
        Dim result = interpolator.Interpolate(
            data:=standardizedData,
            timeLabels:=normTimeLabels,
            intervalHours:=0.25,  ' 15分钟
            strategy:=3)  ' 双向合并策略

        Console.WriteLine($"  - 原始时间点: {numTimePoints}个")
        Console.WriteLine($"  - 插值后时间点: {result.UniqueTimePoints.Length}个")
        Console.WriteLine($"  - 原始细胞数: {numCells}")
        Console.WriteLine($"  - 插值后细胞数: {result.Data.GetLength(0)}")
        Console.WriteLine()

        ' ============ 6. 潜在空间线性插值策略对比 ============
        Console.WriteLine("[6/7] 执行时间序列插值 - 策略4（潜在空间线性插值）...")
        Dim interpolator2 As New TimeSeriesInterpolator(cvae, preprocessor) With {
            .Strategy = 4,
            .NumSamplesPerTime = 50
        }
        Dim result2 = interpolator2.Interpolate(
            data:=standardizedData,
            timeLabels:=normTimeLabels,
            intervalHours:=0.25,
            strategy:=4)

        Console.WriteLine($"  - 插值后时间点: {result2.UniqueTimePoints.Length}个")
        Console.WriteLine($"  - 插值后细胞数: {result2.Data.GetLength(0)}")
        Console.WriteLine()

        ' ============ 7. 输出插值结果摘要 ============
        Console.WriteLine("[7/7] 插值结果摘要（策略3）:")
        Console.WriteLine()
        Console.WriteLine($"  {"时间点(h)",10} {"归一化时间",12} {"细胞数",8}")
        Console.WriteLine($"  {"-"c,10} {"-"c,12} {"-"c,8}")

        For i = 0 To result.UniqueTimePoints.Length - 1
            Dim normTime = result.UniqueTimePoints(i)
            Dim origTime = preprocessor.DenormalizeTimeLabel(normTime)
            Dim cellCount = result.CellsPerTimePoint(normTime)
            Console.WriteLine($"  {origTime,10:F2} {normTime,12:F4} {cellCount,8}")
        Next

        Console.WriteLine()
        Console.WriteLine("="c, 70)
        Console.WriteLine("演示完成！")
        Console.WriteLine("="c, 70)
    End Sub

End Module


