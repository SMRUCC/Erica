---
name: 修复SpatialStats与STclust编译错误
overview: 修复两个 VB 编译错误：(1) ComputeGearysC 中未定义变量 S1；(2) STclust 中对 Matrix 对象误用 ReDim Preserve。同时需要为 Matrix 类补充矩阵扩容方法。
todos:
  - id: fix-s1-geary
    content: 在 ComputeGearysC 中补充 S1 统计量计算
    status: completed
  - id: add-matrix-resize
    content: 为 Matrix 类新增 Resize 扩容方法
    status: completed
  - id: fix-redim-clust
    content: 将 STclust 中 ReDim Preserve D 替换为 D.Resize 调用
    status: completed
    dependencies:
      - add-matrix-resize
---

## 用户需求

修复空间转录组学 VB.NET 模块中两个编译/运行时错误，均基于项目现有代码上下文进行最小侵入式修正。

## 产品概述

本模块为空间转录组数据分析程序，包含两个待修复缺陷：

1. `SpatialStats.vb` 中 `ComputeGearysC` 函数引用未声明的 `S1` 变量导致编译失败。
2. `STclust.vb` 中 `HierarchicalClusteringWard` 函数对自定义 `Matrix` 对象执行 `ReDim Preserve` 导致编译失败。

## 核心功能

- 在 `ComputeGearysC` 中补充 `S1` 统计量计算（行和与列和的平方和的一半），使 Geary's C 方差公式可正确求值。
- 为 `Matrix` 类新增矩阵扩容方法，保留原有数据并在右下角扩展维数。
- 将 `STclust.vb` 中作用于 `Matrix` 对象的 `ReDim Preserve D(...)` 替换为新增的扩容方法调用，`clusterSizes` 一维数组的 `ReDim Preserve` 保持合法不变。

## 技术栈

- 语言：VB.NET（.NET，Win32 平台）
- 现有数学基础结构：`Expression/Math/Matrix.vb` 自定义 `Matrix` 类（内部 `_data As Double(,)`）
- 相关模块：`Expression/SpatialGE/SpatialStats.vb`、`Expression/SpatialGE/STclust.vb`

## 实现方案

### 错误1：S1 未定义

`ComputeGearysC` 计算 Geary's C 近似方差时引用了 `S1`，但函数内仅声明 `S0` 与 `numerator`。`S1` 是空间自相关标准统计量：对所有行计算（行权重和 + 列权重和）的平方，累加后除以 2。同文件 `ComputeMoransI`（233-244 行）已有完全相同的 S1 计算实现，可直接复用其逻辑。在 `ComputeGearysC` 中、`S0` 与 `numerator` 的累加循环之后（约 295 行后）插入与 `ComputeMoransI` 一致的 `S1` 计算代码，确保方差公式 `varC` 可正确编译与运行。该方案不改动既有数值逻辑，仅补齐缺失变量。

### 错误2：ReDim Preserve 作用于 Matrix

`ReDim Preserve` 仅适用于 VB 内建数组类型。`clusterSizes` 为 `Integer()`，其 `ReDim Preserve clusterSizes(maxClusterId)`（251 行）合法，保留不动。`D` 为 `Matrix` 类实例，不能直接 `ReDim`。需在 `Matrix` 类中新增 `Resize(newRows As Integer, newCols As Integer) As Matrix` 方法：创建一个 `newRows × newCols` 的新 `Double(,)`，将旧 `_data` 中 `min(原行,新行) × min(原列,新列)` 范围内的数据复制过去（保留左上角已有距离），返回新 `Matrix`（或直接原地重建 `_data` 并同步 `_rows/_cols`）。随后在 `STclust.vb:259` 将 `ReDim Preserve D(maxClusterId, maxClusterId)` 改为 `D = D.Resize(maxClusterId + 1, maxClusterId + 1)`（注意 Matrix 索引从 0 开始，原 ReDim 上界需 +1 以对齐）。该方案遵循现有 `Clone`/`ToArray` 的数组复制模式，避免引入新架构。

## 实现注意

- `Matrix.Resize` 应保持对 `_rows`、`_cols` 的同步更新，并复用 `_data.Clone`/逐元素拷贝的成熟做法。
- 扩容时仅复制有效交集区域，避免越界；新增单元格默认 0.0，由后续 Ward 距离更新逻辑填充。
- 不改动 `clusterSizes` 的 `ReDim Preserve` 用法（合法），避免非必要重构。
- 不触碰 `STclust.vb:302-305` 回溯逻辑（用户未要求，且属可选范围），控制改动面。

## 架构设计

两个修改均位于既有模块内部，不引入新依赖或新分层：

- `Matrix` 增加纯函数式 `Resize`，符合现有 `Clone`/`Transpose` 等返回新实例的约定。
- `SpatialStats` 补全局部变量，与 `ComputeMoransI` 模式一致。
- `STclust` 仅替换一行调用，调用点语义不变。

## 目录结构

```
g:\Erica\src\STImaging\
├── Expression\
│   ├── Math\
│   │   └── Matrix.vb              # [MODIFY] 新增 Resize(newRows, newCols) 方法，保留原数据并扩展矩阵维度
│   └── SpatialGE\
│       ├── SpatialStats.vb        # [MODIFY] 在 ComputeGearysC 内 S0/numerator 循环后补充 S1 计算（复用 ComputeMoransI 逻辑）
│       └── STclust.vb            # [MODIFY] 将 259 行 ReDim Preserve D(...) 替换为 D = D.Resize(maxClusterId + 1, maxClusterId + 1)
```