---
name: update-erica-nuget-metadata
overview: 扫描代码库中 RootNamespace 以 Erica.（及主项目 Erica）开头的 10 个 VB.NET SDK 风格 vbproj 项目，根据各项目 .vb 源码内容生成 NuGet 描述性元数据（Title/Description/PackageTags/PackageReleaseNotes），并统一写入统一的包元数据（PackageIcon、项目/仓库 URL、MIT 授权、版权、作者、公司、产品等）。
todos:
  - id: explore-metadata
    content: 使用 [subagent:code-explorer] 探索 10 个项目 .vb 源码，生成各自的 Title/Description/PackageTags/PackageReleaseNotes 草稿
    status: completed
  - id: update-spatial-vbproj
    content: 回填 Erica/STImaging/STRaid/HEView/bgee 共 5 个 vbproj 的生成与统一元数据
    status: completed
    dependencies:
      - explore-metadata
  - id: update-singlecell-vbproj
    content: 回填 SingleCell 下 5 个 vbproj（STdeconvolve/SingleGRN/SingleExpression/Monocle3/PhenoGraph.NET5）的元数据
    status: completed
    dependencies:
      - explore-metadata
  - id: validate-vbproj
    content: 校验 10 个 vbproj 的 XML 合法性、图标相对路径与统一字段完整性
    status: completed
    dependencies:
      - update-spatial-vbproj
      - update-singlecell-vbproj
---

## 用户需求

扫描代码库中所有 vbproj 项目文件，提取 RootNamespace 以 `Erica.` 起始的项目（并将 RootNamespace 为 `Erica` 的主项目一并纳入，共 10 个项目），根据各项目 `.vb` 源码内容总结 NuGet 包的描述性元数据（Title、Description、PackageTags、PackageReleaseNotes），并更新这些 vbproj 的 NuGet 包元数据。

## 产品概述

对 Erica 系列 10 个 VB.NET 类库项目统一补充并规范化 NuGet 发布元数据，使 `dotnet pack` 产出的包具备统一的项目主页、仓库地址、MIT 授权、版权/作者/公司/产品信息，以及由各项目代码内容生成的标题、描述、标签与发布说明。

## 核心功能

- 依据各项目 `.vb` 源码内容生成 Title / Description / PackageTags / PackageReleaseNotes
- 统一设置 PackageProjectUrl=https://github.com/SMRUCC/Erica、RepositoryUrl=https://github.com/SMRUCC/Erica.git、RepositoryType=git
- 统一 License 为 MIT（移除旧的 PackageLicenseFile）
- 统一 Copyright、Authors、Company、Product 与 PackageIcon（相对名 + 相对 vbproj 的图标源路径）
- 确保 GeneratePackageOnBuild=true，包图标可正确打入包内

## 技术栈与方案

- 目标项目均为 SDK 风格 VB.NET 工程：`<Project Sdk="Microsoft.NET.Sdk">`，NuGet 元数据通过 MSBuild 属性在 `PropertyGroup` 中声明，图标通过 `<None Include ... Pack="True">` 打进包。
- 采用「先由子代理探索源码生成描述文案，再统一回填 MSBuild 属性」的策略，避免人工逐文件猜测功能描述，保证文案与实际代码一致。
- 关键决策：

1. `PackageIcon` 使用相对名 `icons8-jellyfish-96.png`（NuGet 要求包内相对路径），图标源 `<None Include>` 采用相对 vbproj 的路径指向 `G:\Erica\.pkg\icons8-jellyfish-96.png`，按每个项目目录深度计算正确的 `..\.pkg\` 层级（如 STImaging 为 `..\..\.pkg`，PhenoGraph.NET5 为 `..\..\..\..\.pkg`）。
2. 授权统一用 `<PackageLicenseExpression>MIT</PackageLicenseExpression>`，移除各项目中可能冲突的 `<PackageLicenseFile>LICENSE</PackageLicenseFile>`。
3. 仅编辑各工程首个无条件 `PropertyGroup`，保留所有 `Condition=...` 的配置段（Debug/Release 等），避免破坏既有编译配置。
4. `Product` 统一覆盖为 `Erica`（部分项目原有 `Erica Comprehensive Omics`）。

## 实现注意事项

- 复用各项目已有的 `<None Include="..\.pkg\icons8-jellyfish-96.png"><Pack>True</Pack><PackagePath>\</PackagePath></None>` 模式，仅修正相对路径层级；缺失的补建。
- 严格排除 RootNamespace 非 `Erica.`/`Erica` 的工程（如 DeconvTool、SparoMx、各 test/demo 子项目）。
- 文案生成基于真实源码（模块/类/公开 API 注释），不得编造不存在的功能。
- 统一字段在所有 10 个工程中文本完全一致，便于后续批量维护。

## 架构设计

本任务为 MSBuild 工程文件元数据回填，无架构变更，仅修改既有 PropertyGroup/ItemGroup。各项目相互独立，可并行编辑。统一字段如下：

```
&lt;PropertyGroup&gt;
  &lt;Title&gt;（按源码生成）&lt;/Title&gt;
  &lt;Description&gt;（按源码生成）&lt;/Description&gt;
  &lt;PackageTags&gt;（按源码生成）&lt;/PackageTags&gt;
  &lt;PackageReleaseNotes&gt;（按源码生成，含 2026 日期）&lt;/PackageReleaseNotes&gt;
  &lt;PackageIcon&gt;icons8-jellyfish-96.png&lt;/PackageIcon&gt;
  &lt;PackageProjectUrl&gt;https://github.com/SMRUCC/Erica&lt;/PackageProjectUrl&gt;
  &lt;RepositoryUrl&gt;https://github.com/SMRUCC/Erica.git&lt;/RepositoryUrl&gt;
  &lt;RepositoryType&gt;git&lt;/RepositoryType&gt;
  &lt;PackageLicenseExpression&gt;MIT&lt;/PackageLicenseExpression&gt;
  &lt;Copyright&gt;Copyright © SMRUCC genomics, GuiLin China, 2026&lt;/Copyright&gt;
  &lt;Authors&gt;xieguigang@metabolomics.ac.cn&lt;/Authors&gt;
  &lt;Company&gt;SMRUCC genomics institute&lt;/Company&gt;
  &lt;Product&gt;Erica&lt;/Product&gt;
  &lt;GeneratePackageOnBuild&gt;true&lt;/GeneratePackageOnBuild&gt;
&lt;/PropertyGroup&gt;
&lt;None Include="..\..\.pkg\icons8-jellyfish-96.png"&gt;
  &lt;Pack&gt;True&lt;/Pack&gt;
  &lt;PackagePath&gt;\&lt;/PackagePath&gt;
&lt;/None&gt;
```

## 目录结构（全部为 [MODIFY]）

```
g:/Erica/src/
├── Erica/Erica.vbproj                                          # [MODIFY] RootNamespace=Erica 主项目；补齐 Title/Description/Tags/ReleaseNotes 及统一字段，移除旧冲突项
├── STImaging/STImaging.vbproj                                 # [MODIFY] Erica.Analysis.SpatialTissue.Imaging；覆盖 Product，规范化元数据与图标相对路径
├── STRaid/STRaid.vbproj                                       # [MODIFY] Erica.Analysis.SpatialTissue.RaidData
├── HEView/HEView.vbproj                                       # [MODIFY] Erica.Analysis.SpatialTissue.HEView
├── bgee/bgee.vbproj                                           # [MODIFY] Erica.Analysis.KnowledgeBase.Bgee
├── SingleCell/STdeconvolve/STdeconvolve/STdeconvolve.NET5.vbproj # [MODIFY] Erica.Analysis.SingleCell.SpatialDeconvolve
├── SingleCell/SingleGRN/SingleGRN.vbproj                      # [MODIFY] Erica.Analysis.SingleCell.VirtualGRN
├── SingleCell/SingleExpression/SingleExpression.vbproj        # [MODIFY] Erica.Analysis.SingleCell.Expression；补建图标 <None Include> 相对路径
├── SingleCell/Monocle3/Monocle3.vbproj                        # [MODIFY] Erica.Analysis.SingleCell.Monocle3
└── SingleCell/PhenoGraph/PhenoGraph/PhenoGraph.NET5.vbproj     # [MODIFY] Erica.Analysis.SingleCell.PhenoGraph；移除 PackageLicenseFile，改用 PackageLicenseExpression MIT
```

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 对 10 个目标工程目录递归探索 `.vb` 源文件，理解各项目模块/类/公开 API 与功能，生成符合其代码内容的 Title/Description/PackageTags/PackageReleaseNotes 草稿。
- Expected outcome: 输出每个项目（按 RootNamespace 对应）的结构化元数据草稿，供回填 vbproj 使用，文案须与真实代码一致、无虚构功能。