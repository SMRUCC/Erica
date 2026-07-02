#Region "Microsoft.VisualBasic::CVAE, Data_science\MachineLearning\CVAE\CVAE.vb"

' Author:
' 
'       CVAE Implementation for Single-Cell Time-Series Interpolation
'       基于条件变分自编码器（CVAE）的单细胞转录组时间序列插值算法模块
' 
' Copyright (c) 2024 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.



' /********************************************************************************/

' Summaries:

' Code Statistics:

'   Module Activations
'       Functions: ReLU, ReLUDerivative, Sigmoid, SigmoidDerivative, LeakyReLU
' 
'   Class LinearLayer
'       Functions: Forward, Backward, UpdateParameters, ZeroGradients
'       Properties: Weights, Bias, WeightGrad, BiasGrad
' 
'   Class LayerNormLayer
'       Functions: Forward, Backward, UpdateParameters, ZeroGradients
'       Properties: Gamma, Beta, GammaGrad, BetaGrad
' 
'   Class ReLULayer
'       Functions: Forward, Backward
' 
'   Class AdamOptimizer
'       Sub: UpdateParameter
'       Properties: LearningRate, Beta1, Beta2, Epsilon
' 
'   Class CVAE
'       Functions: Encode, Reparameterize, Decode, Forward, Backward
'                  ComputeLoss, UpdateParameters, Save, Load, InterpolateTimePoint
'       Properties: InputDim, LatentDim, ConditionDim, HiddenDim1, HiddenDim2
' 
'   Class DataPreprocessor
'       Functions: NormalizeAndLog, SelectHVG, NormalizeTimeLabels, InverseTransform
'                  DenormalizeTimeLabel, GetProcessedData
' 
'   Class CVAETrainer
'       Functions: Train, Evaluate, TrainEpoch
'       Properties: BatchSize, LearningRate, Epochs, Beta
' 
'   Class TimeSeriesInterpolator
'       Functions: Interpolate, GenerateTargetTimePoints, InterpolateSingleTime
'       Properties: Strategy, NumSamplesPerTime
' 
'   Class InterpolationResult
'       Properties: Data, TimeLabels, UniqueTimePoints, CellsPerTimePoint
' 
'   Module CVAEDemo
'       Sub: RunDemo, GenerateSyntheticData
' 
' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.MachineLearning.TensorFlow
Imports std = System.Math

Namespace MachineLearning.CVAE

    ''' <summary>
    ''' 条件变分自编码器（CVAE）命名空间
    ''' 包含用于单细胞转录组时间序列插值的完整CVAE算法实现
    ''' 
    ''' 算法原理：
    '''   CVAE通过引入条件变量c（时间标签），将变分自编码器扩展为条件生成模型。
    '''   编码器将输入数据x和条件c映射到潜在空间的分布参数（均值μ和方差σ²），
    '''   通过重参数化技巧采样潜在向量z，解码器再将z和条件c映射回数据空间。
    '''   
    '''   损失函数 = 重建损失（MSE） + β × KL散度
    '''   
    '''   训练完成后，可通过改变条件c来实现时间序列插值：
    '''   将真实细胞编码到潜在空间，再用新的时间条件解码，生成插值时间点的数据。
    ''' </summary>
    Friend Class NamespaceDoc
    End Class

#Region "激活函数模块"

    ''' <summary>
    ''' 激活函数模块
    ''' 提供神经网络中常用的激活函数及其导数
    ''' </summary>
    Public Module Activations

        ''' <summary>
        ''' ReLU激活函数：f(x) = max(0, x)
        ''' 适用于隐藏层，能有效缓解梯度消失问题
        ''' </summary>
        Public Function ReLU(x As Double) As Double
            Return If(x > 0, x, 0.0)
        End Function

        ''' <summary>
        ''' ReLU的导数：f'(x) = 1 if x > 0, else 0
        ''' </summary>
        Public Function ReLUDerivative(x As Double) As Double
            Return If(x > 0, 1.0, 0.0)
        End Function

        ''' <summary>
        ''' LeakyReLU激活函数：f(x) = x if x > 0, else alpha * x
        ''' 解决ReLU在负区间的"神经元死亡"问题
        ''' </summary>
        Public Function LeakyReLU(x As Double, Optional alpha As Double = 0.01) As Double
            Return If(x > 0, x, alpha * x)
        End Function

        ''' <summary>
        ''' Sigmoid激活函数：f(x) = 1 / (1 + e^(-x))
        ''' 将输出压缩到(0, 1)区间
        ''' </summary>
        Public Function Sigmoid(x As Double) As Double
            If x >= 0 Then
                Return 1.0 / (1.0 + std.Exp(-x))
            Else
                Dim expX = std.Exp(x)
                Return expX / (1.0 + expX)
            End If
        End Function

        ''' <summary>
        ''' Sigmoid的导数：f'(x) = f(x) * (1 - f(x))
        ''' </summary>
        Public Function SigmoidDerivative(x As Double) As Double
            Dim s = Sigmoid(x)
            Return s * (1.0 - s)
        End Function

    End Module

#End Region

#Region "神经网络层"

    ''' <summary>
    ''' 全连接层（线性层）
    ''' 实现 y = x * W + b 的线性变换，包含前向传播和反向传播
    ''' 
    ''' 权重矩阵W的形状为 (in_features, out_features)
    ''' 偏置向量b的形状为 (1, out_features)
    ''' 输入x的形状为 (batch_size, in_features)
    ''' 输出y的形状为 (batch_size, out_features)
    ''' </summary>
    Public Class LinearLayer

        ''' <summary>权重矩阵，形状 (in_features, out_features)</summary>
        Public Weights As Tensor

        ''' <summary>偏置向量，形状 (1, out_features)</summary>
        Public Bias As Tensor

        ''' <summary>权重梯度</summary>
        Public WeightGrad As Tensor

        ''' <summary>偏置梯度</summary>
        Public BiasGrad As Tensor

        ' ===== Adam优化器状态 =====
        Public WeightM As Tensor  ' 一阶矩估计
        Public WeightV As Tensor  ' 二阶矩估计
        Public BiasM As Tensor
        Public BiasV As Tensor

        ''' <summary>前向传播时缓存的输入，用于反向传播</summary>
        Public InputCache As Tensor

        ''' <summary>
        ''' 创建全连接层
        ''' </summary>
        ''' <param name="inFeatures">输入特征维度</param>
        ''' <param name="outFeatures">输出特征维度</param>
        ''' <param name="seed">随机种子（可选，用于可复现性）</param>
        Public Sub New(inFeatures As Integer, outFeatures As Integer, Optional seed As Integer? = Nothing)
            ' 使用He初始化（适用于ReLU激活函数）
            Weights = Tensor.HeInit(inFeatures, outFeatures, seed)
            Bias = Tensor.Zeros({1, outFeatures})
            WeightGrad = Tensor.Zeros({inFeatures, outFeatures})
            BiasGrad = Tensor.Zeros({1, outFeatures})
            WeightM = Tensor.Zeros({inFeatures, outFeatures})
            WeightV = Tensor.Zeros({inFeatures, outFeatures})
            BiasM = Tensor.Zeros({1, outFeatures})
            BiasV = Tensor.Zeros({1, outFeatures})
        End Sub

        ''' <summary>
        ''' 前向传播：output = input * weights + bias
        ''' </summary>
        ''' <param name="input">输入张量，形状 (batch, in_features)</param>
        ''' <returns>输出张量，形状 (batch, out_features)</returns>
        Public Function Forward(input As Tensor) As Tensor
            ' 缓存输入用于反向传播
            InputCache = input

            ' 矩阵乘法：input (batch, in) * weights (in, out) = output (batch, out)
            Dim output = input.MatMul(Weights)

            ' 加上偏置（对每一行加相同的偏置向量）
            Dim batch = output.Shape(0)
            Dim outFeatures = output.Shape(1)
            For i = 0 To batch - 1
                For j = 0 To outFeatures - 1
                    output(i, j) += Bias(0, j)
                Next
            Next

            Return output
        End Function

        ''' <summary>
        ''' 反向传播：计算梯度
        ''' </summary>
        ''' <param name="gradOutput">损失对输出的梯度，形状 (batch, out_features)</param>
        ''' <returns>损失对输入的梯度，形状 (batch, in_features)</returns>
        Public Function Backward(gradOutput As Tensor) As Tensor
            Dim batch = gradOutput.Shape(0)
            Dim inFeatures = Weights.Shape(0)
            Dim outFeatures = Weights.Shape(1)

            ' 计算输入梯度：grad_input = grad_output * weights^T
            Dim weightsT = Weights.Transpose()
            Dim gradInput = gradOutput.MatMul(weightsT)

            ' 计算权重梯度：grad_weights = input^T * grad_output
            Dim inputT = InputCache.Transpose()
            WeightGrad = inputT.MatMul(gradOutput)

            ' 计算偏置梯度：grad_bias = sum over batch of grad_output
            BiasGrad = gradOutput.Sum(0)

            Return gradInput
        End Function

        ''' <summary>
        ''' 使用Adam优化器更新参数
        ''' </summary>
        Public Sub UpdateParameters(optimizer As AdamOptimizer)
            optimizer.UpdateParameter(Weights, WeightGrad, WeightM, WeightV)
            optimizer.UpdateParameter(Bias, BiasGrad, BiasM, BiasV)
        End Sub

    End Class

    ''' <summary>
    ''' 层归一化（Layer Normalization）
    ''' 对每个样本的所有特征进行归一化，消除内部协变量偏移
    ''' 
    ''' 对于每个样本i：
    '''   mean_i = mean(x_i)
    '''   var_i = var(x_i)
    '''   x_hat_i = (x_i - mean_i) / sqrt(var_i + eps)
    '''   y_i = gamma * x_hat_i + beta
    ''' </summary>
    Public Class LayerNormLayer

        Public Gamma As Tensor    ' 缩放参数，形状 (1, num_features)
        Public Beta As Tensor     ' 平移参数，形状 (1, num_features)
        Public GammaGrad As Tensor
        Public BetaGrad As Tensor

        ' Adam优化器状态
        Public GammaM As Tensor
        Public GammaV As Tensor
        Public BetaM As Tensor
        Public BetaV As Tensor

        Public Epsilon As Double = 0.00001

        ' 反向传播所需的缓存
        Private InputCache As Tensor
        Private MeanCache As Double()
        Private StdCache As Double()
        Private NormalizedCache As Tensor

        ''' <summary>
        ''' 创建层归一化层
        ''' </summary>
        ''' <param name="numFeatures">特征数量</param>
        Public Sub New(numFeatures As Integer)
            Gamma = Tensor.Ones({1, numFeatures})
            Beta = Tensor.Zeros({1, numFeatures})
            GammaGrad = Tensor.Zeros({1, numFeatures})
            BetaGrad = Tensor.Zeros({1, numFeatures})
            GammaM = Tensor.Zeros({1, numFeatures})
            GammaV = Tensor.Zeros({1, numFeatures})
            BetaM = Tensor.Zeros({1, numFeatures})
            BetaV = Tensor.Zeros({1, numFeatures})
        End Sub

        ''' <summary>
        ''' 前向传播
        ''' </summary>
        Public Function Forward(input As Tensor) As Tensor
            Dim batch = input.Shape(0)
            Dim features = input.Shape(1)

            InputCache = input
            MeanCache = New Double(batch - 1) {}
            StdCache = New Double(batch - 1) {}
            NormalizedCache = New Tensor(batch, features)

            Dim output = New Tensor(batch, features)

            For i = 0 To batch - 1
                ' 计算均值
                Dim mean = 0.0
                For j = 0 To features - 1
                    mean += input(i, j)
                Next
                mean /= features
                MeanCache(i) = mean

                ' 计算方差和标准差
                Dim variance = 0.0
                For j = 0 To features - 1
                    variance += (input(i, j) - mean) * (input(i, j) - mean)
                Next
                variance /= features
                StdCache(i) = std.Sqrt(variance + Epsilon)

                ' 归一化并应用缩放和平移
                For j = 0 To features - 1
                    Dim normalized = (input(i, j) - mean) / StdCache(i)
                    NormalizedCache(i, j) = normalized
                    output(i, j) = Gamma(0, j) * normalized + Beta(0, j)
                Next
            Next

            Return output
        End Function

        ''' <summary>
        ''' 反向传播
        ''' 推导过程：
        '''   grad_gamma_j = sum_i(grad_y_ij * x_hat_ij)
        '''   grad_beta_j = sum_i(grad_y_ij)
        '''   grad_x_ij = (1 / (D * std_i)) * (D * grad_xhat_ij - sum_j(grad_xhat_ij) - x_hat_ij * sum_j(grad_xhat_ij * x_hat_ij))
        '''   其中 grad_xhat_ij = grad_y_ij * gamma_j
        ''' </summary>
        Public Function Backward(gradOutput As Tensor) As Tensor
            Dim batch = gradOutput.Shape(0)
            Dim features = gradOutput.Shape(1)
            Dim gradInput = New Tensor(batch, features)

            ' 计算gamma和beta的梯度
            For j = 0 To features - 1
                Dim gammaGrad = 0.0
                Dim betaGrad = 0.0
                For i = 0 To batch - 1
                    gammaGrad += gradOutput(i, j) * NormalizedCache(i, j)
                    betaGrad += gradOutput(i, j)
                Next
                Me.GammaGrad(0, j) = gammaGrad
                Me.BetaGrad(0, j) = betaGrad
            Next

            ' 计算输入梯度
            For i = 0 To batch - 1
                Dim sumGradXhat = 0.0
                Dim sumGradXhatXhat = 0.0
                For j = 0 To features - 1
                    Dim gradXhat = gradOutput(i, j) * Gamma(0, j)
                    sumGradXhat += gradXhat
                    sumGradXhatXhat += gradXhat * NormalizedCache(i, j)
                Next

                For j = 0 To features - 1
                    Dim gradXhat = gradOutput(i, j) * Gamma(0, j)
                    gradInput(i, j) = (gradXhat - sumGradXhat / features - NormalizedCache(i, j) * sumGradXhatXhat / features) / StdCache(i)
                Next
            Next

            Return gradInput
        End Function

        ''' <summary>
        ''' 使用Adam优化器更新参数
        ''' </summary>
        Public Sub UpdateParameters(optimizer As AdamOptimizer)
            optimizer.UpdateParameter(Gamma, GammaGrad, GammaM, GammaV)
            optimizer.UpdateParameter(Beta, BetaGrad, BetaM, BetaV)
        End Sub

    End Class

    ''' <summary>
    ''' ReLU激活层
    ''' 对输入逐元素应用ReLU激活函数
    ''' </summary>
    Public Class ReLULayer

        ''' <summary>前向传播时缓存的输入</summary>
        Private InputCache As Tensor

        ''' <summary>
        ''' 前向传播：对每个元素应用ReLU
        ''' </summary>
        Public Function Forward(input As Tensor) As Tensor
            InputCache = input
            Dim result = New Tensor(input.Shape)
            For i = 0 To input.Length - 1
                result.Data(i) = If(input.Data(i) > 0, input.Data(i), 0.0)
            Next
            Return result
        End Function

        ''' <summary>
        ''' 反向传播：ReLU的导数为1（当输入>0）或0（当输入&lt;=0）
        ''' </summary>
        Public Function Backward(gradOutput As Tensor) As Tensor
            Dim result = New Tensor(gradOutput.Shape)
            For i = 0 To gradOutput.Length - 1
                result.Data(i) = If(InputCache.Data(i) > 0, gradOutput.Data(i), 0.0)
            Next
            Return result
        End Function

    End Class

#End Region

#Region "Adam优化器"

    ''' <summary>
    ''' Adam优化器（Adaptive Moment Estimation）
    ''' 结合了动量法和自适应学习率的优点
    ''' 
    ''' 算法：
    '''   m_t = β₁ * m_{t-1} + (1 - β₁) * g_t          (一阶矩估计)
    '''   v_t = β₂ * v_{t-1} + (1 - β₂) * g_t²         (二阶矩估计)
    '''   m_hat = m_t / (1 - β₁^t)                       (偏差修正)
    '''   v_hat = v_t / (1 - β₂^t)                       (偏差修正)
    '''   θ_t = θ_{t-1} - lr * m_hat / (√v_hat + ε)     (参数更新)
    ''' </summary>
    Public Class AdamOptimizer

        ''' <summary>学习率（默认0.001）</summary>
        Public Property LearningRate As Double = 0.001

        ''' <summary>一阶矩衰减率（默认0.9）</summary>
        Public Property Beta1 As Double = 0.9

        ''' <summary>二阶矩衰减率（默认0.999）</summary>
        Public Property Beta2 As Double = 0.999

        ''' <summary>数值稳定性的小常数（默认1e-8）</summary>
        Public Property Epsilon As Double = 0.00000001

        ''' <summary>时间步计数器</summary>
        Public T As Integer = 0

        ''' <summary>
        ''' 创建Adam优化器
        ''' </summary>
        Public Sub New(Optional learningRate As Double = 0.001,
                       Optional beta1 As Double = 0.9,
                       Optional beta2 As Double = 0.999,
                       Optional epsilon As Double = 0.00000001)
            Me.LearningRate = learningRate
            Me.Beta1 = beta1
            Me.Beta2 = beta2
            Me.Epsilon = epsilon
        End Sub

        ''' <summary>
        ''' 更新参数
        ''' </summary>
        ''' <param name="param">要更新的参数张量</param>
        ''' <param name="grad">梯度张量</param>
        ''' <param name="m">一阶矩估计（会被修改）</param>
        ''' <param name="v">二阶矩估计（会被修改）</param>
        Public Sub UpdateParameter(param As Tensor, grad As Tensor, ByRef m As Tensor, ByRef v As Tensor)
            T += 1
            Dim biasCorrection1 = 1.0 - std.Pow(Beta1, T)
            Dim biasCorrection2 = 1.0 - std.Pow(Beta2, T)

            For i = 0 To param.Length - 1
                Dim g = grad.Data(i)

                ' 更新一阶矩和二阶矩
                m.Data(i) = Beta1 * m.Data(i) + (1.0 - Beta1) * g
                v.Data(i) = Beta2 * v.Data(i) + (1.0 - Beta2) * g * g

                ' 偏差修正
                Dim mHat = m.Data(i) / biasCorrection1
                Dim vHat = v.Data(i) / biasCorrection2

                ' 更新参数
                param.Data(i) -= LearningRate * mHat / (std.Sqrt(vHat) + Epsilon)
            Next
        End Sub

    End Class

#End Region

#Region "CVAE模型"

    ''' <summary>
    ''' 条件变分自编码器（Conditional Variational Autoencoder, CVAE）
    ''' 
    ''' 网络架构：
    '''   编码器：[x; c] → Linear(input+cond, 512) → LayerNorm → ReLU 
    '''          → Linear(512, 256) → LayerNorm → ReLU 
    '''          → [FcMu(256, latent), FcVar(256, latent)]
    '''   
    '''   重参数化：z = μ + ε * σ, 其中 σ = exp(0.5 * log_var), ε ~ N(0, 1)
    '''   
    '''   解码器：[z; c] → Linear(latent+cond, 256) → LayerNorm → ReLU 
    '''          → Linear(256, 512) → LayerNorm → ReLU 
    '''          → Linear(512, input) → x_recon
    ''' 
    ''' 损失函数：
    '''   L = MSE(x_recon, x) + β * KL(q(z|x,c) || p(z))
    '''   其中 KL = -0.5 * Σ(1 + log_var - μ² - exp(log_var))
    ''' </summary>
    Public Class CVAE

        ' ===== 模型架构参数 =====
        Public Property InputDim As Integer       ' 输入维度（基因数）
        Public Property LatentDim As Integer      ' 潜在空间维度
        Public Property ConditionDim As Integer   ' 条件维度（时间标签维度，默认1）
        Public Property HiddenDim1 As Integer = 512  ' 第一隐藏层维度
        Public Property HiddenDim2 As Integer = 256  ' 第二隐藏层维度

        ' ===== 编码器层 =====
        Public EncLinear1 As LinearLayer
        Public EncLN1 As LayerNormLayer
        Public EncReLU1 As ReLULayer
        Public EncLinear2 As LinearLayer
        Public EncLN2 As LayerNormLayer
        Public EncReLU2 As ReLULayer
        Public FcMu As LinearLayer
        Public FcLogVar As LinearLayer

        ' ===== 解码器层 =====
        Public DecLinear1 As LinearLayer
        Public DecLN1 As LayerNormLayer
        Public DecReLU1 As ReLULayer
        Public DecLinear2 As LinearLayer
        Public DecLN2 As LayerNormLayer
        Public DecReLU2 As ReLULayer
        Public DecLinear3 As LinearLayer

        ' ===== 优化器 =====
        Public Optimizer As AdamOptimizer

        ' ===== 随机数生成器（用于重参数化） =====
        Private Random As Random

        ' ===== 前向传播缓存（用于反向传播） =====
        ''' <summary>前向传播时采样的潜在向量（供外部访问）</summary>
        Public CachedZ As Tensor         ' 采样的潜在向量
        Private CachedEps As Tensor       ' 重参数化中的噪声ε
        Private CachedSigma As Tensor     ' σ = exp(0.5 * log_var)
        Private CachedMu As Tensor        ' 均值
        Private CachedLogVar As Tensor    ' 对数方差
        Private CachedC As Tensor         ' 条件

        ' ===== log_var裁剪范围（防止数值溢出） =====
        Private Const LogVarClipMin As Double = -10.0
        Private Const LogVarClipMax As Double = 10.0

        ''' <summary>
        ''' 创建CVAE模型
        ''' </summary>
        ''' <param name="inputDim">输入维度（基因数）</param>
        ''' <param name="latentDim">潜在空间维度（默认32）</param>
        ''' <param name="conditionDim">条件维度（默认1，即标量时间）</param>
        ''' <param name="seed">随机种子</param>
        Public Sub New(inputDim As Integer,
                       Optional latentDim As Integer = 32,
                       Optional conditionDim As Integer = 1,
                       Optional seed As Integer? = Nothing)
            Me.InputDim = inputDim
            Me.LatentDim = latentDim
            Me.ConditionDim = conditionDim
            Me.Random = New Random(If(seed.HasValue, seed.Value, 42))

            ' 根据输入维度自适应调整隐藏层大小
            If inputDim < 256 Then
                HiddenDim1 = std.Max(128, inputDim)
                HiddenDim2 = std.Max(64, inputDim \ 2)
            End If

            Dim seedVal = If(seed.HasValue, seed.Value, 42)

            ' ===== 初始化编码器 =====
            ' 输入: [x; c], 维度 = inputDim + conditionDim
            EncLinear1 = New LinearLayer(inputDim + conditionDim, HiddenDim1, seedVal + 1)
            EncLN1 = New LayerNormLayer(HiddenDim1)
            EncReLU1 = New ReLULayer()

            EncLinear2 = New LinearLayer(HiddenDim1, HiddenDim2, seedVal + 2)
            EncLN2 = New LayerNormLayer(HiddenDim2)
            EncReLU2 = New ReLULayer()

            ' 均值和方差分支
            FcMu = New LinearLayer(HiddenDim2, latentDim, seedVal + 3)
            FcLogVar = New LinearLayer(HiddenDim2, latentDim, seedVal + 4)

            ' ===== 初始化解码器 =====
            ' 输入: [z; c], 维度 = latentDim + conditionDim
            DecLinear1 = New LinearLayer(latentDim + conditionDim, HiddenDim2, seedVal + 5)
            DecLN1 = New LayerNormLayer(HiddenDim2)
            DecReLU1 = New ReLULayer()

            DecLinear2 = New LinearLayer(HiddenDim2, HiddenDim1, seedVal + 6)
            DecLN2 = New LayerNormLayer(HiddenDim1)
            DecReLU2 = New ReLULayer()

            DecLinear3 = New LinearLayer(HiddenDim1, inputDim, seedVal + 7)

            ' 初始化优化器
            Optimizer = New AdamOptimizer(learningRate:=0.001)
        End Sub

        ''' <summary>
        ''' 沿特征维度拼接两个张量
        ''' a: (batch, dim_a), b: (batch, dim_b) → result: (batch, dim_a + dim_b)
        ''' </summary>
        Private Function ConcatenateFeatures(a As Tensor, b As Tensor) As Tensor
            If a.Shape(0) <> b.Shape(0) Then
                Throw New ArgumentException($"批次大小不匹配: {a.Shape(0)} vs {b.Shape(0)}")
            End If

            Dim batch = a.Shape(0)
            Dim colsA = a.Shape(1)
            Dim colsB = b.Shape(1)
            Dim result = New Tensor(batch, colsA + colsB)

            For i = 0 To batch - 1
                For j = 0 To colsA - 1
                    result(i, j) = a(i, j)
                Next
                For j = 0 To colsB - 1
                    result(i, colsA + j) = b(i, j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 编码：将输入数据和条件映射到潜在空间的分布参数
        ''' </summary>
        ''' <param name="x">输入数据，形状 (batch, inputDim)</param>
        ''' <param name="c">条件，形状 (batch, conditionDim)</param>
        ''' <returns>均值μ和对数方差log_var，形状均为 (batch, latentDim)</returns>
        Public Function Encode(x As Tensor, c As Tensor) As (mu As Tensor, logVar As Tensor)
            ' 拼接输入和条件
            Dim xc = ConcatenateFeatures(x, c)

            ' 第一层
            Dim h = EncLinear1.Forward(xc)
            h = EncLN1.Forward(h)
            h = EncReLU1.Forward(h)

            ' 第二层
            h = EncLinear2.Forward(h)
            h = EncLN2.Forward(h)
            h = EncReLU2.Forward(h)

            ' 均值和方差分支
            Dim mu = FcMu.Forward(h)
            Dim logVar = FcLogVar.Forward(h)

            ' 裁剪log_var防止数值溢出
            For i = 0 To logVar.Length - 1
                If logVar.Data(i) < LogVarClipMin Then logVar.Data(i) = LogVarClipMin
                If logVar.Data(i) > LogVarClipMax Then logVar.Data(i) = LogVarClipMax
            Next

            Return (mu, logVar)
        End Function

        ''' <summary>
        ''' 重参数化技巧：z = μ + ε * σ, 其中 σ = exp(0.5 * log_var), ε ~ N(0, 1)
        ''' 
        ''' 这个技巧使得随机采样过程可微，从而可以使用反向传播训练
        ''' </summary>
        Public Function Reparameterize(mu As Tensor, logVar As Tensor) As Tensor
            Dim batch = mu.Shape(0)
            Dim z = New Tensor(batch, LatentDim)
            Dim eps = New Tensor(batch, LatentDim)
            Dim sigma = New Tensor(batch, LatentDim)

            For i = 0 To mu.Length - 1
                ' 计算标准差 σ = exp(0.5 * log_var)
                sigma.Data(i) = std.Exp(0.5 * logVar.Data(i))

                ' Box-Muller变换生成标准正态分布随机数
                Dim u1 = 1.0 - Random.NextDouble()
                Dim u2 = 1.0 - Random.NextDouble()
                Dim normalRand = std.Sqrt(-2.0 * std.Log(u1)) * std.Cos(2.0 * std.PI * u2)
                eps.Data(i) = normalRand

                ' z = μ + ε * σ
                z.Data(i) = mu.Data(i) + normalRand * sigma.Data(i)
            Next

            ' 缓存用于反向传播
            CachedEps = eps
            CachedSigma = sigma

            Return z
        End Function

        ''' <summary>
        ''' 解码：将潜在向量和条件映射回数据空间
        ''' </summary>
        ''' <param name="z">潜在向量，形状 (batch, latentDim)</param>
        ''' <param name="c">条件，形状 (batch, conditionDim)</param>
        ''' <returns>重建数据，形状 (batch, inputDim)</returns>
        Public Function Decode(z As Tensor, c As Tensor) As Tensor
            ' 拼接潜在向量和条件
            Dim zc = ConcatenateFeatures(z, c)

            ' 第一层
            Dim h = DecLinear1.Forward(zc)
            h = DecLN1.Forward(h)
            h = DecReLU1.Forward(h)

            ' 第二层
            h = DecLinear2.Forward(h)
            h = DecLN2.Forward(h)
            h = DecReLU2.Forward(h)

            ' 输出层
            Dim xRecon = DecLinear3.Forward(h)

            Return xRecon
        End Function

        ''' <summary>
        ''' 前向传播：编码 → 重参数化 → 解码
        ''' </summary>
        ''' <param name="x">输入数据，形状 (batch, inputDim)</param>
        ''' <param name="c">条件，形状 (batch, conditionDim)</param>
        ''' <returns>重建数据、均值、对数方差</returns>
        Public Function Forward(x As Tensor, c As Tensor) As (xRecon As Tensor, mu As Tensor, logVar As Tensor)
            ' 缓存条件用于反向传播
            CachedC = c

            ' 编码
            Dim encoded = Encode(x, c)
            CachedMu = encoded.mu
            CachedLogVar = encoded.logVar

            ' 重参数化
            CachedZ = Reparameterize(encoded.mu, encoded.logVar)

            ' 解码
            Dim xRecon = Decode(CachedZ, c)

            Return (xRecon, encoded.mu, encoded.logVar)
        End Function

        ''' <summary>
        ''' 计算损失：重建损失（MSE） + β × KL散度
        ''' </summary>
        ''' <param name="xRecon">重建数据</param>
        ''' <param name="x">原始数据</param>
        ''' <param name="mu">均值</param>
        ''' <param name="logVar">对数方差</param>
        ''' <param name="beta">KL散度权重（β-VAE参数）</param>
        ''' <returns>总损失、重建损失、KL损失</returns>
        Public Function ComputeLoss(xRecon As Tensor, x As Tensor, mu As Tensor, logVar As Tensor, Optional beta As Double = 1.0) As (totalLoss As Double, reconLoss As Double, klLoss As Double)
            Dim batch = x.Shape(0)
            Dim features = x.Shape(1)

            ' ===== 重建损失：MSE = (1/batch) * Σ(x_recon - x)² =====
            Dim reconLoss = 0.0
            For i = 0 To batch - 1
                For j = 0 To features - 1
                    Dim diff = xRecon(i, j) - x(i, j)
                    reconLoss += diff * diff
                Next
            Next
            reconLoss /= batch

            ' ===== KL散度：KL = -0.5 * Σ(1 + log_var - μ² - exp(log_var)) =====
            Dim klLoss = 0.0
            For i = 0 To mu.Length - 1
                Dim muVal = mu.Data(i)
                Dim logVarVal = logVar.Data(i)
                klLoss += -0.5 * (1.0 + logVarVal - muVal * muVal - std.Exp(logVarVal))
            Next
            klLoss /= batch

            Dim totalLoss = reconLoss + beta * klLoss

            Return (totalLoss, reconLoss, klLoss)
        End Function

        ''' <summary>
        ''' 反向传播：计算所有参数的梯度
        ''' 
        ''' 反向传播路径：
        '''   1. 重建损失梯度 → 解码器 → 潜在向量z
        '''   2. z的梯度通过重参数化传递到μ和log_var
        '''   3. KL散度对μ和log_var的梯度
        '''   4. 合并梯度 → 编码器
        ''' </summary>
        Public Sub Backward(xRecon As Tensor, x As Tensor, mu As Tensor, logVar As Tensor, z As Tensor, c As Tensor, Optional beta As Double = 1.0)
            Dim batch = x.Shape(0)
            Dim features = x.Shape(1)

            ' ===== 1. 重建损失对x_recon的梯度: d(MSE)/d(x_recon) = 2 * (x_recon - x) / batch =====
            Dim gradXRecon = New Tensor(xRecon.Shape)
            For i = 0 To batch - 1
                For j = 0 To features - 1
                    gradXRecon(i, j) = 2.0 * (xRecon(i, j) - x(i, j)) / batch
                Next
            Next

            ' ===== 2. 反向传播通过解码器 =====
            Dim grad = DecLinear3.Backward(gradXRecon)
            grad = DecReLU2.Backward(grad)
            grad = DecLN2.Backward(grad)
            grad = DecLinear2.Backward(grad)
            grad = DecReLU1.Backward(grad)
            grad = DecLN1.Backward(grad)
            Dim gradZC = DecLinear1.Backward(grad)  ' 形状: (batch, latentDim + conditionDim)

            ' 提取z的梯度（前latentDim列），c的梯度不需要
            Dim gradZ = New Tensor(z.Shape)
            For i = 0 To batch - 1
                For j = 0 To LatentDim - 1
                    gradZ(i, j) = gradZC(i, j)
                Next
            Next

            ' ===== 3. 重参数化反向传播 =====
            ' z = μ + ε * σ, 其中 σ = exp(0.5 * log_var)
            ' dz/dμ = 1, dz/d(log_var) = ε * 0.5 * σ
            Dim gradMu = New Tensor(mu.Shape)
            Dim gradLogVar = New Tensor(logVar.Shape)

            For i = 0 To mu.Length - 1
                gradMu.Data(i) = gradZ.Data(i)
                gradLogVar.Data(i) = gradZ.Data(i) * CachedEps.Data(i) * 0.5 * CachedSigma.Data(i)
            Next

            ' ===== 4. KL散度梯度 =====
            ' KL = -0.5 * Σ(1 + log_var - μ² - exp(log_var))
            ' dKL/dμ = μ
            ' dKL/d(log_var) = -0.5 * (1 - exp(log_var)) = 0.5 * (exp(log_var) - 1)
            For i = 0 To mu.Length - 1
                gradMu.Data(i) += beta * mu.Data(i) / batch
                gradLogVar.Data(i) += beta * 0.5 * (std.Exp(logVar.Data(i)) - 1.0) / batch
            Next

            ' ===== 5. 反向传播通过编码器的μ和log_var分支 =====
            Dim gradHFromMu = FcMu.Backward(gradMu)
            Dim gradHFromVar = FcLogVar.Backward(gradLogVar)

            ' 合并梯度（μ和log_var来自同一个隐藏层）
            Dim gradH = New Tensor(gradHFromMu.Shape)
            For i = 0 To gradH.Length - 1
                gradH.Data(i) = gradHFromMu.Data(i) + gradHFromVar.Data(i)
            Next

            ' ===== 6. 继续反向传播通过编码器 =====
            gradH = EncReLU2.Backward(gradH)
            gradH = EncLN2.Backward(gradH)
            gradH = EncLinear2.Backward(gradH)
            gradH = EncReLU1.Backward(gradH)
            gradH = EncLN1.Backward(gradH)
            ' EncLinear1.Backward(gradH) 的结果是对输入的梯度，不需要
            EncLinear1.Backward(gradH)
        End Sub

        ''' <summary>
        ''' 更新所有参数
        ''' </summary>
        Public Sub UpdateParameters()
            ' 编码器
            EncLinear1.UpdateParameters(Optimizer)
            EncLinear2.UpdateParameters(Optimizer)
            FcMu.UpdateParameters(Optimizer)
            FcLogVar.UpdateParameters(Optimizer)
            EncLN1.UpdateParameters(Optimizer)
            EncLN2.UpdateParameters(Optimizer)

            ' 解码器
            DecLinear1.UpdateParameters(Optimizer)
            DecLinear2.UpdateParameters(Optimizer)
            DecLinear3.UpdateParameters(Optimizer)
            DecLN1.UpdateParameters(Optimizer)
            DecLN2.UpdateParameters(Optimizer)
        End Sub

        ''' <summary>
        ''' 对单个时间点进行插值（策略2：基于真实细胞的轨迹推进）
        ''' 
        ''' 将真实细胞编码到潜在空间，再用新的时间条件解码，
        ''' 生成该时间点的插值数据
        ''' </summary>
        ''' <param name="realCells">真实细胞数据，形状 (n_cells, inputDim)</param>
        ''' <param name="originalTime">原始时间标签（归一化后的标量）</param>
        ''' <param name="targetTime">目标时间标签（归一化后的标量）</param>
        ''' <returns>插值后的细胞数据，形状 (n_cells, inputDim)</returns>
        Public Function InterpolateTimePoint(realCells As Tensor, originalTime As Double, targetTime As Double) As Tensor
            Dim nCells = realCells.Shape(0)

            ' 构建条件张量
            Dim cOriginal = New Tensor(nCells, ConditionDim)
            Dim cTarget = New Tensor(nCells, ConditionDim)
            For i = 0 To nCells - 1
                cOriginal(i, 0) = originalTime
                cTarget(i, 0) = targetTime
            Next

            ' 编码（使用原始时间条件）
            Dim encoded = Encode(realCells, cOriginal)

            ' 直接使用均值作为潜在向量（不采样，更稳定）
            Dim z = encoded.mu

            ' 解码（使用目标时间条件）
            Dim interpolated = Decode(z, cTarget)

            Return interpolated
        End Function

        ''' <summary>
        ''' 保存模型参数到文件
        ''' </summary>
        Public Sub Save(filePath As String)
            Using writer As New System.IO.StreamWriter(filePath)
                ' 写入架构参数
                writer.WriteLine($"InputDim:{InputDim}")
                writer.WriteLine($"LatentDim:{LatentDim}")
                writer.WriteLine($"ConditionDim:{ConditionDim}")
                writer.WriteLine($"HiddenDim1:{HiddenDim1}")
                writer.WriteLine($"HiddenDim2:{HiddenDim2}")

                ' 写入所有层参数
                WriteLayer(writer, "EncLinear1_Weights", EncLinear1.Weights)
                WriteLayer(writer, "EncLinear1_Bias", EncLinear1.Bias)
                WriteLayer(writer, "EncLinear2_Weights", EncLinear2.Weights)
                WriteLayer(writer, "EncLinear2_Bias", EncLinear2.Bias)
                WriteLayer(writer, "FcMu_Weights", FcMu.Weights)
                WriteLayer(writer, "FcMu_Bias", FcMu.Bias)
                WriteLayer(writer, "FcLogVar_Weights", FcLogVar.Weights)
                WriteLayer(writer, "FcLogVar_Bias", FcLogVar.Bias)
                WriteLayer(writer, "DecLinear1_Weights", DecLinear1.Weights)
                WriteLayer(writer, "DecLinear1_Bias", DecLinear1.Bias)
                WriteLayer(writer, "DecLinear2_Weights", DecLinear2.Weights)
                WriteLayer(writer, "DecLinear2_Bias", DecLinear2.Bias)
                WriteLayer(writer, "DecLinear3_Weights", DecLinear3.Weights)
                WriteLayer(writer, "DecLinear3_Bias", DecLinear3.Bias)
                WriteLayer(writer, "EncLN1_Gamma", EncLN1.Gamma)
                WriteLayer(writer, "EncLN1_Beta", EncLN1.Beta)
                WriteLayer(writer, "EncLN2_Gamma", EncLN2.Gamma)
                WriteLayer(writer, "EncLN2_Beta", EncLN2.Beta)
                WriteLayer(writer, "DecLN1_Gamma", DecLN1.Gamma)
                WriteLayer(writer, "DecLN1_Beta", DecLN1.Beta)
                WriteLayer(writer, "DecLN2_Gamma", DecLN2.Gamma)
                WriteLayer(writer, "DecLN2_Beta", DecLN2.Beta)
            End Using
        End Sub

        ''' <summary>
        ''' 辅助方法：写入张量到文件
        ''' </summary>
        Private Sub WriteLayer(writer As System.IO.StreamWriter, name As String, tensor As Tensor)
            writer.WriteLine($"{name}:{String.Join(",", tensor.Shape)}")
            writer.WriteLine(String.Join(",", tensor.Data.Select(Function(d) d.ToString("G17"))))
        End Sub

        ''' <summary>
        ''' 从文件加载模型参数
        ''' </summary>
        Public Shared Function Load(filePath As String) As CVAE
            Dim lines = System.IO.File.ReadAllLines(filePath)
            Dim params As New Dictionary(Of String, String)
            Dim tensorData As New Dictionary(Of String, (shape As Integer(), data As Double()))

            Dim i = 0
            While i < lines.Length
                Dim line = lines(i)
                Dim colonIdx = line.IndexOf(":"c)
                If colonIdx < 0 Then
                    i += 1
                    Continue While
                End If

                Dim key = line.Substring(0, colonIdx)
                Dim value = line.Substring(colonIdx + 1)

                If key = "InputDim" OrElse key = "LatentDim" OrElse key = "ConditionDim" OrElse
                   key = "HiddenDim1" OrElse key = "HiddenDim2" Then
                    params(key) = value
                    i += 1
                Else
                    ' 这是张量数据
                    Dim shape = value.Split(","c).Select(Function(s) Integer.Parse(s.Trim())).ToArray()
                    i += 1
                    Dim data = lines(i).Split(","c).Select(Function(s) Double.Parse(s.Trim())).ToArray()
                    tensorData(key) = (shape, data)
                    i += 1
                End If
            End While

            ' 创建模型
            Dim cvae As New CVAE(
                Integer.Parse(params("InputDim")),
                Integer.Parse(params("LatentDim")),
                Integer.Parse(params("ConditionDim")))
            cvae.HiddenDim1 = Integer.Parse(params("HiddenDim1"))
            cvae.HiddenDim2 = Integer.Parse(params("HiddenDim2"))

            ' 加载参数
            cvae.EncLinear1.Weights = New Tensor(tensorData("EncLinear1_Weights").data, tensorData("EncLinear1_Weights").shape)
            cvae.EncLinear1.Bias = New Tensor(tensorData("EncLinear1_Bias").data, tensorData("EncLinear1_Bias").shape)
            cvae.EncLinear2.Weights = New Tensor(tensorData("EncLinear2_Weights").data, tensorData("EncLinear2_Weights").shape)
            cvae.EncLinear2.Bias = New Tensor(tensorData("EncLinear2_Bias").data, tensorData("EncLinear2_Bias").shape)
            cvae.FcMu.Weights = New Tensor(tensorData("FcMu_Weights").data, tensorData("FcMu_Weights").shape)
            cvae.FcMu.Bias = New Tensor(tensorData("FcMu_Bias").data, tensorData("FcMu_Bias").shape)
            cvae.FcLogVar.Weights = New Tensor(tensorData("FcLogVar_Weights").data, tensorData("FcLogVar_Weights").shape)
            cvae.FcLogVar.Bias = New Tensor(tensorData("FcLogVar_Bias").data, tensorData("FcLogVar_Bias").shape)
            cvae.DecLinear1.Weights = New Tensor(tensorData("DecLinear1_Weights").data, tensorData("DecLinear1_Weights").shape)
            cvae.DecLinear1.Bias = New Tensor(tensorData("DecLinear1_Bias").data, tensorData("DecLinear1_Bias").shape)
            cvae.DecLinear2.Weights = New Tensor(tensorData("DecLinear2_Weights").data, tensorData("DecLinear2_Weights").shape)
            cvae.DecLinear2.Bias = New Tensor(tensorData("DecLinear2_Bias").data, tensorData("DecLinear2_Bias").shape)
            cvae.DecLinear3.Weights = New Tensor(tensorData("DecLinear3_Weights").data, tensorData("DecLinear3_Weights").shape)
            cvae.DecLinear3.Bias = New Tensor(tensorData("DecLinear3_Bias").data, tensorData("DecLinear3_Bias").shape)
            cvae.EncLN1.Gamma = New Tensor(tensorData("EncLN1_Gamma").data, tensorData("EncLN1_Gamma").shape)
            cvae.EncLN1.Beta = New Tensor(tensorData("EncLN1_Beta").data, tensorData("EncLN1_Beta").shape)
            cvae.EncLN2.Gamma = New Tensor(tensorData("EncLN2_Gamma").data, tensorData("EncLN2_Gamma").shape)
            cvae.EncLN2.Beta = New Tensor(tensorData("EncLN2_Beta").data, tensorData("EncLN2_Beta").shape)
            cvae.DecLN1.Gamma = New Tensor(tensorData("DecLN1_Gamma").data, tensorData("DecLN1_Gamma").shape)
            cvae.DecLN1.Beta = New Tensor(tensorData("DecLN1_Beta").data, tensorData("DecLN1_Beta").shape)
            cvae.DecLN2.Gamma = New Tensor(tensorData("DecLN2_Gamma").data, tensorData("DecLN2_Gamma").shape)
            cvae.DecLN2.Beta = New Tensor(tensorData("DecLN2_Beta").data, tensorData("DecLN2_Beta").shape)

            Return cvae
        End Function

    End Class

#End Region

#Region "数据预处理"

    ''' <summary>
    ''' 数据预处理器
    ''' 提供单细胞转录组数据的预处理功能：
    '''   - 对数归一化
    '''   - 高变基因（HVG）选择
    '''   - 时间标签归一化
    '''   - 逆变换
    ''' </summary>
    Public Class DataPreprocessor

        ''' <summary>每个细胞的目标总表达量（归一化因子）</summary>
        Public Property TargetSum As Double = 10000.0

        ''' <summary>是否已执行对数变换</summary>
        Public Property IsLogTransformed As Boolean = False

        ''' <summary>选择的HVG索引</summary>
        Public Property SelectedGeneIndices As Integer()

        ''' <summary>原始时间标签的最小值</summary>
        Public Property MinTime As Double

        ''' <summary>原始时间标签的最大值</summary>
        Public Property MaxTime As Double

        ''' <summary>每个基因的均值（用于标准化）</summary>
        Private GeneMeans As Double()

        ''' <summary>每个基因的标准差（用于标准化）</summary>
        Private GeneStds As Double()

        ''' <summary>
        ''' 对数归一化
        ''' 1. 将每个细胞的总表达量归一化到TargetSum
        ''' 2. 对数变换：log1p(normalized)
        ''' </summary>
        ''' <param name="data">原始表达矩阵，形状 (n_cells, n_genes)</param>
        ''' <returns>归一化后的矩阵</returns>
        Public Function NormalizeAndLog(data As Double(,)) As Double(,)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)
            Dim result = New Double(nCells - 1, nGenes - 1) {}

            ' 归一化每个细胞的总表达量
            For i = 0 To nCells - 1
                Dim libSize = 0.0
                For j = 0 To nGenes - 1
                    libSize += data(i, j)
                Next

                If libSize <= 0 Then libSize = 1.0  ' 防止除零

                For j = 0 To nGenes - 1
                    ' 归一化 + 对数变换
                    result(i, j) = std.Log(1.0 + data(i, j) / libSize * TargetSum)
                Next
            Next

            IsLogTransformed = True
            Return result
        End Function

        ''' <summary>
        ''' 选择高变基因（Highly Variable Genes, HVG）
        ''' 基于基因表达的方差进行选择
        ''' </summary>
        ''' <param name="data">归一化后的表达矩阵</param>
        ''' <param name="numGenes">要选择的基因数量</param>
        ''' <returns>选择后的矩阵</returns>
        Public Function SelectHVG(data As Double(,), numGenes As Integer) As Double(,)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)

            If numGenes >= nGenes Then
                ' 不需要选择，返回全部基因
                SelectedGeneIndices = Enumerable.Range(0, nGenes).ToArray()
                Return data
            End If

            ' 计算每个基因的方差
            Dim variances = New Double(nGenes - 1) {}
            Dim means = New Double(nGenes - 1) {}

            For j = 0 To nGenes - 1
                Dim sum = 0.0
                For i = 0 To nCells - 1
                    sum += data(i, j)
                Next
                means(j) = sum / nCells

                Dim sumSq = 0.0
                For i = 0 To nCells - 1
                    Dim diff = data(i, j) - means(j)
                    sumSq += diff * diff
                Next
                variances(j) = sumSq / nCells
            Next

            ' 按方差降序排序，选择前numGenes个基因
            Dim sortedIndices = Enumerable.Range(0, nGenes).
                OrderByDescending(Function(j) variances(j)).
                Take(numGenes).
                OrderBy(Function(j) j).
                ToArray()

            SelectedGeneIndices = sortedIndices

            ' 提取选中的基因
            Dim result = New Double(nCells - 1, numGenes - 1) {}
            For i = 0 To nCells - 1
                For k = 0 To numGenes - 1
                    result(i, k) = data(i, sortedIndices(k))
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 对基因进行Z-score标准化
        ''' 使每个基因的均值为0，标准差为1
        ''' </summary>
        Public Function StandardizeGenes(data As Double(,)) As Double(,)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)

            GeneMeans = New Double(nGenes - 1) {}
            GeneStds = New Double(nGenes - 1) {}

            ' 计算均值和标准差
            For j = 0 To nGenes - 1
                Dim sum = 0.0
                For i = 0 To nCells - 1
                    sum += data(i, j)
                Next
                GeneMeans(j) = sum / nCells

                Dim sumSq = 0.0
                For i = 0 To nCells - 1
                    Dim diff = data(i, j) - GeneMeans(j)
                    sumSq += diff * diff
                Next
                GeneStds(j) = std.Sqrt(sumSq / nCells)

                ' 防止除零
                If GeneStds(j) < 0.00000001 Then GeneStds(j) = 1.0
            Next

            ' 标准化
            Dim result = New Double(nCells - 1, nGenes - 1) {}
            For i = 0 To nCells - 1
                For j = 0 To nGenes - 1
                    result(i, j) = (data(i, j) - GeneMeans(j)) / GeneStds(j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 逆标准化
        ''' </summary>
        Public Function DeStandardizeGenes(data As Double(,)) As Double(,)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)
            Dim result = New Double(nCells - 1, nGenes - 1) {}

            For i = 0 To nCells - 1
                For j = 0 To nGenes - 1
                    result(i, j) = data(i, j) * GeneStds(j) + GeneMeans(j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' 归一化时间标签到[0, 1]区间
        ''' </summary>
        ''' <param name="timeLabels">原始时间标签数组</param>
        ''' <returns>归一化后的时间标签</returns>
        Public Function NormalizeTimeLabels(timeLabels As Double()) As Double()
            MinTime = timeLabels.Min()
            MaxTime = timeLabels.Max()
            Dim range = MaxTime - MinTime
            If range = 0 Then range = 1.0

            Return timeLabels.Select(Function(t) (t - MinTime) / range).ToArray()
        End Function

        ''' <summary>
        ''' 将归一化的时间标签还原为原始时间
        ''' </summary>
        Public Function DenormalizeTimeLabel(normalizedTime As Double) As Double
            Return normalizedTime * (MaxTime - MinTime) + MinTime
        End Function

        ''' <summary>
        ''' 逆变换：将对数归一化数据还原为原始尺度
        ''' </summary>
        Public Function InverseTransform(data As Double(,)) As Double(,)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)
            Dim result = New Double(nCells - 1, nGenes - 1) {}

            For i = 0 To nCells - 1
                For j = 0 To nGenes - 1
                    ' 逆对数变换：exp(x) - 1
                    result(i, j) = std.Exp(data(i, j)) - 1.0
                Next
            Next

            Return result
        End Function

    End Class

#End Region

#Region "训练器"

    ''' <summary>
    ''' CVAE训练器
    ''' 提供小批量训练、损失跟踪、学习率调度等功能
    ''' </summary>
    Public Class CVAETrainer

        ''' <summary>要训练的CVAE模型</summary>
        Public Property Model As CVAE

        ''' <summary>批量大小</summary>
        Public Property BatchSize As Integer = 128

        ''' <summary>训练轮数</summary>
        Public Property Epochs As Integer = 100

        ''' <summary>KL散度权重（β-VAE）</summary>
        Public Property Beta As Double = 1.0

        ''' <summary>学习率衰减因子（每decayInterval轮衰减一次）</summary>
        Public Property LrDecayFactor As Double = 0.95

        ''' <summary>学习率衰减间隔（轮数）</summary>
        Public Property LrDecayInterval As Integer = 10

        ''' <summary>是否打乱数据</summary>
        Public Property ShuffleData As Boolean = True

        ''' <summary>训练损失历史</summary>
        Public Property LossHistory As New List(Of Double)

        ''' <summary>重建损失历史</summary>
        Public Property ReconLossHistory As New List(Of Double)

        ''' <summary>KL损失历史</summary>
        Public Property KlLossHistory As New List(Of Double)

        ''' <summary>随机种子</summary>
        Public Property Seed As Integer? = 42

        Private Random As Random

        ''' <summary>
        ''' 创建训练器
        ''' </summary>
        Public Sub New(model As CVAE,
                       Optional batchSize As Integer = 128,
                       Optional epochs As Integer = 100,
                       Optional learningRate As Double = 0.001,
                       Optional beta As Double = 1.0)
            Me.Model = model
            Me.BatchSize = batchSize
            Me.Epochs = epochs
            Me.Beta = beta
            Me.Model.Optimizer.LearningRate = learningRate
            Me.Random = New Random(If(Seed.HasValue, Seed.Value, 42))
        End Sub

        ''' <summary>
        ''' 训练模型
        ''' </summary>
        ''' <param name="data">训练数据，形状 (n_cells, n_genes)</param>
        ''' <param name="timeLabels">时间标签（归一化后），形状 (n_cells,)</param>
        ''' <param name="verbose">是否打印训练进度</param>
        Public Sub Train(data As Double(,), timeLabels As Double(), Optional verbose As Boolean = True)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)
            Dim numBatches = CInt(std.Ceiling(nCells / CDbl(BatchSize)))

            If verbose Then
                Console.WriteLine($"开始训练CVAE...")
                Console.WriteLine($"  细胞数: {nCells}")
                Console.WriteLine($"  基因数: {nGenes}")
                Console.WriteLine($"  批量大小: {BatchSize}")
                Console.WriteLine($"  训练轮数: {Epochs}")
                Console.WriteLine($"  初始学习率: {Model.Optimizer.LearningRate}")
                Console.WriteLine($"  KL权重β: {Beta}")
                Console.WriteLine()
            End If

            ' 创建索引数组用于打乱
            Dim indices = Enumerable.Range(0, nCells).ToArray()

            For epoch = 1 To Epochs
                ' 学习率衰减
                If epoch > 1 AndAlso epoch Mod LrDecayInterval = 0 Then
                    Model.Optimizer.LearningRate *= LrDecayFactor
                End If

                ' 打乱数据
                If ShuffleData Then
                    Shuffle(indices)
                End If

                Dim epochTotalLoss = 0.0
                Dim epochReconLoss = 0.0
                Dim epochKlLoss = 0.0
                Dim batchCount = 0

                ' 小批量训练
                For batchStart = 0 To nCells - 1 Step BatchSize
                    Dim batchEnd = std.Min(batchStart + BatchSize, nCells)
                    Dim currentBatchSize = batchEnd - batchStart

                    ' 构建批量数据
                    Dim batchData = New Tensor(currentBatchSize, nGenes)
                    Dim batchConditions = New Tensor(currentBatchSize, Model.ConditionDim)

                    For k = 0 To currentBatchSize - 1
                        Dim cellIdx = indices(batchStart + k)
                        For j = 0 To nGenes - 1
                            batchData(k, j) = data(cellIdx, j)
                        Next
                        batchConditions(k, 0) = timeLabels(cellIdx)
                    Next

                    ' 前向传播
                    Dim forwardResult = Model.Forward(batchData, batchConditions)

                    ' 计算损失
                    Dim loss = Model.ComputeLoss(forwardResult.xRecon, batchData,
                                                  forwardResult.mu, forwardResult.logVar, Beta)

                    ' 反向传播
                    Model.Backward(forwardResult.xRecon, batchData,
                                   forwardResult.mu, forwardResult.logVar,
                                   Model.CachedZ, batchConditions, Beta)

                    ' 更新参数
                    Model.UpdateParameters()

                    epochTotalLoss += loss.totalLoss
                    epochReconLoss += loss.reconLoss
                    epochKlLoss += loss.klLoss
                    batchCount += 1
                Next

                ' 记录平均损失
                Dim avgTotalLoss = epochTotalLoss / batchCount
                Dim avgReconLoss = epochReconLoss / batchCount
                Dim avgKlLoss = epochKlLoss / batchCount

                LossHistory.Add(avgTotalLoss)
                ReconLossHistory.Add(avgReconLoss)
                KlLossHistory.Add(avgKlLoss)

                ' 打印进度
                If verbose AndAlso (epoch Mod 10 = 0 OrElse epoch = 1 OrElse epoch = Epochs) Then
                    Console.WriteLine($"  Epoch {epoch,4}/{Epochs} | " &
                                      $"Loss: {avgTotalLoss,10:F4} | " &
                                      $"Recon: {avgReconLoss,10:F4} | " &
                                      $"KL: {avgKlLoss,8:F4} | " &
                                      $"LR: {Model.Optimizer.LearningRate,8:F6}")
                End If
            Next

            If verbose Then
                Console.WriteLine()
                Console.WriteLine("训练完成！")
            End If
        End Sub

        ''' <summary>
        ''' 评估模型在给定数据上的损失
        ''' </summary>
        Public Function Evaluate(data As Double(,), timeLabels As Double()) As (totalLoss As Double, reconLoss As Double, klLoss As Double)
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)
            Dim totalLoss = 0.0
            Dim reconLoss = 0.0
            Dim klLoss = 0.0
            Dim batchCount = 0

            For batchStart = 0 To nCells - 1 Step BatchSize
                Dim batchEnd = std.Min(batchStart + BatchSize, nCells)
                Dim currentBatchSize = batchEnd - batchStart

                Dim batchData = New Tensor(currentBatchSize, nGenes)
                Dim batchConditions = New Tensor(currentBatchSize, Model.ConditionDim)

                For k = 0 To currentBatchSize - 1
                    For j = 0 To nGenes - 1
                        batchData(k, j) = data(batchStart + k, j)
                    Next
                    batchConditions(k, 0) = timeLabels(batchStart + k)
                Next

                Dim forwardResult = Model.Forward(batchData, batchConditions)
                Dim loss = Model.ComputeLoss(forwardResult.xRecon, batchData,
                                              forwardResult.mu, forwardResult.logVar, Beta)

                totalLoss += loss.totalLoss
                reconLoss += loss.reconLoss
                klLoss += loss.klLoss
                batchCount += 1
            Next

            Return (totalLoss / batchCount, reconLoss / batchCount, klLoss / batchCount)
        End Function

        ''' <summary>
        ''' 评估重建质量：计算每个基因的MSE和整体R²
        ''' </summary>
        ''' <returns>(meanMSE, meanR2, perGeneMSE)</returns>
        Public Function EvaluateReconstruction(data As Double(,),
                                                timeLabels As Double()) As (meanMSE As Double, meanR2 As Double, perGeneMSE As Double())
            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)

            ' 前向传播获取重建结果
            Dim dataTensor = New Tensor(nCells, nGenes)
            For i = 0 To nCells - 1
                For j = 0 To nGenes - 1
                    dataTensor(i, j) = data(i, j)
                Next
            Next

            Dim condTensor = New Tensor(nCells, 1)
            For i = 0 To nCells - 1
                condTensor(i, 0) = timeLabels(i)
            Next

            Dim recon = Model.Forward(dataTensor, condTensor).xRecon
            Dim reconArr = recon.To2DArrayDouble()

            ' 计算每个基因的MSE
            Dim perGeneMSE(nGenes - 1) As Double
            Dim perGeneVar(nGenes - 1) As Double
            Dim perGeneMean(nGenes - 1) As Double

            ' 计算每个基因的均值
            For j = 0 To nGenes - 1
                Dim sum = 0.0
                For i = 0 To nCells - 1
                    sum += data(i, j)
                Next
                perGeneMean(j) = sum / nCells
            Next

            ' 计算每个基因的方差和MSE
            Dim totalMSE = 0.0
            Dim totalVar = 0.0
            For j = 0 To nGenes - 1
                Dim se = 0.0
                Dim var = 0.0
                For i = 0 To nCells - 1
                    Dim diff = data(i, j) - reconArr(i, j)
                    se += diff * diff
                    var += (data(i, j) - perGeneMean(j)) ^ 2
                Next
                perGeneMSE(j) = se / nCells
                perGeneVar(j) = var / nCells
                totalMSE += perGeneMSE(j)
                totalVar += perGeneVar(j)
            Next

            Dim meanMSE = totalMSE / nGenes
            ' R² = 1 - SSE/SST, 整体R²
            Dim meanR2 = If(totalVar > 0, 1.0 - totalMSE / totalVar, 0.0)

            Return (meanMSE, meanR2, perGeneMSE)
        End Function

        ''' <summary>
        ''' Fisher-Yates洗牌算法
        ''' </summary>
        Private Sub Shuffle(arr As Integer())
            For i = arr.Length - 1 To 1 Step -1
                Dim j = Random.Next(i + 1)
                Dim temp = arr(i)
                arr(i) = arr(j)
                arr(j) = temp
            Next
        End Sub

    End Class

#End Region

#Region "时间序列插值器"

    ''' <summary>
    ''' 插值结果容器
    ''' </summary>
    Public Class InterpolationResult

        ''' <summary>插值后的表达矩阵，形状 (n_cells, n_genes)</summary>
        Public Property Data As Double(,)

        ''' <summary>每个细胞对应的时间标签（归一化后）</summary>
        Public Property TimeLabels As Double()

        ''' <summary>所有唯一的时间点（归一化后）</summary>
        Public Property UniqueTimePoints As Double()

        ''' <summary>每个时间点的细胞数量</summary>
        Public Property CellsPerTimePoint As Dictionary(Of Double, Integer)

    End Class

    ''' <summary>
    ''' 时间序列插值器
    ''' 
    ''' 实现基于CVAE的时间序列插值策略：
    '''   策略1（前向）：使用目标时间点之前最近的真实时间点的细胞
    '''   策略2（后向）：使用目标时间点之后最近的真实时间点的细胞
    '''   策略3（双向合并）：同时使用前后两个时间点的细胞，合并结果
    '''   
    ''' 对于目标时间t_new：
    '''   1. 找到前后最近的真实时间点t1和t2
    '''   2. 将t1和t2的真实细胞编码到潜在空间
    '''   3. 用条件c=t_new解码，得到插值细胞
    ''' </summary>
    Public Class TimeSeriesInterpolator

        ''' <summary>训练好的CVAE模型</summary>
        Public Property Model As CVAE

        ''' <summary>数据预处理器（用于逆变换）</summary>
        Public Property Preprocessor As DataPreprocessor

        ''' <summary>插值策略：1=前向, 2=后向, 3=双向合并, 4=潜在空间线性插值</summary>
        Public Property Strategy As Integer = 3

        ''' <summary>每个插值时间点生成的细胞数量（0表示使用所有可用细胞）</summary>
        Public Property NumSamplesPerTime As Integer = 0

        ''' <summary>随机种子</summary>
        Public Property Seed As Integer? = 42

        Private Random As Random

        ''' <summary>
        ''' 创建插值器
        ''' </summary>
        Public Sub New(model As CVAE, Optional preprocessor As DataPreprocessor = Nothing)
            Me.Model = model
            Me.Preprocessor = preprocessor
            Me.Random = New Random(If(Seed.HasValue, Seed.Value, 42))
        End Sub

        ''' <summary>
        ''' 生成目标时间点
        ''' </summary>
        ''' <param name="normalizedTimes">归一化后的原始时间标签</param>
        ''' <param name="intervalHours">插值间隔（小时），如0.25表示15分钟</param>
        ''' <returns>归一化后的目标时间点数组</returns>
        Public Function GenerateTargetTimePoints(normalizedTimes As Double(), intervalHours As Double) As Double()
            ' 获取原始时间范围
            Dim minNorm = normalizedTimes.Min()
            Dim maxNorm = normalizedTimes.Max()

            ' 如果有预处理器，将间隔转换为归一化空间
            Dim normInterval As Double
            If Preprocessor IsNot Nothing AndAlso Preprocessor.MaxTime > Preprocessor.MinTime Then
                Dim timeRange = Preprocessor.MaxTime - Preprocessor.MinTime
                normInterval = intervalHours / timeRange
            Else
                ' 假设归一化时间就是小时
                normInterval = intervalHours / (1.0 / (maxNorm - minNorm))
            End If

            ' 生成目标时间点
            Dim timePoints = New List(Of Double)()
            Dim t = minNorm
            Do While t <= maxNorm + 0.0000000001
                timePoints.Add(t)
                t += normInterval
            Loop

            ' 确保包含最大时间点
            If std.Abs(timePoints(timePoints.Count - 1) - maxNorm) > 0.000001 Then
                timePoints.Add(maxNorm)
            End If

            Return timePoints.ToArray()
        End Function

        ''' <summary>
        ''' 执行时间序列插值
        ''' </summary>
        ''' <param name="data">原始表达数据，形状 (n_cells, n_genes)</param>
        ''' <param name="timeLabels">每个细胞的时间标签（归一化后）</param>
        ''' <param name="intervalHours">插值间隔（小时），如0.25表示15分钟</param>
        ''' <param name="strategy">插值策略：1=前向, 2=后向, 3=双向合并, 4=潜在空间线性插值</param>
        ''' <returns>插值结果</returns>
        Public Function Interpolate(data As Double(,),
                                    timeLabels As Double(),
                                    intervalHours As Double,
                                    Optional strategy As Integer = 3) As InterpolationResult
            Me.Strategy = strategy

            Dim nCells = data.GetLength(0)
            Dim nGenes = data.GetLength(1)

            ' 获取唯一时间点
            Dim uniqueTimes = timeLabels.Distinct().OrderBy(Function(t) t).ToArray()
            Console.WriteLine($"  原始时间点数: {uniqueTimes.Length}")

            ' 生成目标时间点
            Dim targetTimes = GenerateTargetTimePoints(timeLabels, intervalHours)
            Console.WriteLine($"  目标时间点数: {targetTimes.Length}")

            ' 按时间点组织细胞索引
            Dim cellsByTime As New Dictionary(Of Double, List(Of Integer))
            For i = 0 To nCells - 1
                Dim t = timeLabels(i)
                If Not cellsByTime.ContainsKey(t) Then
                    cellsByTime(t) = New List(Of Integer)()
                End If
                cellsByTime(t).Add(i)
            Next

            ' 存储插值结果
            Dim resultData As New List(Of Double(,))()
            Dim resultTimeLabels As New List(Of Double)()
            Dim cellsPerTime As New Dictionary(Of Double, Integer)()

            ' 对每个目标时间点进行插值
            For Each targetTime In targetTimes
                ' 检查是否是原始时间点
                Dim isOriginal = uniqueTimes.Any(Function(t) std.Abs(t - targetTime) < 0.000001)

                If isOriginal Then
                    ' 原始时间点：直接使用原始数据
                    Dim origTime = uniqueTimes.First(Function(t) std.Abs(t - targetTime) < 0.000001)
                    Dim cellIndices = cellsByTime(origTime)
                    Dim cellsData = New Double(cellIndices.Count - 1, nGenes - 1) {}

                    For k = 0 To cellIndices.Count - 1
                        For j = 0 To nGenes - 1
                            cellsData(k, j) = data(cellIndices(k), j)
                        Next
                    Next

                    resultData.Add(cellsData)
                    For k = 0 To cellIndices.Count - 1
                        resultTimeLabels.Add(targetTime)
                    Next
                    cellsPerTime(targetTime) = cellIndices.Count
                Else
                    ' 新时间点：使用CVAE插值
                    Dim interpolated = InterpolateSingleTime(
                        data, timeLabels, uniqueTimes, cellsByTime, targetTime, nGenes)

                    If interpolated IsNot Nothing Then
                        resultData.Add(interpolated)
                        For k = 0 To interpolated.GetLength(0) - 1
                            resultTimeLabels.Add(targetTime)
                        Next
                        cellsPerTime(targetTime) = interpolated.GetLength(0)
                    End If
                End If
            Next

            ' 合并所有结果
            Dim totalCells = resultData.Sum(Function(d) d.GetLength(0))
            Dim finalData = New Double(totalCells - 1, nGenes - 1) {}
            Dim finalTimeLabels = resultTimeLabels.ToArray()

            Dim offset = 0
            For Each block In resultData
                Dim blockCells = block.GetLength(0)
                For i = 0 To blockCells - 1
                    For j = 0 To nGenes - 1
                        finalData(offset + i, j) = block(i, j)
                    Next
                Next
                offset += blockCells
            Next

            Return New InterpolationResult With {
                .Data = finalData,
                .TimeLabels = finalTimeLabels,
                .UniqueTimePoints = targetTimes,
                .CellsPerTimePoint = cellsPerTime
            }
        End Function

        ''' <summary>
        ''' 对单个时间点进行插值
        ''' </summary>
        Private Function InterpolateSingleTime(data As Double(,),
                                               timeLabels As Double(),
                                               uniqueTimes As Double(),
                                               cellsByTime As Dictionary(Of Double, List(Of Integer)),
                                               targetTime As Double,
                                               nGenes As Integer) As Double(,)
            ' 找到前后最近的真实时间点
            Dim tBefore = Double.NaN
            Dim tAfter = Double.NaN

            For Each t In uniqueTimes
                If t < targetTime Then
                    If Double.IsNaN(tBefore) OrElse t > tBefore Then
                        tBefore = t
                    End If
                ElseIf t > targetTime Then
                    If Double.IsNaN(tAfter) OrElse t < tAfter Then
                        tAfter = t
                    End If
                End If
            Next

            Dim resultBlocks As New List(Of Double(,))()

            ' 策略4：潜在空间线性插值（在z空间对前后时间点的细胞做线性插值）
            If Strategy = 4 AndAlso Not Double.IsNaN(tBefore) AndAlso Not Double.IsNaN(tAfter) Then
                Dim block = InterpolateLatentLinear(data, cellsByTime, tBefore, tAfter, targetTime, nGenes)
                If block IsNot Nothing Then resultBlocks.Add(block)
                GoTo mergeResults
            End If

            ' 根据策略选择源时间点
            Dim useBefore = (Strategy = 1 OrElse Strategy = 3) AndAlso Not Double.IsNaN(tBefore)
            Dim useAfter = (Strategy = 2 OrElse Strategy = 3) AndAlso Not Double.IsNaN(tAfter)

            ' 如果没有可用的源时间点，返回Nothing
            If Not useBefore AndAlso Not useAfter Then
                Return Nothing
            End If

            ' 使用前向时间点的细胞
            If useBefore Then
                Dim block = InterpolateFromTimePoint(data, cellsByTime, tBefore, targetTime, nGenes)
                If block IsNot Nothing Then resultBlocks.Add(block)
            End If

            ' 使用后向时间点的细胞
            If useAfter Then
                Dim block = InterpolateFromTimePoint(data, cellsByTime, tAfter, targetTime, nGenes)
                If block IsNot Nothing Then resultBlocks.Add(block)
            End If

mergeResults:
            ' 合并结果
            If resultBlocks.Count = 0 Then Return Nothing

            Dim totalCells = resultBlocks.Sum(Function(b) b.GetLength(0))
            Dim result = New Double(totalCells - 1, nGenes - 1) {}
            Dim offset = 0
            For Each block In resultBlocks
                For i = 0 To block.GetLength(0) - 1
                    For j = 0 To nGenes - 1
                        result(offset + i, j) = block(i, j)
                    Next
                Next
                offset += block.GetLength(0)
            Next

            Return result
        End Function

        ''' <summary>
        ''' 从单个时间点的细胞生成插值数据
        ''' </summary>
        Private Function InterpolateFromTimePoint(data As Double(,),
                                                   cellsByTime As Dictionary(Of Double, List(Of Integer)),
                                                   sourceTime As Double,
                                                   targetTime As Double,
                                                   nGenes As Integer) As Double(,)
            If Not cellsByTime.ContainsKey(sourceTime) Then Return Nothing

            Dim cellIndices = cellsByTime(sourceTime)

            ' 如果指定了采样数量，随机采样
            If NumSamplesPerTime > 0 AndAlso NumSamplesPerTime < cellIndices.Count Then
                Dim sampled = cellIndices.OrderBy(Function(x) Random.Next()).Take(NumSamplesPerTime).ToList()
                cellIndices = sampled
            End If

            Dim nCells = cellIndices.Count
            Dim cellsTensor = New Tensor(nCells, nGenes)

            For i = 0 To nCells - 1
                For j = 0 To nGenes - 1
                    cellsTensor(i, j) = data(cellIndices(i), j)
                Next
            Next

            ' 使用CVAE插值
            Dim interpolated = Model.InterpolateTimePoint(cellsTensor, sourceTime, targetTime)

            Return interpolated.To2DArrayDouble()
        End Function

        ''' <summary>
        ''' 策略4：潜在空间线性插值
        ''' 将前后两个时间点的细胞分别编码到潜在空间，在z空间做线性插值后用目标时间条件解码
        ''' z_new = (1-α)·z_before + α·z_after，其中 α = (t_new - t_before)/(t_after - t_before)
        ''' </summary>
        Private Function InterpolateLatentLinear(data As Double(,),
                                                  cellsByTime As Dictionary(Of Double, List(Of Integer)),
                                                  tBefore As Double,
                                                  tAfter As Double,
                                                  targetTime As Double,
                                                  nGenes As Integer) As Double(,)
            If Not cellsByTime.ContainsKey(tBefore) OrElse Not cellsByTime.ContainsKey(tAfter) Then
                Return Nothing
            End If

            Dim beforeCells = cellsByTime(tBefore)
            Dim afterCells = cellsByTime(tAfter)

            ' 采样使前后细胞数量一致
            Dim nPairs = std.Min(beforeCells.Count, afterCells.Count)
            If NumSamplesPerTime > 0 AndAlso NumSamplesPerTime < nPairs Then
                nPairs = NumSamplesPerTime
            End If
            If nPairs = 0 Then Return Nothing

            ' 随机配对
            Dim beforeSampled = beforeCells.OrderBy(Function(x) Random.Next()).Take(nPairs).ToList()
            Dim afterSampled = afterCells.OrderBy(Function(x) Random.Next()).Take(nPairs).ToList()

            ' 构建前后细胞张量
            Dim beforeTensor = New Tensor(nPairs, nGenes)
            Dim afterTensor = New Tensor(nPairs, nGenes)
            For i = 0 To nPairs - 1
                For j = 0 To nGenes - 1
                    beforeTensor(i, j) = data(beforeSampled(i), j)
                    afterTensor(i, j) = data(afterSampled(i), j)
                Next
            Next

            ' 编码到潜在空间（使用均值mu作为潜在表示）
            Dim cBefore = New Tensor(nPairs, 1)
            Dim cAfter = New Tensor(nPairs, 1)
            For i = 0 To nPairs - 1
                cBefore(i, 0) = tBefore
                cAfter(i, 0) = tAfter
            Next

            Dim encBefore = Model.Encode(beforeTensor, cBefore)
            Dim encAfter = Model.Encode(afterTensor, cAfter)
            Dim zBefore = encBefore.mu
            Dim zAfter = encAfter.mu

            ' 在潜在空间做线性插值
            Dim alpha = (targetTime - tBefore) / (tAfter - tBefore)
            alpha = std.Max(0.0, std.Min(1.0, alpha))  ' 裁剪到[0,1]

            Dim zInterp = New Tensor(nPairs, Model.LatentDim)
            For i = 0 To nPairs - 1
                For j = 0 To Model.LatentDim - 1
                    zInterp(i, j) = (1.0 - alpha) * zBefore(i, j) + alpha * zAfter(i, j)
                Next
            Next

            ' 用目标时间条件解码
            Dim cTarget = New Tensor(nPairs, 1)
            For i = 0 To nPairs - 1
                cTarget(i, 0) = targetTime
            Next

            Dim reconstructed = Model.Decode(zInterp, cTarget)
            Return reconstructed.To2DArrayDouble()
        End Function

    End Class

#End Region

#Region "演示模块"

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

#End Region

End Namespace
