Imports Erica.Analysis.SingleCell.Expression.MachineLearning.Diffusion

#Region "演示模块 - Diffusion Demo"

''' <summary>
''' Diffusion模型细胞状态预测演示
''' 
''' 完整流程:
'''   1. 生成模拟细胞状态数据
'''   2. 数据预处理（归一化、标准化）
'''   3. 创建条件扩散模型
'''   4. 训练模型
'''   5. 输入条件预测细胞状态
'''   6. 评估预测结果
''' </summary>
Public Module DiffusionDemo

    ''' <summary>
    ''' 运行完整演示
    ''' </summary>
    Public Sub RunDemo()
        Console.WriteLine()
        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine("  Diffusion模型 - 细胞状态预测演示")
        Console.WriteLine("  Conditional Diffusion Model for Cell State Prediction")
        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine()

        ' ===== 步骤1: 生成模拟数据 =====
        Console.WriteLine("--- 步骤1: 生成模拟细胞状态数据 ---")
        Console.WriteLine()
        Dim generator As New SyntheticCellDataGenerator(seed:=42) With {
            .NumCells = 800,
            .NumGenes = 20,
            .NoiseLevel = 0.15,
            .UseMixedPatterns = True
        }
        Dim rawData = generator.Generate()
        Console.WriteLine($"  细胞数 (Cells):    {rawData.data.GetLength(0)}")
        Console.WriteLine($"  基因数 (Genes):    {rawData.data.GetLength(1)}")
        Console.WriteLine($"  噪声水平 (Noise):  {generator.NoiseLevel}")
        Console.WriteLine($"  条件范围 (Range):  [{rawData.conditions.Min():F3}, {rawData.conditions.Max():F3}]")
        Console.WriteLine()

        ' 打印部分基因信息
        Console.WriteLine("  基因表达模式示例:")
        Dim geneInfo = generator.GetGeneInfo()
        For i As Integer = 0 To Math.Min(4, geneInfo.Count - 1)
            Dim g = geneInfo(i)
            Console.WriteLine($"    Gene {g.geneIdx,2}: base={g.baseExpr:F2}, amp={g.amplitude:F2}, " &
                              $"freq={g.frequency:F2}, pattern={g.patternType}")
        Next
        Console.WriteLine()

        ' ===== 步骤2: 数据预处理 =====
        Console.WriteLine("--- 步骤2: 数据预处理 ---")
        Console.WriteLine()
        Dim preprocessor As New DiffusionDataPreprocessor()
        Dim normalizedData = preprocessor.NormalizeAndLog(rawData.data)
        Dim standardizedData = preprocessor.StandardizeGenes(normalizedData)
        Dim normalizedConditions = preprocessor.NormalizeConditions(rawData.conditions)
        Console.WriteLine($"  归一化目标总和: {preprocessor.TargetSum}")
        Console.WriteLine($"  对数变换: log1p")
        Console.WriteLine($"  Z-score标准化: 基因维度")
        Console.WriteLine($"  条件归一化范围: [{preprocessor.MinCondition:F3}, {preprocessor.MaxCondition:F3}] -> [0, 1]")
        Console.WriteLine()

        ' ===== 步骤3: 创建模型 =====
        Console.WriteLine("--- 步骤3: 创建条件扩散模型 ---")
        Console.WriteLine()
        Dim numGenes As Integer = standardizedData.GetLength(1)
        Dim model As New DiffusionModel(
            inputDim:=numGenes,
            conditionDim:=1,
            numTimesteps:=100,
            timeEmbedDim:=32,
            hiddenDim:=256,
            scheduleType:="linear",
            seed:=42)
        Console.WriteLine($"  输入维度 (Input):     {numGenes}")
        Console.WriteLine($"  条件维度 (Cond):      {model.ConditionDim}")
        Console.WriteLine($"  扩散步数 (Steps T):   {model.NumTimesteps}")
        Console.WriteLine($"  噪声调度 (Schedule):  {model.Scheduler.ScheduleType}")
        Console.WriteLine($"  隐藏维度 (Hidden):    {model.Network.HiddenDim}")
        Console.WriteLine($"  时间嵌入 (TimeEmb):   {model.Network.TimeEmbedDim}")
        Console.WriteLine($"  网络层数 (Layers):    4 (3 hidden + 1 output)")
        Console.WriteLine()

        ' ===== 步骤4: 训练模型 =====
        Console.WriteLine("--- 步骤4: 训练扩散模型 ---")
        Console.WriteLine()
        Dim trainer As New DiffusionTrainer(
            model,
            batchSize:=64,
            epochs:=50,
            learningRate:=0.002)
        trainer.Train(standardizedData, normalizedConditions, verbose:=True)

        ' ===== 步骤5: 预测细胞状态 =====
        Console.WriteLine("--- 步骤5: 输入条件预测细胞状态 ---")
        Console.WriteLine()
        Dim predictor As New CellStatePredictor(model, preprocessor)

        ' 定义预测条件（归一化后的时间点）
        Dim targetConditions = {0.1, 0.3, 0.5, 0.7, 0.9}
        Console.WriteLine("  对以下条件进行预测（归一化时间点）:")
        For Each c In targetConditions
            Console.Write($"  c={c:F2}  ")
        Next
        Console.WriteLine()
        Console.WriteLine()

        ' 生成预测并显示统计信息
        Console.WriteLine("  预测结果（原始空间，每个条件生成5个样本的均值）:")
        Console.WriteLine()

        Dim numSamplesPerCond As Integer = 5
        For Each c In targetConditions
            Dim samples = predictor.PredictMultiple(c, numSamplesPerCond)
            Dim meanExpr(numGenes - 1) As Double
            For j As Integer = 0 To numGenes - 1
                Dim sum As Double = 0.0
                For i As Integer = 0 To numSamplesPerCond - 1
                    sum += samples(i, j)
                Next
                meanExpr(j) = sum / numSamplesPerCond
            Next

            ' 反归一化条件用于显示
            Dim origCond = preprocessor.DenormalizeCondition(c)
            Console.Write($"  c={c:F2} (orig={origCond:F3}): [")
            For j As Integer = 0 To Math.Min(5, numGenes - 1)
                Console.Write($"{meanExpr(j),7:F2}")
            Next
            Console.WriteLine("  ... ]")
        Next
        Console.WriteLine()

        ' ===== 步骤6: 评估预测质量 =====
        Console.WriteLine("--- 步骤6: 评估预测质量 ---")
        Console.WriteLine()
        Console.WriteLine("  对比预测均值与真实表达值（前5个基因）:")
        Console.WriteLine()

        ' 生成真实表达值
        Dim trueExpr = generator.GenerateTrueExpression(targetConditions)

        Console.WriteLine($"  {"Cond",8} {"Gene",6} {"Predicted",12} {"True",12} {"Error",12}")
        Console.WriteLine($"  {StrDup(56, "-")}")
        For ci As Integer = 0 To targetConditions.Length - 1
            ' 预测均值
            Dim predSamples = predictor.PredictMultiple(targetConditions(ci), 10)
            For j As Integer = 0 To 4
                Dim predMean As Double = 0.0
                For i As Integer = 0 To 9
                    predMean += predSamples(i, j)
                Next
                predMean /= 10.0

                Dim trueVal As Double = trueExpr(ci, j)
                Dim err As Double = predMean - trueVal
                Console.WriteLine($"  {targetConditions(ci),8:F2} {j,6} {predMean,12:F3} {trueVal,12:F3} {err,12:F3}")
            Next
            Console.WriteLine()
        Next

        ' ===== 保存模型 =====
        Console.WriteLine("--- 保存模型 ---")
        Dim modelPath As String = "/home/z/my-project/download/diffusion_cell_state_model.txt"
        model.Save(modelPath)
        Console.WriteLine($"  模型已保存到: {modelPath}")
        Console.WriteLine()

        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine("  演示完成！")
        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine()
    End Sub

    ''' <summary>
    ''' 快速演示（较少训练轮数，用于快速验证）
    ''' </summary>
    Public Sub RunQuickDemo()
        Console.WriteLine()
        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine("  Diffusion模型 - 快速演示")
        Console.WriteLine("=" & StrDup(60, "="))
        Console.WriteLine()

        ' 生成数据
        Dim generator As New SyntheticCellDataGenerator(seed:=42) With {
            .NumCells = 300,
            .NumGenes = 10,
            .NoiseLevel = 0.1
        }
        Dim rawData = generator.Generate()
        Console.WriteLine($"  生成 {rawData.data.GetLength(0)} 个细胞, {rawData.data.GetLength(1)} 个基因")

        ' 预处理
        Dim preprocessor As New DiffusionDataPreprocessor()
        Dim normalizedData = preprocessor.NormalizeAndLog(rawData.data)
        Dim standardizedData = preprocessor.StandardizeGenes(normalizedData)
        Dim normalizedConditions = preprocessor.NormalizeConditions(rawData.conditions)

        ' 创建模型
        Dim numGenes As Integer = standardizedData.GetLength(1)
        Dim model As New DiffusionModel(numGenes, 1, numTimesteps:=50, timeEmbedDim:=16, hiddenDim:=128, seed:=42)
        Console.WriteLine($"  模型: T={model.NumTimesteps}, hidden={model.Network.HiddenDim}")

        ' 训练
        Dim trainer As New DiffusionTrainer(model, batchSize:=32, epochs:=20, learningRate:=0.003)
        trainer.Train(standardizedData, normalizedConditions, verbose:=True)

        ' 预测
        Dim predictor As New CellStatePredictor(model, preprocessor)
        Console.WriteLine("  预测结果:")
        For Each c In {0.2, 0.5, 0.8}
            Dim result = predictor.PredictMultiple(c, 1)
            Console.Write($"    c={c:F2}: [")
            For j As Integer = 0 To Math.Min(4, numGenes - 1)
                Console.Write($"{result(0, j),7:F2}")
            Next
            Console.WriteLine(" ...]")
        Next

        Console.WriteLine()
        Console.WriteLine("  快速演示完成！")
        Console.WriteLine()
    End Sub

End Module

#End Region
