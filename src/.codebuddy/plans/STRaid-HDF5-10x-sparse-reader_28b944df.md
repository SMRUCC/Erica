---
name: STRaid-HDF5-10x-sparse-reader
overview: 在基础 HDF5 模块中新增稀疏感知读取能力（基于 COO/扁平三元组直接构建 SparseMatrix，避免一次性解码整块到内存），并在 STRaid 的 HDF5 层按 10x Genomics 语义封装 feature_slice.h5 与 molecule_info.h5 两个 Visium HD 文件的解析，提供内存友好的稀疏矩阵读取。
todos:
  - id: explore-chunk-sparse
    content: 用 [subagent:code-explorer] 核对 ChunkedDatasetV3 解码链与 SparseMatrix COO 构造签名
    status: completed
  - id: add-stream-chunk
    content: 在 ChunkedDatasetV3 新增 EnumerateChunkArrays 流式分块读取 API
    status: completed
    dependencies:
      - explore-chunk-sparse
  - id: add-sparse-triplets
    content: 在 HDF5Reader 新增 COO 三元组转 SparseMatrix 通用读取封装
    status: completed
    dependencies:
      - explore-chunk-sparse
  - id: read-feature-slice
    content: 在 STRaid 实现 feature_slice.h5 的 feature_slices 稀疏解析
    status: completed
    dependencies:
      - add-sparse-triplets
  - id: read-molecule-info
    content: 在 STRaid 实现 molecule_info.h5 流式聚合 UMI 稀疏矩阵
    status: completed
    dependencies:
      - add-sparse-triplets
  - id: unify-entry-meta
    content: 新增 TenXReader 统一入口并提取 barcodes/features 元数据
    status: completed
    dependencies:
      - read-feature-slice
      - read-molecule-info
  - id: verify-test-files
    content: 用两个 h5 测试文件验证稀疏读取不 OOM 且结果正确
    status: completed
    dependencies:
      - unify-entry-meta
---

## 用户需求

在 STRaid 项目中基于已有的基础 HDF5 读取模块解析 10x Genomics 的 Visium HD 原始 hdf5 数据，并用 `Visium_HD_6p5mm_Rat_Liver_feature_slice.h5` 与 `Visium_HD_6p5mm_Rat_Liver_molecule_info.h5` 两个文件测试。由于数据矩阵极大且稀疏性极高（内存无法一次性加载），需同步优化基础 HDF5 模块的矩阵加载，使其按稀疏性读取，并使用基础数学模块中的 `SparseMatrix` 对象承载稀疏矩阵。

## 产品概述

提供一个内存友好的 10x Genomics hdf5 解析能力：基础 HDF5 模块新增通用稀疏感知读取（避免一次性解压整块到内存），STRaid 的 HDF5 层按 10x 语义封装两种文件结构，最终输出 `LinearAlgebra.Matrix.SparseMatrix` 表达的表达矩阵，以及对应的 barcode / feature 元数据。

## 核心功能

- 基础模块：新增按 chunk 流式枚举分块 dataset 一维数组的能力，避免 `ChunkedDatasetV3.getBuffer` 一次性解压整块到内存。
- 基础模块（或稀疏库）：支持由 COO 三元组 `(row, col, value)` 直接构建 `SparseMatrix`，兼容 uint32/uint64 计数与坐标类型（转内部 Integer/Double）。
- STRaid：解析 `feature_slice.h5` 的 `feature_slices/<id>/{row,col,data}` 分组为稀疏矩阵（约 3350×3350 bins，约 3.96 亿非零）。
- STRaid：解析 `molecule_info.h5` 的扁平分子表（`barcode_idx`、`feature_idx`、`count`），分块流式聚合为稀疏 UMI 计数矩阵（约 915 万 × 25629，约 5.07 亿非零，稠密化约 940GB）。
- STRaid：提取两个文件共有的 `features`（id/name/genome/feature_type）与 `barcodes` 元数据，关联到稀疏矩阵维度。
- 统一封装入口：给定文件路径即返回结构化结果（稀疏矩阵 + barcodes + features），供上层 STRaid 数据模型消费。

## 技术栈选择

- 语言/框架：VB.NET（.NET，与现有项目一致），命名空间沿用 `Microsoft.VisualBasic.Data.IO.HDF5`（基础模块）与 `Microsoft.VisualBasic.DataMining.STRAID.HDF5`（STRaid 封装层）。
- 稀疏矩阵：`Microsoft.VisualBasic.Math.LinearAlgebra.Matrix.SparseMatrix`（已存在于 Matrix.NET 项目，基础 HDF5 模块已通过 `Math.NET5.vbproj` 引用，无需新增 ProjectReference）。
- 测试探查：Anaconda3 Python + h5py（仅用于验证文件结构，不写入项目代码）。

## 实现方案

### 总体策略

在基础 HDF5 模块新增「分块流式一维读取」与「COO 三元组 → SparseMatrix」两处通用能力；STRaid 的 HDF5 封装层按两种 10x 文件结构调用通用能力完成稀疏解析与元数据提取。

### 关键技术决策

1. **流式分块读取（性能瓶颈核心）**：`ChunkedDatasetV3.getBuffer` 当前会将整个 dataset 解压为 `diskSize` 字节的 `MemoryStream`，对 5.07 亿行的 `molecule_info` 数组会直接 OOM。新增 `ChunkedDatasetV3.EnumerateChunkArrays(Of T)() As IEnumerable(Of T())`，复用已有的 `DataBTree`/`ChunkLookup` 逐 chunk 解码为 `T()` 后 yield 返回，上层在遍历中聚合，内存峰值降到单 chunk 量级（65536 元素）。
2. **稀疏构建走 COO 而非 CSC**：两个文件均无 `indptr`，不能直接用现有 `UnpackData`（仅接受 CSC 的 Single/Integer）。`SparseMatrix` 已有 `Sub New(row As Integer(), col As Integer(), x As Double())` 的 COO 构造入口，可直接复用。需把 uint32 坐标/计数经 `CInt`/`CDbl` 转换（坐标范围 0..3349、feature_idx 0..25628、barcode_idx 0..9155403 均在 Integer 范围内，安全）；`row/col` 用 `Integer()`、值用 `Double()`。
3. **molecule_info 聚合策略**：分块读取 `(barcode_idx, feature_idx, count)` 三元组，按 `(barcode, feature)` 累加。因无法预排序全量，采用 `Dictionary(Of Long, Double)` 以 `barcode * nFeatures + feature` 为键增量累加（5.07 亿键最坏占用偏高，但 count 样本均为 1、实际聚合后远小于 5.07 亿）。聚合完成后批量调用 `Sub New(row(), col(), x())` 一次性构建 SparseMatrix，避免逐元素 `Set` 的字典开销。可选：若内存紧张，按 barcode 区间分治聚合。
4. **复用而非新增依赖**：基础模块已可访问 SparseMatrix，封装层已引用两个库，方案最小化新增引用，符合现有架构。

### 性能与可靠性

- 时间复杂度：feature_slice 为 O(nnz) 三元组读取与构建；molecule_info 为 O(总行数) 流式遍历 + 聚合，约 5.07 亿次，单线程可接受（分钟级），后续可并入现有并行模式。
- 空间复杂度：峰值内存由「整块解压」降为「单 chunk（65536 元素 ≈ 256KB~512KB）」+ 聚合字典（仅非零项数）。
- 边界：空 dataset、maxshape=None 的动态维度、uint64 barcode_idx 转 Integer 的范围校验（越界抛明确异常）；dataset 维度非 1-D 时回退到现有稠密路径。

## 实现要点（防回归）

- 仅新增 API，不改动 `GetMatrix`/`getBuffer` 现有行为，保证向后兼容。
- 复用现有 `DatasetReader.ParseDataChunk` 与 `pipeline.decode` 做 chunk 解码，避免重复实现压缩逻辑。
- 日志沿用现有 sciBASIC 日志约定（如有），避免打印 5 亿行条目造成日志刷屏；仅在切片/分块边界输出进度采样。
- SparseMatrix 类型重载新增时保持 `Sub New(row,col,x)` 现有语义不变。

## 架构设计

```mermaid
graph TD
    A[10x hdf5 文件] --> B[HDF5Reader 通用读取层]
    B --> C[ChunkedDatasetV3.EnumerateChunkArrays(Of T) 流式分块]
    B --> D[GetArray(Of T) 单 dataset 全量]
    C --> E[COO 三元组 row,col,val]
    D --> E
    E --> F[SparseMatrix COO 构造 Sub New row(),col(),x()]
    F --> G[STRaid 封装层]
    G --> H[ReadFeatureSlice: feature_slices/* 分组]
    G --> I[ReadMoleculeInfo: barcode_idx,feature_idx,count 聚合]
    H --> J[FeatureSliceData = SparseMatrix + barcodes + features]
    I --> K[UMI 稀疏矩阵 + barcodes + features]
    J --> L[上层 STRaid 数据模型]
    K --> L
```

## 目录结构

```
G:\GCModeller\src\runtime\sciBASIC#\Data\BinaryData\HDF5\
├── dataset/
│   └── ChunkedDatasetV3.vb   # [MODIFY] 新增 EnumerateChunkArrays(Of T)() As IEnumerable(Of T())，复用 ChunkLookup/DataBTree 逐 chunk 解码并 yield；不改动现有 getBuffer。
├── HDF5Reader.vb             # [MODIFY] 新增通用方法 GetSparseMatrixFromTriplets(row,col,data 三个 dataset 路径) 与流式分块读取封装，调用 SparseMatrix COO 构造。
G:\GCModeller\src\runtime\sciBASIC#\Data_science\Mathematica\Math\Math\Algebra\Matrix.NET\
└── SparseMatrix.vb           # [MODIFY] 必要时为 UnpackData/COO 构造增加 uint32/uint64 三元组重载（按实际类型适配，优先复用现有 Sub New(row(),col(),x() As Double)）。
G:\Erica\src\STRaid\HDF5\
├── TenXTypes.vb             # [MODIFY] 在现有 Read10XHDF5 基础上新增 ReadFeatureSliceHDF5 与 ReadMoleculeInfoHDF5，封装两种 10x 结构；复用 Hdf5ArrayHelpers 与 SparseMatrix。
└── (新增) TenXReader.vb     # [NEW] 统一入口：OpenVisiumHD(file) 返回结构化结果（稀疏矩阵 + barcodes + features），内部按文件名/结构探测分流到两个解析器。
```

## 关键代码结构

```
' ChunkedDatasetV3 新增（基础模块，流式避免 OOM）
Public Iterator Function EnumerateChunkArrays(Of T)() As IEnumerable(Of T())

' SparseMatrix 复用现有 COO 构造（无需新增即可满足）
Public Sub New(row As Integer(), col As Integer(), x As Double(),
               Optional m As Integer = -1, Optional n As Integer = -1)

' STRaid 封装层统一结果
Public Class VisiumHDResult
    Public Property matrix As LinearAlgebra.Matrix.SparseMatrix
    Public Property barcodes As String()
    Public Property features As FeatureData()
End Class
```

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 在实现前深入核对 ChunkedDatasetV3 的 chunk 解码链（DataBTree/ChunkLookup/decodeChunk）与 SparseMatrix 的全部构造/UnpackData 入口，确保新增流式 API 与 COO 构造的参数与现有内部类型精确匹配。
- Expected outcome: 产出准确的调用点与方法签名清单，避免实现阶段因类型/接口不匹配返工。