# SpatialOmics — SpatialDE + spatialGE VB.NET 实现

基于两篇方法学论文，使用 VB.NET (.NET 10) 和 BCL (Base Class Library) 实现的空间转录组学差异基因分析模块。

## 文献来源

| 模块 | 论文 | 核心算法 |
|------|------|----------|
| **SpatialDE** | Svensson et al., *Nat Methods* 15:343–346 (2018) | Gaussian Process 回归 + 似然比检验 |
| **spatialGE** | Ospina et al., *Bioinformatics* 38(9):2645–2647 (2022) | 空间自相关统计 + 克里金插值 + 空间感知聚类 |

## 项目结构

```
SpatialOmics/
├── SpatialOmics.vbproj          # .NET 10 项目文件
├── Program.vb                   # 入口程序（含模拟数据演示）
├── Math/
│   ├── Matrix.vb                # 矩阵运算（Cholesky 分解、求逆、行列式等）
│   ├── Statistics.vb            # 统计分布（Gamma、χ²、BH-FDR）
│   └── Optimization.vb          # 优化器（Brent 一维、坐标下降）
├── SpatialDE/
│   ├── Covariance.vb           # 协方差核（SE / Linear / Periodic）
│   ├── SpatialDEModel.vb        # GP 模型 + 似然比检验 + BIC 模型选择
│   └── AEH.vb                   # Automatic Expression Histology 聚类
├── SpatialGE/
│   ├── SpatialStats.vb          # Moran's I / Geary's C / Getis-Ord Gi*
│   ├── Kriging.vb               # 普通克里金插值
│   ├── STclust.vb               # 空间感知聚类（Ward + K-Medoids）
│   └── SpatialGEModel.vb        # 整合分析流程
└── README.md
```

## 编译方法

```bash
# 需要 .NET 10 SDK
dotnet build
dotnet run
```

## 算法详解

### SpatialDE 模块

**核心思想**：将每个基因的表达值建模为空间高斯过程，通过比较"有空间分量"和"无空间分量"两个模型的似然，鉴定空间变异基因。

**数学模型**：

1. **完整模型（GP）**：`y ~ N(μ·1, σ_s²·(Σ + δ·I))`
   - `Σ_ij = exp(-||x_i - x_j||² / (2·l²))` — 平方指数核
   - `δ` — 非空间噪声比；`FSV = 1/(1+δ)` — 空间方差分数
   - `l` — 长度尺度（控制空间相关距离）

2. **零假设模型**：`y ~ N(μ·1, σ²·I)`（无空间分量）

3. **似然比检验**：`LR = 2·(LL_full - LL_null)` ~ χ²(1)

4. **超参数优化**：
   - `μ`, `σ_s²` 闭式解
   - `δ` 通过梯度优化
   - `l` 通过网格搜索

5. **BIC 模型选择**：比较 SE / Linear / Periodic 核
   `BIC = log(N)·M - 2·LL`

6. **AEH 聚类**：对显著基因的 GP 后验均值做 K-means + 变分 EM

### spatialGE 模块

**核心思想**：使用经典空间统计学方法量化肿瘤微环境异质性。

**统计量**：

1. **Moran's I（全局空间自相关）**：
   `I = (N/S₀) · ΣᵢΣⱼ wᵢⱼ(xᵢ-x̄)(xⱼ-x̄) / Σᵢ(xᵢ-x̄)²`
   - 期望 `E[I] = -1/(N-1)`
   - `I > E[I]` → 正空间自相关（聚集分布）

2. **Geary's C（局部空间自相关）**：
   `C = ((N-1)/(2·S₀)) · ΣᵢΣⱼ wᵢⱼ(xᵢ-xⱼ)² / Σᵢ(xᵢ-x̄)²`
   - `C ∈ [0,2]`；`C<1` 正自相关，`C>1` 负自相关

3. **Getis-Ord Gi*（热点分析）**：
   `Gi* = (Σⱼ wᵢⱼ xⱼ - x̄·Σⱼ wᵢⱼ) / (S·√((N·Σⱼ wᵢⱼ²-(Σⱼ wᵢⱼ)²)/(N-1)))`
   - `Gi* > 0` 热点（高值聚集）；`Gi* < 0` 冷点（低值聚集）

4. **普通克里金插值**：
   - 拟合球状变异函数模型
   - `λ = Γ⁻¹·g`，`ẑ(x₀) = Σ λᵢ·z(xᵢ)`

5. **STclust 聚类**：
   - 组合距离 `D = (1-w)·D₁ + w·D₂`
   - `D₁` 转录组距离，`D₂` 空间距离
   - Ward 层次聚类 / K-Medoids

## API 速查

### SpatialDE

```vbnet
Dim deModel As New SpatialDEModel(coords)
Dim results = deModel.Analyze(expression, geneNames)

' 结果属性
results(0).GeneName      ' 基因名
results(0).FSV           ' 空间方差分数 (0~1)
results(0).PValue        ' p 值
results(0).QValue         ' FDR 校正后 q 值
results(0).IsSignificant  ' q < 0.05?
results(0).BestKernel     ' 最优核类型
results(0).LengthScale    ' 长度尺度参数

' AEH 聚类
Dim aeh As New AEHCalculator(coords)
Dim aehResult = aeh.Run(results, expression, geneNames, kPatterns:=4)
```

### spatialGE

```vbnet
Dim geModel As New SpatialGEModel(coords)

' 空间自相关统计
Dim stats = geModel.ComputeSpatialStats(expression, geneNames)
stats(0).MoransI          ' Moran's I
stats(0).GearysC          ' Geary's C
stats(0).GetisOrdGiZScore ' Gi* z-scores 数组

' 克里金插值
Dim kriging As New OrdinaryKriging(coords)
Dim krigResult = kriging.Interpolate(values, targetCoords)

' STclust 聚类
Dim clustResult = geModel.RunSTclust(expression, geneNames, nClusters:=4)

' 完整分析
Dim fullResult = geModel.RunFull(expression, geneNames, nClusters:=4)
```

## 设计特点

- **纯 BCL 实现**：不依赖任何第三方 NuGet 包，仅使用 `System.Math`、`System.Linq` 等 BCL 基础类库
- **中文注释**：所有代码注释使用中文，符合中文项目规范
- **模块化设计**：每个算法组件独立封装，可单独调用
- **数值稳定性**：使用 Cholesky 分解替代直接求逆，避免数值不稳定
