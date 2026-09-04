' ============================================================================
' Diffusion Model Implementation for Cell State Prediction
' 基于扩散模型（Diffusion Model）的细胞状态预测算法模块
'
' 本模块基于DDPM（Denoising Diffusion Probabilistic Models）算法原理，
' 实现了条件扩散模型用于细胞状态（如基因表达谱）的生成与预测。
'
' 算法核心原理:
'   1. 前向扩散过程（Forward Diffusion）:
'      逐步向数据添加高斯噪声，经过T步后数据变为纯噪声
'      q(x_t | x_{t-1}) = N(x_t; sqrt(alpha_t) * x_{t-1}, (1-alpha_t)*I)
'      闭式解: x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1-alpha_bar_t) * epsilon
'      其中 alpha_t = 1 - beta_t, alpha_bar_t = prod(alpha_1..alpha_t)
'
'   2. 反向去噪过程（Reverse Denoising）:
'      训练神经网络预测每一步加入的噪声 epsilon_theta(x_t, t, c)
'      p_theta(x_{t-1} | x_t) = N(x_{t-1}; mu_theta, sigma_t^2 * I)
'      mu_theta = (1/sqrt(alpha_t)) * (x_t - (1-alpha_t)/sqrt(1-alpha_bar_t) * epsilon_theta)
'
'   3. 训练损失:
'      L = E_{t,x_0,epsilon} [||epsilon - epsilon_theta(sqrt(alpha_bar_t)*x_0 + sqrt(1-alpha_bar_t)*epsilon, t, c)||^2]
'
'   4. 条件生成:
'      给定条件c（如时间点、处理剂量），从纯噪声开始逐步去噪生成细胞状态
'
' 代码复用说明:
'   - 复用 Tensor.vb 中的 Tensor 类进行张量运算
'   - 复用 CVAE.vb 中的 LinearLayer, LayerNormLayer, ReLULayer, AdamOptimizer 等网络层
'   - 复用 CVAE.vb 中的数据预处理与训练器设计模式
' ============================================================================

Imports Erica.Analysis.SingleCell.Expression.MachineLearning.CVAE
Imports Microsoft.VisualBasic.MachineLearning.TensorFlow
Imports Microsoft.VisualBasic.Math
Imports std = System.Math

Namespace MachineLearning.Diffusion

    ' ===========================================================================
    ' NamespaceDoc: 基于扩散模型的细胞状态预测算法模块
    ' ===========================================================================

#Region "噪声调度器 - Noise Scheduler"

    ''' <summary>
    ''' 噪声调度器（Noise Scheduler）
    ''' 管理扩散过程中的方差调度 beta_t，以及派生量 alpha_t, alpha_bar_t
    ''' 
    ''' 支持两种调度方式:
    '''   - linear:  beta_t 从 beta_start 线性增长到 beta_end
    '''   - cosine:  基于余弦曲线的调度（Improved DDPM, Nichol &amp; Dhariwal 2021）
    '''              在后期保持更低的噪声水平，生成质量更好
    ''' </summary>
    Public Class NoiseScheduler

        ''' <summary>扩散总步数T</summary>
        Public Property NumTimesteps As Integer = 200

        ''' <summary>线性调度的起始beta值</summary>
        Public Property BetaStart As Double = 0.0001

        ''' <summary>线性调度的结束beta值</summary>
        Public Property BetaEnd As Double = 0.02

        ''' <summary>调度类型: "linear" 或 "cosine"</summary>
        Public Property ScheduleType As String = "linear"

        ' ===== 预计算的调度表（索引0对应t=0, 索引T-1对应t=T-1）=====

        ''' <summary>beta_t: 每步添加的噪声方差</summary>
        Public Betas As Double()

        ''' <summary>alpha_t = 1 - beta_t</summary>
        Public Alphas As Double()

        ''' <summary>alpha_bar_t = prod(alpha_1 .. alpha_t) 累积乘积</summary>
        Public AlphaBars As Double()

        ''' <summary>sqrt(alpha_bar_t)</summary>
        Public SqrtAlphaBars As Double()

        ''' <summary>sqrt(1 - alpha_bar_t)</summary>
        Public SqrtOneMinusAlphaBars As Double()

        ''' <summary>1 / sqrt(alpha_t)</summary>
        Public SqrtRecipAlphas As Double()

        ''' <summary>(1 - alpha_t) / sqrt(1 - alpha_bar_t)  -- 用于反向过程均值计算</summary>
        Public OneMinusAlphasOverSqrtOneMinusAlphaBars As Double()

        ''' <summary>后验方差 q(x_{t-1}|x_t,x_0) 的方差 = beta_t * (1-alpha_bar_{t-1}) / (1-alpha_bar_t)</summary>
        Public PosteriorVariances As Double()

        ''' <summary>后验方差的开方，用于采样时添加噪声</summary>
        Public PosteriorLogVarClipped As Double()

        ''' <summary>
        ''' 创建噪声调度器
        ''' </summary>
        Public Sub New(Optional numTimesteps As Integer = 200,
                       Optional betaStart As Double = 0.0001,
                       Optional betaEnd As Double = 0.02,
                       Optional scheduleType As String = "linear")
            Me.NumTimesteps = numTimesteps
            Me.BetaStart = betaStart
            Me.BetaEnd = betaEnd
            Me.ScheduleType = scheduleType
            BuildSchedule()
        End Sub

        ''' <summary>
        ''' 构建噪声调度表
        ''' </summary>
        Public Sub BuildSchedule()
            ReDim Betas(NumTimesteps - 1)
            ReDim Alphas(NumTimesteps - 1)
            ReDim AlphaBars(NumTimesteps - 1)
            ReDim SqrtAlphaBars(NumTimesteps - 1)
            ReDim SqrtOneMinusAlphaBars(NumTimesteps - 1)
            ReDim SqrtRecipAlphas(NumTimesteps - 1)
            ReDim OneMinusAlphasOverSqrtOneMinusAlphaBars(NumTimesteps - 1)
            ReDim PosteriorVariances(NumTimesteps - 1)
            ReDim PosteriorLogVarClipped(NumTimesteps - 1)

            ' Step 1: 生成 beta_t 序列
            If ScheduleType = "cosine" Then
                ' 余弦调度 (Improved DDPM)
                Dim s As Double = 0.008
                Dim steps As Integer = NumTimesteps + 1
                Dim alphaBarCalc(steps - 1) As Double
                For t As Integer = 0 To steps - 1
                    Dim f_t As Double = std.Cos((CDbl(t) / steps + s) / (1.0 + s) * std.PI / 2.0)
                    alphaBarCalc(t) = f_t * f_t
                Next
                Dim f_0 As Double = std.Cos((s / (1.0 + s)) * std.PI / 2.0)
                Dim alphaBar0 As Double = f_0 * f_0
                For t As Integer = 0 To steps - 1
                    alphaBarCalc(t) = alphaBarCalc(t) / alphaBar0
                Next
                For t As Integer = 0 To NumTimesteps - 1
                    ' beta_t = 1 - alpha_bar_{t+1} / alpha_bar_t
                    Dim b As Double = 1.0 - alphaBarCalc(t + 1) / alphaBarCalc(t)
                    Betas(t) = std.Min(0.999, b)
                Next
            Else
                ' 线性调度
                For t As Integer = 0 To NumTimesteps - 1
                    Betas(t) = BetaStart + (BetaEnd - BetaStart) * CDbl(t) / CDbl(NumTimesteps - 1)
                Next
            End If

            ' Step 2: 计算派生量
            Dim cumulativeAlpha As Double = 1.0
            For t As Integer = 0 To NumTimesteps - 1
                Alphas(t) = 1.0 - Betas(t)
                cumulativeAlpha *= Alphas(t)
                AlphaBars(t) = cumulativeAlpha
                SqrtAlphaBars(t) = std.Sqrt(std.Max(0.0000000001, AlphaBars(t)))
                SqrtOneMinusAlphaBars(t) = std.Sqrt(std.Max(0.0000000001, 1.0 - AlphaBars(t)))
                SqrtRecipAlphas(t) = std.Sqrt(1.0 / std.Max(0.0000000001, Alphas(t)))
                OneMinusAlphasOverSqrtOneMinusAlphaBars(t) = (1.0 - Alphas(t)) / std.Max(0.0000000001, std.Sqrt(1.0 - AlphaBars(t)))
            Next

            ' Step 3: 计算后验方差
            ' q(x_{t-1}|x_t,x_0) 的方差 = beta_t * (1-alpha_bar_{t-1}) / (1-alpha_bar_t)
            PosteriorVariances(0) = Betas(0)  ' t=0时使用beta_0作为近似
            PosteriorLogVarClipped(0) = std.Sqrt(std.Max(1.0E-20, PosteriorVariances(0)))
            For t As Integer = 1 To NumTimesteps - 1
                Dim pv As Double = Betas(t) * (1.0 - AlphaBars(t - 1)) / std.Max(0.0000000001, (1.0 - AlphaBars(t)))
                PosteriorVariances(t) = pv
                PosteriorLogVarClipped(t) = std.Sqrt(std.Max(1.0E-20, pv))
            Next
        End Sub

        ''' <summary>
        ''' 获取指定时间步的调度参数
        ''' </summary>
        Public Function GetParams(t As Integer) As (sqrtAlphaBar As Double, sqrtOneMinusAlphaBar As Double)
            Return (SqrtAlphaBars(t), SqrtOneMinusAlphaBars(t))
        End Function

    End Class

#End Region

#Region "时间步嵌入 - Time Step Embedding"

    ''' <summary>
    ''' 正弦时间步嵌入（Sinusoidal Time Step Embedding）
    ''' 
    ''' 类似Transformer的位置编码，将离散时间步t映射到连续高维向量，
    ''' 使网络能够区分不同的扩散步骤。这是扩散模型中处理时间步信息的标准方法。
    ''' 
    ''' 公式:
    '''   emb[2i]   = sin(t / 10000^(2i/d))
    '''   emb[2i+1] = cos(t / 10000^(2i/d))
    ''' </summary>
    Public Module TimeEmbedding

        ''' <summary>
        ''' 计算单个时间步的正弦嵌入向量
        ''' </summary>
        ''' <param name="t">时间步（整数或归一化后的浮点数）</param>
        ''' <param name="dim">嵌入维度</param>
        ''' <returns>维度为dim的嵌入向量</returns>
        Public Function SinusoidalEmbedding(t As Double, [dim] As Integer) As Double()
            Dim emb([dim] - 1) As Double
            Dim halfDim As Integer = [dim] \ 2
            For i As Integer = 0 To halfDim - 1
                Dim freq As Double = std.Exp(-std.Log(10000.0) * CDbl(i) / CDbl(halfDim))
                Dim arg As Double = t * freq
                emb(2 * i) = std.Sin(arg)
                emb(2 * i + 1) = std.Cos(arg)
            Next
            ' 处理奇数维度
            If [dim] Mod 2 = 1 Then
                Dim freq As Double = std.Exp(-std.Log(10000.0) * CDbl(halfDim) / CDbl(halfDim))
                emb([dim] - 1) = std.Sin(t * freq)
            End If
            Return emb
        End Function

        ''' <summary>
        ''' 批量计算时间步嵌入
        ''' </summary>
        ''' <param name="timeSteps">每个样本的时间步数组</param>
        ''' <param name="dim">嵌入维度</param>
        ''' <returns>(batch, dim) 的二维数组</returns>
        Public Function BatchSinusoidalEmbedding(timeSteps As Integer(), [dim] As Integer) As Double(,)
            Dim batch As Integer = timeSteps.Length
            Dim result(batch - 1, [dim] - 1) As Double
            For i As Integer = 0 To batch - 1
                Dim emb As Double() = SinusoidalEmbedding(CDbl(timeSteps(i)), [dim])
                For j As Integer = 0 To [dim] - 1
                    result(i, j) = emb(j)
                Next
            Next
            Return result
        End Function

    End Module

#End Region

#Region "条件去噪网络 - Conditional Denoising Network"

    ''' <summary>
    ''' 条件去噪网络（Conditional Denoising Network）
    ''' 
    ''' 预测在时间步t加入的噪声: epsilon_theta(x_t, t, c)
    ''' 
    ''' 网络架构（基于MLP，适用于向量数据如细胞基因表达谱）:
    '''   输入: [x_t; c; time_emb]  (拼接细胞状态、条件、时间嵌入)
    '''     -> Linear(input+cond+time_emb, hidden) -> LayerNorm -> ReLU
    '''     -> Linear(hidden, hidden) -> LayerNorm -> ReLU
    '''     -> Linear(hidden, hidden) -> LayerNorm -> ReLU
    '''     -> Linear(hidden, input) -> epsilon (预测的噪声)
    ''' 
    ''' 复用 CVAE.vb 中的 LinearLayer, LayerNormLayer, ReLULayer, AdamOptimizer
    ''' </summary>
    Public Class DenoisingNetwork

        ''' <summary>输入维度（细胞状态维度，如基因数）</summary>
        Public Property InputDim As Integer

        ''' <summary>条件维度（如时间点、处理剂量等）</summary>
        Public Property ConditionDim As Integer

        ''' <summary>时间嵌入维度</summary>
        Public Property TimeEmbedDim As Integer = 64

        ''' <summary>隐藏层维度</summary>
        Public Property HiddenDim As Integer = 256

        ''' <summary>网络层</summary>
        Public Linear1, Linear2, Linear3, Linear4 As LinearLayer
        Public LN1, LN2, LN3 As LayerNormLayer
        Public ReLU1, ReLU2, ReLU3 As ReLULayer

        ''' <summary>Adam优化器</summary>
        Public Optimizer As AdamOptimizer

        ''' <summary>
        ''' 创建条件去噪网络
        ''' </summary>
        Public Sub New(inputDim As Integer, conditionDim As Integer,
                       Optional timeEmbedDim As Integer = 64,
                       Optional hiddenDim As Integer = 256,
                       Optional seed As Integer? = Nothing)
            Me.InputDim = inputDim
            Me.ConditionDim = conditionDim
            Me.TimeEmbedDim = timeEmbedDim
            Me.HiddenDim = hiddenDim

            Dim seedVal As Integer = If(seed.HasValue, seed.Value, 42)
            Dim inputSize As Integer = inputDim + conditionDim + timeEmbedDim

            ' 构建网络层（复用CVAE.vb中的层实现）
            Linear1 = New LinearLayer(inputSize, hiddenDim, seedVal + 1)
            LN1 = New LayerNormLayer(hiddenDim)
            ReLU1 = New ReLULayer()

            Linear2 = New LinearLayer(hiddenDim, hiddenDim, seedVal + 2)
            LN2 = New LayerNormLayer(hiddenDim)
            ReLU2 = New ReLULayer()

            Linear3 = New LinearLayer(hiddenDim, hiddenDim, seedVal + 3)
            LN3 = New LayerNormLayer(hiddenDim)
            ReLU3 = New ReLULayer()

            Linear4 = New LinearLayer(hiddenDim, inputDim, seedVal + 4)

            Optimizer = New AdamOptimizer(learningRate:=0.001)
        End Sub

        ''' <summary>
        ''' 沿特征维度拼接多个张量 [t1; t2; t3; ...]
        ''' </summary>
        Private Function ConcatenateFeatures(tensors As Tensor()) As Tensor
            Dim batch As Integer = tensors(0).Shape(0)
            Dim totalCols As Integer = 0
            For Each t As Tensor In tensors
                totalCols += t.Shape(1)
            Next
            Dim result As New Tensor(batch, totalCols)
            Dim offset As Integer = 0
            For Each t As Tensor In tensors
                Dim cols As Integer = t.Shape(1)
                For i As Integer = 0 To batch - 1
                    For j As Integer = 0 To cols - 1
                        result(i, offset + j) = t(i, j)
                    Next
                Next
                offset += cols
            Next
            Return result
        End Function

        ''' <summary>
        ''' 前向传播: 预测噪声 epsilon_theta(x_t, t, c)
        ''' </summary>
        ''' <param name="xt">加噪后的细胞状态 (batch, InputDim)</param>
        ''' <param name="timeEmb">时间步嵌入 (batch, TimeEmbedDim)</param>
        ''' <param name="c">条件向量 (batch, ConditionDim)</param>
        ''' <returns>预测的噪声 (batch, InputDim)</returns>
        Public Function Forward(xt As Tensor, timeEmb As Tensor, c As Tensor) As Tensor
            ' 拼接输入: [x_t; c; time_emb]
            Dim input As Tensor = ConcatenateFeatures({xt, c, timeEmb})

            ' 第一层
            Dim h As Tensor = Linear1.Forward(input)
            h = LN1.Forward(h)
            h = ReLU1.Forward(h)

            ' 第二层
            h = Linear2.Forward(h)
            h = LN2.Forward(h)
            h = ReLU2.Forward(h)

            ' 第三层
            h = Linear3.Forward(h)
            h = LN3.Forward(h)
            h = ReLU3.Forward(h)

            ' 输出层
            Dim epsPred As Tensor = Linear4.Forward(h)
            Return epsPred
        End Function

        ''' <summary>
        ''' 反向传播
        ''' </summary>
        ''' <param name="gradOutput">损失对预测噪声的梯度 (batch, InputDim)</param>
        Public Sub Backward(gradOutput As Tensor)
            Dim grad As Tensor = Linear4.Backward(gradOutput)
            grad = ReLU3.Backward(grad)
            grad = LN3.Backward(grad)
            grad = Linear3.Backward(grad)
            grad = ReLU2.Backward(grad)
            grad = LN2.Backward(grad)
            grad = Linear2.Backward(grad)
            grad = ReLU1.Backward(grad)
            grad = LN1.Backward(grad)
            Linear1.Backward(grad)
        End Sub

        ''' <summary>
        ''' 使用Adam优化器更新所有参数
        ''' </summary>
        Public Sub UpdateParameters()
            Linear1.UpdateParameters(Optimizer)
            Linear2.UpdateParameters(Optimizer)
            Linear3.UpdateParameters(Optimizer)
            Linear4.UpdateParameters(Optimizer)
            LN1.UpdateParameters(Optimizer)
            LN2.UpdateParameters(Optimizer)
            LN3.UpdateParameters(Optimizer)
        End Sub

        ''' <summary>
        ''' 清零所有梯度
        ''' </summary>
        Public Sub ZeroGrad()
            Linear1.ZeroGrad()
            Linear2.ZeroGrad()
            Linear3.ZeroGrad()
            Linear4.ZeroGrad()
            LN1.ZeroGrad()
            LN2.ZeroGrad()
            LN3.ZeroGrad()
        End Sub

        ''' <summary>
        ''' 保存网络参数到文件
        ''' </summary>
        Public Sub Save(filePath As String)
            Using writer As New System.IO.StreamWriter(filePath)
                writer.WriteLine($"InputDim:{InputDim}")
                writer.WriteLine($"ConditionDim:{ConditionDim}")
                writer.WriteLine($"TimeEmbedDim:{TimeEmbedDim}")
                writer.WriteLine($"HiddenDim:{HiddenDim}")
                writer.WriteLine($"LearningRate:{Optimizer.LearningRate}")
                writer.WriteLine("---Linear1---")
                'Linear1.Save(writer)
                writer.WriteLine("---LN1---")
                'LN1.Save(writer)
                writer.WriteLine("---Linear2---")
                'Linear2.Save(writer)
                writer.WriteLine("---LN2---")
                'LN2.Save(writer)
                writer.WriteLine("---Linear3---")
                'Linear3.Save(writer)
                writer.WriteLine("---LN3---")
                'LN3.Save(writer)
                writer.WriteLine("---Linear4---")
                'Linear4.Save(writer)
            End Using
        End Sub

        ''' <summary>
        ''' 从文件加载网络参数
        ''' </summary>
        Public Sub Load(filePath As String)
            Using reader As New System.IO.StreamReader(filePath)
                Dim line As String
                ' 读取配置
                line = reader.ReadLine()  ' InputDim
                line = reader.ReadLine()  ' ConditionDim
                line = reader.ReadLine()  ' TimeEmbedDim
                line = reader.ReadLine()  ' HiddenDim
                line = reader.ReadLine()  ' LearningRate
                reader.ReadLine()  ' ---Linear1---
                'Linear1.Load(reader)
                reader.ReadLine()  ' ---LN1---
                'LN1.Load(reader)
                reader.ReadLine()  ' ---Linear2---
                'Linear2.Load(reader)
                reader.ReadLine()  ' ---LN2---
                'LN2.Load(reader)
                reader.ReadLine()  ' ---Linear3---
                'Linear3.Load(reader)
                reader.ReadLine()  ' ---LN3---
                'LN3.Load(reader)
                reader.ReadLine()  ' ---Linear4---
                'Linear4.Load(reader)
            End Using
        End Sub

    End Class

#End Region

#Region "条件扩散模型 - Conditional Diffusion Model"

    ''' <summary>
    ''' 条件扩散模型（Conditional Diffusion Model）
    ''' 
    ''' 核心算法:
    '''   1. 前向过程: q(x_t | x_0) = N(x_t; sqrt(alpha_bar_t)*x_0, (1-alpha_bar_t)*I)
    '''      闭式解: x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1-alpha_bar_t) * epsilon
    ''' 
    '''   2. 反向过程: p_theta(x_{t-1} | x_t) = N(x_{t-1}; mu_theta, sigma_t^2 * I)
    '''      mu_theta = (1/sqrt(alpha_t)) * (x_t - (1-alpha_t)/sqrt(1-alpha_bar_t) * epsilon_theta(x_t,t,c))
    '''      sigma_t = sqrt(beta_t) 或 sqrt(后验方差)
    ''' 
    '''   3. 训练: L = E[||epsilon - epsilon_theta(x_t, t, c)||^2]
    ''' 
    '''   4. 条件生成: 给定条件c，从x_T~N(0,I)开始逐步去噪得到x_0
    ''' </summary>
    Public Class DiffusionModel

        ''' <summary>输入维度（细胞状态维度）</summary>
        Public Property InputDim As Integer

        ''' <summary>条件维度</summary>
        Public Property ConditionDim As Integer

        ''' <summary>扩散总步数T</summary>
        Public Property NumTimesteps As Integer = 200

        ''' <summary>噪声调度器</summary>
        Public Scheduler As NoiseScheduler

        ''' <summary>去噪网络</summary>
        Public Network As DenoisingNetwork

        ''' <summary>随机数生成器</summary>
        Private Random As Random

        ' ===== 训练缓存（用于反向传播）=====
        Private CachedEps As Tensor       ' 真实噪声
        Private CachedNoisePred As Tensor ' 预测噪声

        ''' <summary>
        ''' 创建条件扩散模型
        ''' </summary>
        Public Sub New(inputDim As Integer, conditionDim As Integer,
                       Optional numTimesteps As Integer = 200,
                       Optional timeEmbedDim As Integer = 64,
                       Optional hiddenDim As Integer = 256,
                       Optional scheduleType As String = "linear",
                       Optional seed As Integer? = Nothing)
            Me.InputDim = inputDim
            Me.ConditionDim = conditionDim
            Me.NumTimesteps = numTimesteps
            Me.Random = New Random(If(seed.HasValue, seed.Value, 42))

            Scheduler = New NoiseScheduler(numTimesteps, 0.0001, 0.02, scheduleType)
            Network = New DenoisingNetwork(inputDim, conditionDim, timeEmbedDim, hiddenDim, seed)
        End Sub

        ''' <summary>
        ''' 从标准正态分布采样（Box-Muller变换）
        ''' </summary>
        Private Function SampleNormal() As Double
            Dim u1 As Double = 1.0 - Random.NextDouble()
            Dim u2 As Double = 1.0 - Random.NextDouble()
            Return std.Sqrt(-2.0 * std.Log(u1)) * std.Cos(2.0 * std.PI * u2)
        End Function

        ''' <summary>
        ''' 创建标准正态噪声张量
        ''' </summary>
        Private Function CreateNoiseTensor(batch As Integer, [dim] As Integer) As Tensor
            Dim noise As New Tensor(batch, [dim])
            For i As Integer = 0 To noise.Length - 1
                noise.Data(i) = SampleNormal()
            Next
            Return noise
        End Function

        ''' <summary>
        ''' 前向扩散过程: 给x_0添加t步噪声得到x_t
        ''' x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1-alpha_bar_t) * epsilon
        ''' </summary>
        ''' <param name="x0">原始细胞状态 (batch, InputDim)</param>
        ''' <param name="timeSteps">每个样本的时间步 (batch,)</param>
        ''' <param name="eps">可选的预采样噪声，若为Nothing则自动采样</param>
        ''' <returns>加噪后的状态 x_t (batch, InputDim)</returns>
        Public Function QSample(x0 As Tensor, timeSteps As Integer(), Optional eps As Tensor = Nothing) As Tensor
            Dim batch As Integer = x0.Shape(0)
            Dim [dim] As Integer = x0.Shape(1)

            ' 采样噪声 epsilon ~ N(0, I)
            If eps Is Nothing Then
                eps = CreateNoiseTensor(batch, [dim])
            End If

            ' 计算 x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1-alpha_bar_t) * epsilon
            Dim xt As New Tensor(batch, [dim])
            For i As Integer = 0 To batch - 1
                Dim t As Integer = timeSteps(i)
                Dim sqrtAlphaBar As Double = Scheduler.SqrtAlphaBars(t)
                Dim sqrtOneMinusAlphaBar As Double = Scheduler.SqrtOneMinusAlphaBars(t)
                For j As Integer = 0 To [dim] - 1
                    xt(i, j) = sqrtAlphaBar * x0(i, j) + sqrtOneMinusAlphaBar * eps(i, j)
                Next
            Next

            Return xt
        End Function

        ''' <summary>
        ''' 构建时间步嵌入张量
        ''' </summary>
        Private Function BuildTimeEmbedding(timeSteps As Integer()) As Tensor
            Dim batch As Integer = timeSteps.Length
            Dim timeEmb As New Tensor(batch, Network.TimeEmbedDim)
            For i As Integer = 0 To batch - 1
                Dim emb As Double() = TimeEmbedding.SinusoidalEmbedding(CDbl(timeSteps(i)), Network.TimeEmbedDim)
                For j As Integer = 0 To Network.TimeEmbedDim - 1
                    timeEmb(i, j) = emb(j)
                Next
            Next
            Return timeEmb
        End Function

        ''' <summary>
        ''' 训练步骤: 采样t -> 加噪 -> 预测噪声 -> 计算损失
        ''' 返回MSE损失值（未执行反向传播）
        ''' </summary>
        ''' <param name="x0">原始细胞状态 (batch, InputDim)</param>
        ''' <param name="c">条件向量 (batch, ConditionDim)</param>
        ''' <returns>MSE损失</returns>
        Public Function TrainingStep(x0 As Tensor, c As Tensor) As Double
            Dim batch As Integer = x0.Shape(0)

            ' 1. 为每个样本随机采样时间步 t ~ Uniform{0, 1, ..., T-1}
            Dim timeSteps(batch - 1) As Integer
            For i As Integer = 0 To batch - 1
                timeSteps(i) = Random.Next(0, NumTimesteps)
            Next

            ' 2. 采样噪声 epsilon ~ N(0, I)
            Dim eps As Tensor = CreateNoiseTensor(batch, InputDim)

            ' 3. 前向扩散: x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1-alpha_bar_t) * epsilon
            Dim xt As Tensor = QSample(x0, timeSteps, eps)

            ' 4. 构建时间嵌入
            Dim timeEmb As Tensor = BuildTimeEmbedding(timeSteps)

            ' 5. 预测噪声 epsilon_theta(x_t, t, c)
            Dim epsPred As Tensor = Network.Forward(xt, timeEmb, c)

            ' 6. 计算MSE损失并缓存
            CachedEps = eps
            CachedNoisePred = epsPred
            Return ComputeLoss(eps, epsPred)
        End Function

        ''' <summary>
        ''' 计算MSE损失: L = mean(||epsilon - epsilon_pred||^2)
        ''' </summary>
        Public Function ComputeLoss(eps As Tensor, epsPred As Tensor) As Double
            Dim batch As Integer = eps.Shape(0)
            Dim [dim] As Integer = eps.Shape(1)
            Dim loss As Double = 0.0
            For i As Integer = 0 To batch - 1
                For j As Integer = 0 To [dim] - 1
                    Dim diff As Double = epsPred(i, j) - eps(i, j)
                    loss += diff * diff
                Next
            Next
            Return loss / CDbl(batch)
        End Function

        ''' <summary>
        ''' 反向传播: 计算梯度并传播到去噪网络
        ''' d(Loss)/d(eps_pred) = 2 * (eps_pred - eps) / batch
        ''' </summary>
        Public Sub Backward()
            Dim batch As Integer = CachedEps.Shape(0)
            Dim [dim] As Integer = CachedEps.Shape(1)

            Dim gradOutput As New Tensor(CachedNoisePred.Shape)
            Dim scale As Double = 2.0 / CDbl(batch)
            For i As Integer = 0 To batch - 1
                For j As Integer = 0 To [dim] - 1
                    gradOutput(i, j) = scale * (CachedNoisePred(i, j) - CachedEps(i, j))
                Next
            Next

            Network.Backward(gradOutput)
        End Sub

        ''' <summary>
        ''' 更新模型参数
        ''' </summary>
        Public Sub UpdateParameters()
            Network.UpdateParameters()
        End Sub

        ''' <summary>
        ''' 清零梯度
        ''' </summary>
        Public Sub ZeroGrad()
            Network.ZeroGrad()
        End Sub

        ''' <summary>
        ''' 反向去噪一步: p_sample
        ''' x_{t-1} = (1/sqrt(alpha_t)) * (x_t - (1-alpha_t)/sqrt(1-alpha_bar_t) * eps_theta) + sigma_t * z
        ''' </summary>
        ''' <param name="xt">当前状态 (batch, InputDim)</param>
        ''' <param name="t">当前时间步</param>
        ''' <param name="c">条件向量 (batch, ConditionDim)</param>
        ''' <returns>去噪后的状态 x_{t-1}</returns>
        Public Function PSample(xt As Tensor, t As Integer, c As Tensor) As Tensor
            Dim batch As Integer = xt.Shape(0)
            Dim [dim] As Integer = xt.Shape(1)

            ' 构建时间嵌入（所有样本使用相同时间步t）
            Dim timeSteps(batch - 1) As Integer
            For i As Integer = 0 To batch - 1
                timeSteps(i) = t
            Next
            Dim timeEmb As Tensor = BuildTimeEmbedding(timeSteps)

            ' 预测噪声
            Dim epsPred As Tensor = Network.Forward(xt, timeEmb, c)

            ' 计算均值: mu = (1/sqrt(alpha_t)) * (x_t - (1-alpha_t)/sqrt(1-alpha_bar_t) * eps_pred)
            Dim sqrtRecipAlpha As Double = Scheduler.SqrtRecipAlphas(t)
            Dim coeff As Double = Scheduler.OneMinusAlphasOverSqrtOneMinusAlphaBars(t)

            Dim mean As New Tensor(batch, [dim])
            For i As Integer = 0 To batch - 1
                For j As Integer = 0 To [dim] - 1
                    mean(i, j) = sqrtRecipAlpha * (xt(i, j) - coeff * epsPred(i, j))
                Next
            Next

            ' t=0时直接返回均值（不加噪声）
            If t = 0 Then
                Return mean
            End If

            ' t>0时添加噪声: x_{t-1} = mean + sigma_t * z, z ~ N(0, I)
            Dim sigma As Double = Scheduler.PosteriorLogVarClipped(t)
            Dim result As New Tensor(batch, [dim])
            For i As Integer = 0 To batch - 1
                For j As Integer = 0 To [dim] - 1
                    result(i, j) = mean(i, j) + sigma * SampleNormal()
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 完整采样: 给定条件c，从纯噪声开始逐步去噪生成细胞状态
        ''' x_T ~ N(0, I) -> x_{T-1} -> ... -> x_0
        ''' </summary>
        ''' <param name="c">条件向量 (numSamples, ConditionDim)</param>
        ''' <param name="numSamples">生成样本数（若c已有batch则使用c的batch）</param>
        ''' <returns>生成的细胞状态 (numSamples, InputDim)</returns>
        Public Function Sample(c As Tensor, Optional numSamples As Integer = 1) As Tensor
            Dim batch As Integer = c.Shape(0)
            If batch <> numSamples Then
                numSamples = batch
            End If

            ' 从纯噪声开始: x_T ~ N(0, I)
            Dim xt As Tensor = CreateNoiseTensor(numSamples, InputDim)

            ' 反向去噪: t = T-1, T-2, ..., 0
            For t As Integer = NumTimesteps - 1 To 0 Step -1
                xt = PSample(xt, t, c)
            Next

            Return xt
        End Function

        ''' <summary>
        ''' 保存模型到文件
        ''' </summary>
        Public Sub Save(filePath As String)
            Using writer As New System.IO.StreamWriter(filePath)
                writer.WriteLine($"InputDim:{InputDim}")
                writer.WriteLine($"ConditionDim:{ConditionDim}")
                writer.WriteLine($"NumTimesteps:{NumTimesteps}")
                writer.WriteLine($"ScheduleType:{Scheduler.ScheduleType}")
                writer.WriteLine($"TimeEmbedDim:{Network.TimeEmbedDim}")
                writer.WriteLine($"HiddenDim:{Network.HiddenDim}")
            End Using
            Network.Save(filePath & ".network")
        End Sub

        ''' <summary>
        ''' 从文件加载模型
        ''' </summary>
        Public Shared Function Load(filePath As String) As DiffusionModel
            Dim inputDim As Integer, conditionDim As Integer, numTimesteps As Integer
            Dim scheduleType As String, timeEmbedDim As Integer, hiddenDim As Integer

            Using reader As New System.IO.StreamReader(filePath)
                inputDim = CInt(reader.ReadLine().Split(":"c)(1))
                conditionDim = CInt(reader.ReadLine().Split(":"c)(1))
                numTimesteps = CInt(reader.ReadLine().Split(":"c)(1))
                scheduleType = reader.ReadLine().Split(":"c)(1)
                timeEmbedDim = CInt(reader.ReadLine().Split(":"c)(1))
                hiddenDim = CInt(reader.ReadLine().Split(":"c)(1))
            End Using

            Dim model As New DiffusionModel(inputDim, conditionDim, numTimesteps,
                                            timeEmbedDim, hiddenDim, scheduleType)
            model.Network.Load(filePath & ".network")
            Return model
        End Function

    End Class

#End Region

#Region "数据预处理器 - Data Preprocessor"

    ''' <summary>
    ''' 扩散模型数据预处理器
    ''' 
    ''' 复用CVAE.vb中DataPreprocessor的设计模式，包含:
    '''   - 归一化（Library Size Normalization, 目标总和10000）
    '''   - 对数变换（log1p）
    '''   - Z-score标准化（基因维度）
    '''   - 条件归一化到[0, 1]
    ''' </summary>
    Public Class DiffusionDataPreprocessor

        ''' <summary>归一化目标总和</summary>
        Public Property TargetSum As Double = 10000.0

        ''' <summary>是否已执行对数变换</summary>
        Public Property IsLogTransformed As Boolean = False

        ''' <summary>条件最小值</summary>
        Public Property MinCondition As Double = 0.0

        ''' <summary>条件最大值</summary>
        Public Property MaxCondition As Double = 1.0

        ' 标准化参数
        Private GeneMeans As Double()
        Private GeneStds As Double()

        ''' <summary>
        ''' 归一化（Library Size Normalization）+ 对数变换
        ''' </summary>
        Public Function NormalizeAndLog(data As Double(,)) As Double(,)
            Dim nCells As Integer = data.GetLength(0)
            Dim nGenes As Integer = data.GetLength(1)
            Dim result(nCells - 1, nGenes - 1) As Double

            For i As Integer = 0 To nCells - 1
                ' 计算library size
                Dim libSize As Double = 0.0
                For j As Integer = 0 To nGenes - 1
                    libSize += data(i, j)
                Next

                ' 归一化 + log1p变换
                If libSize > 0 Then
                    Dim scaleFactor As Double = TargetSum / libSize
                    For j As Integer = 0 To nGenes - 1
                        result(i, j) = log1p(data(i, j) * scaleFactor)
                    Next
                Else
                    For j As Integer = 0 To nGenes - 1
                        result(i, j) = 0.0
                    Next
                End If
            Next

            IsLogTransformed = True
            Return result
        End Function

        ''' <summary>
        ''' Z-score标准化（基因维度）
        ''' 计算每个基因的均值和标准差，并进行标准化
        ''' </summary>
        Public Function StandardizeGenes(data As Double(,)) As Double(,)
            Dim nCells As Integer = data.GetLength(0)
            Dim nGenes As Integer = data.GetLength(1)
            ReDim GeneMeans(nGenes - 1)
            ReDim GeneStds(nGenes - 1)

            ' 计算每个基因的均值
            For j As Integer = 0 To nGenes - 1
                Dim sum As Double = 0.0
                For i As Integer = 0 To nCells - 1
                    sum += data(i, j)
                Next
                GeneMeans(j) = sum / nCells
            Next

            ' 计算每个基因的标准差
            For j As Integer = 0 To nGenes - 1
                Dim sumSq As Double = 0.0
                For i As Integer = 0 To nCells - 1
                    Dim diff As Double = data(i, j) - GeneMeans(j)
                    sumSq += diff * diff
                Next
                GeneStds(j) = std.Sqrt(sumSq / nCells)
                If GeneStds(j) < 0.00000001 Then GeneStds(j) = 1.0
            Next

            ' 标准化
            Dim result(nCells - 1, nGenes - 1) As Double
            For i As Integer = 0 To nCells - 1
                For j As Integer = 0 To nGenes - 1
                    result(i, j) = (data(i, j) - GeneMeans(j)) / GeneStds(j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 反向标准化（Z-score逆变换）
        ''' </summary>
        Public Function InverseStandardize(data As Double(,)) As Double(,)
            Dim nCells As Integer = data.GetLength(0)
            Dim nGenes As Integer = data.GetLength(1)
            Dim result(nCells - 1, nGenes - 1) As Double

            For i As Integer = 0 To nCells - 1
                For j As Integer = 0 To nGenes - 1
                    result(i, j) = data(i, j) * GeneStds(j) + GeneMeans(j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 归一化条件到[0, 1]
        ''' </summary>
        Public Function NormalizeConditions(conditions As Double()) As Double()
            MinCondition = conditions.Min()
            MaxCondition = conditions.Max()
            Dim range As Double = MaxCondition - MinCondition
            If range < 0.0000000001 Then range = 1.0

            Dim result(conditions.Length - 1) As Double
            For i As Integer = 0 To conditions.Length - 1
                result(i) = (conditions(i) - MinCondition) / range
            Next
            Return result
        End Function

        ''' <summary>
        ''' 反归一化条件
        ''' </summary>
        Public Function DenormalizeCondition(normalizedCond As Double) As Double
            Return normalizedCond * (MaxCondition - MinCondition) + MinCondition
        End Function

        ''' <summary>
        ''' 完整逆变换: 反标准化 -> 反对数变换
        ''' </summary>
        Public Function InverseTransform(data As Double(,)) As Double(,)
            ' 反Z-score
            Dim destandardized As Double(,) = InverseStandardize(data)

            ' 反log1p
            Dim nCells As Integer = destandardized.GetLength(0)
            Dim nGenes As Integer = destandardized.GetLength(1)
            Dim result(nCells - 1, nGenes - 1) As Double
            For i As Integer = 0 To nCells - 1
                For j As Integer = 0 To nGenes - 1
                    result(i, j) = std.Exp(destandardized(i, j)) - 1.0
                    If result(i, j) < 0 Then result(i, j) = 0.0
                Next
            Next

            Return result
        End Function

    End Class

#End Region

#Region "训练器 - Diffusion Trainer"

    ''' <summary>
    ''' 扩散模型训练器
    ''' 
    ''' 复用CVAE.vb中CVAETrainer的设计模式，包含:
    '''   - 批次训练
    '''   - 学习率衰减
    '''   - 数据打乱
    '''   - 损失历史记录
    '''   - 验证集评估
    ''' </summary>
    Public Class DiffusionTrainer

        ''' <summary>扩散模型</summary>
        Public Property Model As DiffusionModel

        ''' <summary>批量大小</summary>
        Public Property BatchSize As Integer = 128

        ''' <summary>训练轮数</summary>
        Public Property Epochs As Integer = 100

        ''' <summary>学习率衰减因子</summary>
        Public Property LrDecayFactor As Double = 0.95

        ''' <summary>学习率衰减间隔（每N轮衰减一次）</summary>
        Public Property LrDecayInterval As Integer = 20

        ''' <summary>是否打乱数据</summary>
        Public Property ShuffleData As Boolean = True

        ''' <summary>损失历史</summary>
        Public Property LossHistory As New List(Of Double)

        ''' <summary>验证损失历史</summary>
        Public Property ValLossHistory As New List(Of Double)

        ''' <summary>随机种子</summary>
        Public Property Seed As Integer? = 42

        Private Random As Random

        ''' <summary>
        ''' 创建训练器
        ''' </summary>
        Public Sub New(model As DiffusionModel, Optional batchSize As Integer = 128,
                       Optional epochs As Integer = 100, Optional learningRate As Double = 0.001)
            Me.Model = model
            Me.BatchSize = batchSize
            Me.Epochs = epochs
            Me.Model.Network.Optimizer.LearningRate = learningRate
            Me.Random = New Random(If(Seed.HasValue, Seed.Value, 42))
        End Sub

        ''' <summary>
        ''' 训练模型
        ''' </summary>
        ''' <param name="data">细胞状态数据 (nCells, nGenes)</param>
        ''' <param name="conditions">条件数组 (nCells,)</param>
        ''' <param name="verbose">是否打印训练进度</param>
        Public Sub Train(data As Double(,), conditions As Double(), Optional verbose As Boolean = True)
            Dim nCells As Integer = data.GetLength(0)
            Dim nGenes As Integer = data.GetLength(1)
            Dim numBatches As Integer = CInt(std.Ceiling(nCells / CDbl(BatchSize)))

            If verbose Then
                Console.WriteLine("=" & StrDup(58, "="))
                Console.WriteLine("开始训练Diffusion模型")
                Console.WriteLine("=" & StrDup(58, "="))
                Console.WriteLine($"  细胞数 (Samples):     {nCells}")
                Console.WriteLine($"  基因数 (Features):    {nGenes}")
                Console.WriteLine($"  条件维度 (Cond Dim):  {Model.ConditionDim}")
                Console.WriteLine($"  批量大小 (Batch):     {BatchSize}")
                Console.WriteLine($"  训练轮数 (Epochs):    {Epochs}")
                Console.WriteLine($"  扩散步数 (Steps T):   {Model.NumTimesteps}")
                Console.WriteLine($"  噪声调度 (Schedule):  {Model.Scheduler.ScheduleType}")
                Console.WriteLine($"  隐藏维度 (Hidden):    {Model.Network.HiddenDim}")
                Console.WriteLine($"  时间嵌入 (TimeEmb):   {Model.Network.TimeEmbedDim}")
                Console.WriteLine($"  初始学习率 (LR):      {Model.Network.Optimizer.LearningRate:F6}")
                Console.WriteLine()
            End If

            Dim indices = Enumerable.Range(0, nCells).ToArray()

            For epoch As Integer = 1 To Epochs
                ' 学习率衰减
                If epoch > 1 AndAlso epoch Mod LrDecayInterval = 0 Then
                    Model.Network.Optimizer.LearningRate *= LrDecayFactor
                End If

                ' 打乱数据
                If ShuffleData Then
                    Shuffle(indices)
                End If

                Dim epochLoss As Double = 0.0
                Dim batchCount As Integer = 0

                ' 批次训练
                For batchStart As Integer = 0 To nCells - 1 Step BatchSize
                    Dim batchEnd As Integer = std.Min(batchStart + BatchSize, nCells)
                    Dim currentBatchSize As Integer = batchEnd - batchStart

                    ' 构建批次数据
                    Dim batchData As New Tensor(currentBatchSize, nGenes)
                    Dim batchConditions As New Tensor(currentBatchSize, Model.ConditionDim)

                    For k As Integer = 0 To currentBatchSize - 1
                        Dim cellIdx As Integer = indices(batchStart + k)
                        For j As Integer = 0 To nGenes - 1
                            batchData(k, j) = data(cellIdx, j)
                        Next
                        For j As Integer = 0 To Model.ConditionDim - 1
                            batchConditions(k, j) = conditions(cellIdx)
                        Next
                    Next

                    ' 训练步骤
                    Model.ZeroGrad()
                    Dim loss As Double = Model.TrainingStep(batchData, batchConditions)
                    Model.Backward()
                    Model.UpdateParameters()

                    epochLoss += loss
                    batchCount += 1
                Next

                Dim avgLoss As Double = epochLoss / batchCount
                LossHistory.Add(avgLoss)

                ' 打印进度
                If verbose AndAlso (epoch Mod 10 = 0 OrElse epoch = 1 OrElse epoch = Epochs) Then
                    Console.WriteLine($"  Epoch {epoch,4}/{Epochs} | Loss: {avgLoss,12:F6} | LR: {Model.Network.Optimizer.LearningRate,10:F6}")
                End If
            Next

            If verbose Then
                Console.WriteLine()
                Console.WriteLine("训练完成！")
                Console.WriteLine($"  最终损失: {LossHistory(LossHistory.Count - 1):F6}")
                Console.WriteLine($"  最低损失: {LossHistory.Min():F6}")
                Console.WriteLine()
            End If
        End Sub

        ''' <summary>
        ''' 在验证集上评估模型
        ''' </summary>
        Public Function Evaluate(data As Double(,), conditions As Double()) As Double
            Dim nCells As Integer = data.GetLength(0)
            Dim nGenes As Integer = data.GetLength(1)
            Dim totalLoss As Double = 0.0
            Dim batchCount As Integer = 0

            For batchStart As Integer = 0 To nCells - 1 Step BatchSize
                Dim batchEnd As Integer = std.Min(batchStart + BatchSize, nCells)
                Dim currentBatchSize As Integer = batchEnd - batchStart

                Dim batchData As New Tensor(currentBatchSize, nGenes)
                Dim batchConditions As New Tensor(currentBatchSize, Model.ConditionDim)

                For k As Integer = 0 To currentBatchSize - 1
                    Dim cellIdx As Integer = batchStart + k
                    For j As Integer = 0 To nGenes - 1
                        batchData(k, j) = data(cellIdx, j)
                    Next
                    For j As Integer = 0 To Model.ConditionDim - 1
                        batchConditions(k, j) = conditions(cellIdx)
                    Next
                Next

                Dim loss As Double = Model.TrainingStep(batchData, batchConditions)
                totalLoss += loss
                batchCount += 1
            Next

            Return totalLoss / batchCount
        End Function

        ''' <summary>
        ''' Fisher-Yates打乱算法
        ''' </summary>
        Private Sub Shuffle(arr As Integer())
            For i As Integer = arr.Length - 1 To 1 Step -1
                Dim j As Integer = Random.Next(i + 1)
                Dim temp As Integer = arr(i)
                arr(i) = arr(j)
                arr(j) = temp
            Next
        End Sub

    End Class

#End Region

#Region "细胞状态预测器 - Cell State Predictor"

    ''' <summary>
    ''' 细胞状态预测器
    ''' 
    ''' 封装扩散模型的采样过程，提供简洁的预测接口:
    '''   - 给定条件（如时间点），生成对应的细胞状态
    '''   - 支持单个或批量条件预测
    '''   - 自动处理数据预处理逆变换
    ''' </summary>
    Public Class CellStatePredictor

        ''' <summary>扩散模型</summary>
        Public Property Model As DiffusionModel

        ''' <summary>数据预处理器（用于逆变换）</summary>
        Public Property Preprocessor As DiffusionDataPreprocessor

        ''' <summary>
        ''' 创建预测器
        ''' </summary>
        Public Sub New(model As DiffusionModel, Optional preprocessor As DiffusionDataPreprocessor = Nothing)
            Me.Model = model
            Me.Preprocessor = preprocessor
        End Sub

        ''' <summary>
        ''' 预测给定条件下的细胞状态
        ''' </summary>
        ''' <param name="conditions">条件数组（已归一化到[0,1]）</param>
        ''' <returns>生成的细胞状态 (numSamples, nGenes)（标准化空间）</returns>
        Public Function PredictNormalized(conditions As Double()) As Double(,)
            Dim numSamples As Integer = conditions.Length
            Dim condTensor As New Tensor(numSamples, Model.ConditionDim)
            For i As Integer = 0 To numSamples - 1
                For j As Integer = 0 To Model.ConditionDim - 1
                    condTensor(i, j) = conditions(i)
                Next
            Next

            ' 从模型采样
            Dim generated As Tensor = Model.Sample(condTensor, numSamples)

            ' 转换为二维数组
            Dim result(numSamples - 1, Model.InputDim - 1) As Double
            For i As Integer = 0 To numSamples - 1
                For j As Integer = 0 To Model.InputDim - 1
                    result(i, j) = generated(i, j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 预测给定条件下的细胞状态（原始空间，含逆变换）
        ''' </summary>
        Public Function Predict(conditions As Double()) As Double(,)
            Dim normalizedResult As Double(,) = PredictNormalized(conditions)

            ' 逆变换到原始空间
            If Preprocessor IsNot Nothing Then
                Return Preprocessor.InverseTransform(normalizedResult)
            End If

            Return normalizedResult
        End Function

        ''' <summary>
        ''' 对单个条件生成多个样本
        ''' </summary>
        ''' <param name="condition">单个条件值（已归一化）</param>
        ''' <param name="numSamples">生成样本数</param>
        ''' <returns>生成的细胞状态 (numSamples, nGenes)</returns>
        Public Function PredictMultiple(condition As Double, numSamples As Integer) As Double(,)
            Dim conditions(numSamples - 1) As Double
            For i As Integer = 0 To numSamples - 1
                conditions(i) = condition
            Next
            Return Predict(conditions)
        End Function

        ''' <summary>
        ''' 预测并返回统计信息（均值和标准差）
        ''' </summary>
        Public Function PredictWithStats(conditions As Double(), numSamplesPerCond As Integer) As (mean As Double(,), std As Double(,))
            Dim numConds As Integer = conditions.Length
            Dim nGenes As Integer = Model.InputDim
            Dim meanResult(numConds - 1, nGenes - 1) As Double
            Dim stdResult(numConds - 1, nGenes - 1) As Double

            For ci As Integer = 0 To numConds - 1
                Dim samples As Double(,) = PredictMultiple(conditions(ci), numSamplesPerCond)
                ' 计算均值
                For j As Integer = 0 To nGenes - 1
                    Dim sum As Double = 0.0
                    For i As Integer = 0 To numSamplesPerCond - 1
                        sum += samples(i, j)
                    Next
                    meanResult(ci, j) = sum / numSamplesPerCond
                Next
                ' 计算标准差
                For j As Integer = 0 To nGenes - 1
                    Dim sumSq As Double = 0.0
                    For i As Integer = 0 To numSamplesPerCond - 1
                        Dim diff As Double = samples(i, j) - meanResult(ci, j)
                        sumSq += diff * diff
                    Next
                    stdResult(ci, j) = std.Sqrt(sumSq / numSamplesPerCond)
                Next
            Next

            Return (meanResult, stdResult)
        End Function

    End Class

#End Region

#Region "模拟数据生成器 - Synthetic Cell Data Generator"

    ''' <summary>
    ''' 模拟细胞状态数据生成器
    ''' 
    ''' 生成具有连续条件依赖性的模拟基因表达数据:
    '''   Gene_j(t) = base_j + amplitude_j * sin(2*pi*freq_j*t + phase_j) + noise
    ''' 
    ''' 模拟生物学场景:
    '''   - 细胞状态随时间（条件）连续变化
    '''   - 不同基因有不同的表达模式（基线、振幅、频率、相位）
    '''   - 添加生物噪声模拟测量误差和细胞异质性
    ''' </summary>
    Public Class SyntheticCellDataGenerator

        ''' <summary>细胞数量</summary>
        Public Property NumCells As Integer = 1000

        ''' <summary>基因数量（细胞状态维度）</summary>
        Public Property NumGenes As Integer = 20

        ''' <summary>最小条件值（如起始时间点）</summary>
        Public Property MinTime As Double = 0.0

        ''' <summary>最大条件值（如结束时间点）</summary>
        Public Property MaxTime As Double = 1.0

        ''' <summary>噪声水平</summary>
        Public Property NoiseLevel As Double = 0.1

        ''' <summary>随机种子</summary>
        Public Property Seed As Integer? = 42

        ''' <summary>是否使用多种基因表达模式</summary>
        Public Property UseMixedPatterns As Boolean = True

        Private Random As Random

        ' 基因参数（用于生成数据）
        Private BaseExpr As Double()
        Private Amplitudes As Double()
        Private Frequencies As Double()
        Private Phases As Double()
        Private PatternTypes As Integer()  ' 0=sin, 1=linear, 2=gaussian

        ''' <summary>
        ''' 创建数据生成器
        ''' </summary>
        Public Sub New(Optional seed As Integer? = 42)
            Me.Seed = seed
            Me.Random = New Random(If(seed.HasValue, seed.Value, 42))
        End Sub

        ''' <summary>
        ''' 从标准正态分布采样
        ''' </summary>
        Private Function SampleNormal() As Double
            Dim u1 As Double = 1.0 - Random.NextDouble()
            Dim u2 As Double = 1.0 - Random.NextDouble()
            Return std.Sqrt(-2.0 * std.Log(u1)) * std.Cos(2.0 * std.PI * u2)
        End Function

        ''' <summary>
        ''' 初始化基因参数
        ''' </summary>
        Private Sub InitGeneParameters()
            ReDim BaseExpr(NumGenes - 1)
            ReDim Amplitudes(NumGenes - 1)
            ReDim Frequencies(NumGenes - 1)
            ReDim Phases(NumGenes - 1)
            ReDim PatternTypes(NumGenes - 1)

            For j As Integer = 0 To NumGenes - 1
                BaseExpr(j) = 5.0 + Random.NextDouble() * 10.0       ' 基线表达 5-15
                Amplitudes(j) = 2.0 + Random.NextDouble() * 5.0       ' 振幅 2-7
                Frequencies(j) = 0.5 + Random.NextDouble() * 2.0      ' 频率 0.5-2.5
                Phases(j) = Random.NextDouble() * 2.0 * std.PI        ' 相位 0-2pi

                If UseMixedPatterns Then
                    PatternTypes(j) = Random.Next(0, 3)  ' 0=sin, 1=linear, 2=gaussian
                Else
                    PatternTypes(j) = 0  ' 全部使用sin模式
                End If
            Next
        End Sub

        ''' <summary>
        ''' 计算指定基因在指定条件下的表达值（不含噪声）
        ''' </summary>
        Private Function GeneExpression(j As Integer, t As Double) As Double
            Select Case PatternTypes(j)
                Case 0  ' 正弦模式
                    Return BaseExpr(j) + Amplitudes(j) * std.Sin(2.0 * std.PI * Frequencies(j) * t + Phases(j))
                Case 1  ' 线性模式
                    Dim slope As Double = Amplitudes(j) * (std.Cos(Phases(j)) * 2.0 - 1.0)
                    Return BaseExpr(j) + slope * t
                Case 2  ' 高斯峰模式
                    Dim center As Double = 0.3 + 0.4 * (Phases(j) / (2.0 * std.PI))
                    Dim width As Double = 0.1 + 0.05 * Frequencies(j)
                    Return BaseExpr(j) + Amplitudes(j) * std.Exp(-((t - center) * (t - center)) / (2.0 * width * width))
                Case Else
                    Return BaseExpr(j)
            End Select
        End Function

        ''' <summary>
        ''' 生成模拟细胞状态数据
        ''' </summary>
        ''' <returns>(data: (nCells, nGenes)基因表达矩阵, conditions: (nCells,)条件数组)</returns>
        Public Function Generate() As (data As Double(,), conditions As Double())
            InitGeneParameters()

            Dim data(NumCells - 1, NumGenes - 1) As Double
            Dim conditions(NumCells - 1) As Double

            For i As Integer = 0 To NumCells - 1
                ' 采样条件值（时间点）
                Dim t As Double = MinTime + (MaxTime - MinTime) * Random.NextDouble()
                conditions(i) = t

                ' 生成基因表达
                For j As Integer = 0 To NumGenes - 1
                    Dim expr As Double = GeneExpression(j, t)
                    ' 添加高斯噪声
                    expr += SampleNormal() * NoiseLevel * BaseExpr(j)
                    ' 确保非负（基因表达量）
                    If expr < 0 Then expr = 0.0
                    data(i, j) = expr
                Next
            Next

            Return (data, conditions)
        End Function

        ''' <summary>
        ''' 生成指定条件下的真实表达值（无噪声，用于评估）
        ''' </summary>
        Public Function GenerateTrueExpression(conditions As Double()) As Double(,)
            Dim nConds As Integer = conditions.Length
            Dim result(nConds - 1, NumGenes - 1) As Double

            For i As Integer = 0 To nConds - 1
                For j As Integer = 0 To NumGenes - 1
                    result(i, j) = GeneExpression(j, conditions(i))
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 获取基因参数信息（用于调试和可视化）
        ''' </summary>
        Public Function GetGeneInfo() As List(Of (geneIdx As Integer, baseExpr As Double, amplitude As Double,
                                                   frequency As Double, phase As Double, patternType As String))
            Dim info As New List(Of (Integer, Double, Double, Double, Double, String))
            For j As Integer = 0 To NumGenes - 1
                Dim patternName As String = ""
                Select Case PatternTypes(j)
                    Case 0 : patternName = "sin"
                    Case 1 : patternName = "linear"
                    Case 2 : patternName = "gaussian"
                End Select
                info.Add((j, BaseExpr(j), Amplitudes(j), Frequencies(j), Phases(j), patternName))
            Next
            Return info
        End Function

    End Class

#End Region

End Namespace

