<#
.SYNOPSIS
    Register a "nuget_release|x64" build configuration for all Erica.* VB.NET projects
    and enable NuGet package generation on build.

.DESCRIPTION
    Recursively scans $RootDir for *.vbproj files, matches projects whose <RootNamespace>
    is "Erica" or starts with "Erica.", then for each:
      * appends "nuget_release" to <Configurations> (and ensures "x64" in <Platforms>)
      * ensures <GeneratePackageOnBuild>true</GeneratePackageOnBuild> and
        <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance> in the
        root (unconditional) PropertyGroup
      * adds/updates a
        <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='nuget_release|x64'">
        with <PackageOutputPath> set to the relative path of $NugetDir

    The script is idempotent and auto-creates $NugetDir.

.PARAMETER RootDir
    Root folder to scan for *.vbproj (default: g:/Erica/src).

.PARAMETER NugetDir
    Absolute folder where .nupkg files are dropped (default: G:\Erica\.nuget).
#>
param(
    [string]$RootDir = "g:/Erica/src",
    [string]$NugetDir = "G:\Erica\.nuget"
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Set-PkgChild {
    param($Parent, [string]$Name, [string]$Value)
    if ($null -eq $Parent.$Name) {
        $el = $Parent.OwnerDocument.CreateElement($Name)
        $Parent.AppendChild($el) | Out-Null
    }
    $Parent.$Name = $Value
}

# Compute a relative path compatible with .NET Framework 4.x (Windows PowerShell 5.1),
# which lacks [System.IO.Path]::GetRelativePath.
function Get-RelativePath {
    param([string]$From, [string]$To)
    $fromStr = $From.TrimEnd('\').TrimEnd('/') + '\'
    $toStr   = $To.TrimEnd('\').TrimEnd('/')   + '\'
    $fromUri = New-Object System.Uri($fromStr)
    $toUri   = New-Object System.Uri($toStr)
    $rel = $fromUri.MakeRelativeUri($toUri).ToString()
    return $rel.Replace('/', [System.IO.Path]::DirectorySeparatorChar).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
}

# ---------------------------------------------------------------------------
# Prepare output directory
# ---------------------------------------------------------------------------
if (-not (Test-Path $NugetDir)) {
    New-Item -ItemType Directory -Force -Path $NugetDir | Out-Null
    Write-Host "Created nuget output directory: $NugetDir" -ForegroundColor Cyan
}

# Literal MSBuild condition value (backtick escapes the $( so it stays literal)
$conditionValue = "'`$(Configuration)|`$(Platform)'=='nuget_release|x64'"

$writerSettings = New-Object System.Xml.XmlWriterSettings
$writerSettings.Indent = $true
$writerSettings.IndentChars = "  "
$writerSettings.OmitXmlDeclaration = $true
$writerSettings.Encoding = New-Object System.Text.UTF8Encoding($false)  # UTF-8 no BOM

$projFiles = Get-ChildItem -Path $RootDir -Recurse -Filter *.vbproj

$matched = 0
$changed = 0

foreach ($file in $projFiles) {
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($file.FullName)

    $rootNsNode = $xml.SelectSingleNode('//RootNamespace')
    if ($null -eq $rootNsNode) { continue }
    $ns = $rootNsNode.InnerText
    if (-not ($ns -eq 'Erica' -or $ns.StartsWith('Erica.'))) { continue }

    $matched++
    Write-Host "Processing: $($file.FullName)  (RootNamespace=$ns)" -ForegroundColor Green

    $projectEl = $xml.DocumentElement                      # <Project>
    $propertyGroups = $projectEl.PropertyGroup

    # --- locate root (unconditional) PropertyGroup ---
    $root = $null
    foreach ($pg in $propertyGroups) {
        if ([string]::IsNullOrEmpty($pg.Condition)) { $root = $pg; break }
    }
    if ($null -eq $root) {
        $root = $xml.CreateElement('PropertyGroup')
        $projectEl.AppendChild($root) | Out-Null
    }

    $fileChanged = $false

    # --- Configurations: append nuget_release ---
    $configs = $root.Configurations
    if ([string]::IsNullOrEmpty($configs) -or ($configs -notmatch 'nuget_release')) {
        $newConfigs = (@($configs -split ';' | Where-Object { $_ }) + 'nuget_release') -join ';'
        Set-PkgChild $root 'Configurations' $newConfigs
        $fileChanged = $true
        Write-Host "  + Configurations <- $newConfigs" -ForegroundColor Yellow
    }

    # --- Platforms: ensure x64 ---
    $platforms = $root.Platforms
    if ([string]::IsNullOrEmpty($platforms) -or ($platforms -notmatch 'x64')) {
        $newPlatforms = (@($platforms -split ';' | Where-Object { $_ }) + 'x64') -join ';'
        Set-PkgChild $root 'Platforms' $newPlatforms
        $fileChanged = $true
        Write-Host "  + Platforms <- $newPlatforms" -ForegroundColor Yellow
    }

    # --- root-level package switches (global) ---
    if ($root.GeneratePackageOnBuild -ne 'true') {
        Set-PkgChild $root 'GeneratePackageOnBuild' 'true'
        $fileChanged = $true
        Write-Host "  + GeneratePackageOnBuild = true" -ForegroundColor Yellow
    }
    if ($root.PackageRequireLicenseAcceptance -ne 'true') {
        Set-PkgChild $root 'PackageRequireLicenseAcceptance' 'true'
        $fileChanged = $true
        Write-Host "  + PackageRequireLicenseAcceptance = true" -ForegroundColor Yellow
    }

    # --- relative path from this project to the nuget output dir ---
    $projDir = $file.DirectoryName
    $rel = Get-RelativePath -From $projDir -To $NugetDir
    if ($rel -eq '') { $rel = '.' }

    # --- nuget_release|x64 conditional PropertyGroup ---
    $condGroup = $null
    foreach ($pg in $propertyGroups) {
        if ($pg.Condition -eq $conditionValue) { $condGroup = $pg; break }
    }

    if ($null -eq $condGroup) {
        $condGroup = $xml.CreateElement('PropertyGroup')
        $condGroup.SetAttribute('Condition', $conditionValue) | Out-Null
        $pop = $xml.CreateElement('PackageOutputPath')
        $pop.InnerText = $rel
        $condGroup.AppendChild($pop) | Out-Null
        $projectEl.AppendChild($condGroup) | Out-Null
        $fileChanged = $true
        Write-Host "  + new PropertyGroup ($conditionValue) with PackageOutputPath=$rel" -ForegroundColor Yellow
    }
    else {
        $existing = $condGroup.PackageOutputPath
        if ($existing -ne $rel) {
            Set-PkgChild $condGroup 'PackageOutputPath' $rel
            $fileChanged = $true
            Write-Host "  ~ updated PackageOutputPath=$rel" -ForegroundColor Yellow
        }
        else {
            Write-Host "  = nuget_release|x64 already configured (PackageOutputPath=$rel)" -ForegroundColor DarkGray
        }
    }

    if ($fileChanged) {
        $writer = [System.Xml.XmlWriter]::Create($file.FullName, $writerSettings)
        $xml.Save($writer)
        $writer.Close()
        $changed++
        Write-Host "  -> SAVED $($file.Name)" -ForegroundColor Magenta
    }
    else {
        Write-Host "  = no changes needed" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Matched $matched Erica project(s); updated $changed file(s)." -ForegroundColor Cyan
Write-Host "Verify with: dotnet pack -c nuget_release -p:Platform=x64" -ForegroundColor Cyan
