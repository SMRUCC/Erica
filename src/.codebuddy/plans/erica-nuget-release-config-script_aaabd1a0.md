---
name: erica-nuget-release-config-script
overview: 编写一个 PowerShell 脚本，递归扫描 g:/Erica/src 下所有 *.vbproj，匹配 RootNamespace 为 "Erica" 或以 "Erica." 起始的 10 个项目，为每个项目：① 计算到 G:\Erica\.nuget 的相对路径并写入 nuget_release|x64 条件 PropertyGroup 的 PackageOutputPath；② 将 nuget_release 注册进 Configurations 并确认 x64 在 Platforms；③ 在根(无条件) PropertyGroup 中设置 GeneratePackageOnBuild 与 PackageRequireLicenseAcceptance 为 true。脚本幂等，并自动创建 G:\Erica\.nuget 目录。
todos:
  - id: write-script
    content: 编写 g:/Erica/src/setup-nuget-release.ps1（扫描匹配、相对路径、XML 幂等修改）
    status: completed
  - id: run-script
    content: 运行脚本对 10 个 Erica 项目应用 nuget_release|x64 配置与全局开关
    status: completed
    dependencies:
      - write-script
  - id: verify-report
    content: 校验各 vbproj 节点并汇总修改报告，提示 dotnet pack 验证方式
    status: completed
    dependencies:
      - run-script
---

## 用户需求

编写一个 PowerShell 脚本，扫描 `g:/Erica/src` 下所有 `*.vbproj` 项目文件，对其中 `<RootNamespace>` 为 `Erica` 或以 `Erica.` 起始的项目（共 10 个），进行如下 NuGet 发布配置修改：

## 核心功能

- 递归扫描并匹配目标 vbproj 项目（基于 `<RootNamespace>` 文本判定）。
- 为每个项目创建 `nuget_release | x64` 编译配置：
- 将 `nuget_release` 注册进 `<Configurations>` 列表，并确认 `x64` 已存在于 `<Platforms>`。
- 新增/更新条件 `<PropertyGroup Condition="...=='nuget_release|x64'">`，把 `<PackageOutputPath>` 设为相对该 vbproj 文件指向 `G:\Erica\.nuget` 的相对路径。
- 在根（无条件）PropertyGroup 中确保 `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` 与 `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>` 存在并置为 true。
- 自动创建 `G:\Erica\.nuget` 输出目录。
- 脚本幂等：重复运行不会产生重复节点或脏数据；运行后可用 `dotnet pack -c nuget_release -p:Platform=x64` 验证。

## 技术栈

- 语言：PowerShell 5.1+/7.x（工作区为 Windows，Shell 为 PowerShell）。
- 解析方式：.NET `[System.Xml.XmlDocument]`（`[xml]` 类型加速器），无第三方依赖。
- 目标文件：SDK 风格 VB.NET 项目（`g:/Erica/src` 下 10 个 Erica 系列 `.vbproj`）。

## 实现方案

脚本 `g:/Erica/src/setup-nuget-release.ps1` 以参数化根目录（默认 `g:/Erica/src`）递归查找 `*.vbproj`，逐个加载 XML 处理。判定条件：`$ns -eq 'Erica' -or $ns.StartsWith('Erica.')`。相对路径用 `[System.IO.Path]::GetRelativePath($projDir, 'G:\Erica\.nuget')` 计算（Windows 下返回反斜杠串），去掉尾部分隔符后写入 `<PackageOutputPath>`。

### 关键决策与权衡

1. **根 PropertyGroup 定位**：取第一个无 `Condition` 属性的 `<PropertyGroup>` 作为根组（经验证：`Erica.vbproj` 与 `STImaging.vbproj` 均为首节点根组，含 `Configurations`/`Platforms`/`GeneratePackageOnBuild` 等）。两个开关写在此处，保证对所有配置（含 Debug/Release/nuget_release）生效，符合用户"全局"意图。
2. **`<PackageOutputPath>` 放在条件组**：仅在 `nuget_release|x64` 时把 nupkg 输出到 `.nuget`，避免污染其它配置的输出目录；与用户"创建 nuget_release 配置并设输出到 .nuget"一致。
3. **条件组 Condition 字符串精确匹配**：文件内现有条件形如 `Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"`（属性值为字面量，含 `$(...)` 文本）。新建时 PowerShell 字符串用 `` `'`$(Configuration)... `` 转义 ` 生成完全一致的字面值，并通过属性值字符串比对实现幂等查找/更新。
4. **相对路径而非绝对路径**：直接写绝对路径会让 MSBuild 把 .nupkg 落到绝对位置且跨机不可移植；相对路径对任意检出位置均有效，且 `.nuget` 目录已被仓库 `.gitignore` 忽略（与既有 `..\..\.nuget` 写法一致）。

### 性能与可靠性

- 仅递归遍历 `*.vbproj`（数量级为十几个），无热路径问题。
- 幂等：每个节点先查后改，重复运行安全。
- 保存前 `New-Item -ItemType Directory -Force -Path 'G:\Erica\.nuget'` 确保输出目录存在。
- 保存采用 `$xml.Save($path)`，保留 XML 声明；SDK 项目无 xmlns，新建元素不会产生 `xmlns=""`，格式化差异对 MSBuild 无影响。
- 不修改非 Erica 项目，不触碰 `<Configurations>` 中已有项（仅追加 `nuget_release`），控制改动范围。

## 实现要点（执行细节）

- 参数：`param([string]$RootDir = "g:/Erica/src", [string]$NugetDir = "G:\Erica\.nuget")`。
- 匹配后输出将要修改的文件清单（便于确认）。
- `<Configurations>`：若不存在或不含 `nuget_release` 则按 `;` 追加。
- `<Platforms>`：若不存在或不含 `x64` 则追加。
- 根组开关：存在则置 `true`，不存在则 `CreateElement` 后 `AppendChild`。
- 条件组：用 XPath/属性循环查找 `Condition` 含 `nuget_release|x64` 的 PropertyGroup；命中则更新 `<PackageOutputPath>` 子元素，未命中则 `CreateElement('PropertyGroup')` + `SetAttribute('Condition', ...)` + 追加到 `</Project>` 末尾。
- 每个文件保存后打印修改摘要（新增/更新了哪些节点）。

## 架构设计

脚本为单文件独立工具，无额外模块依赖；输入为目录，输出为就地修改的 vbproj + 创建的 `.nuget` 目录。可重复运行，便于后续新增 Erica 项目时复跑。
（结构单一，无需 Mermaid 图）

## 目录结构

```
g:/Erica/src/
└── setup-nuget-release.ps1   # [NEW] 主脚本。递归扫描 *.vbproj，按 RootNamespace 匹配 Erica 系列项目，
                              #       注册 nuget_release 配置、写入 nuget_release|x64 条件组的相对 PackageOutputPath，
                              #       并在根 PropertyGroup 置 GeneratePackageOnBuild/PackageRequireLicenseAcceptance 为 true，
                              #       自动创建 G:\Erica\.nuget，整体幂等。
```

被脚本就地修改的目标文件（10 个，运行脚本后变更）：

```
g:/Erica/src/Erica/Erica.vbproj                                   # [MODIFY] 根组置两开关为 true；Configurations 追加 nuget_release；新增 nuget_release|x64 条件组(PackageOutputPath=..\..\.nuget)
g:/Erica/src/STImaging/STImaging.vbproj                           # [MODIFY] 同上；其根组已有 PackageOutputPath，条件组追加相对路径
g:/Erica/src/STRaid/STRaid.vbproj                                # [MODIFY] 同上
g:/Erica/src/HEView/HEView.vbproj                                # [MODIFY] 同上
g:/Erica/src/bgee/bgee.vbproj                                    # [MODIFY] 同上
g:/Erica/src/SingleCell/STdeconvolve/STdeconvolve/STdeconvolve.NET5.vbproj  # [MODIFY] 同上(PackageOutputPath=..\..\..\..\.nuget)
g:/Erica/src/SingleCell/SingleGRN/SingleGRN.vbproj               # [MODIFY] 同上(PackageOutputPath=..\..\..\.nuget)
g:/Erica/src/SingleCell/SingleExpression/SingleExpression.vbproj # [MODIFY] 同上
g:/Erica/src/SingleCell/Monocle3/Monocle3.vbproj                 # [MODIFY] 同上
g:/Erica/src/SingleCell/PhenoGraph/PhenoGraph/PhenoGraph.NET5.vbproj  # [MODIFY] 同上(PackageOutputPath=..\..\..\..\.nuget)
```

## 关键代码结构（脚本核心逻辑示意）

```
param([string]$RootDir = "g:/Erica/src", [string]$NugetDir = "G:\Erica\.nuget")

$cond = "'`$(Configuration)|`$(Platform)'=='nuget_release|x64'"
$rel  = [System.IO.Path]::GetRelativePath($projDir, $NugetDir).TrimEnd('\','/').Replace('/','\')

# 根组定位
$root = $xml.Project.PropertyGroup | Where-Object { -not $_.Condition } | Select-Object -First 1
# Configurations 追加
if ($root.Configurations -notmatch 'nuget_release') {
    $root.Configurations = ($root.Configurations -split ';' + 'nuget_release') -join ';'
}
# 两开关置 true（缺失则新建子元素）
# 条件组查找/新建并更新 PackageOutputPath = $rel
```