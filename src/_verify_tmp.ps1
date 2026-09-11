$err = 0
foreach ($f in Get-ChildItem -Path 'g:/Erica/src' -Recurse -Filter *.vbproj) {
    $x = [xml](Get-Content $f.FullName)
    $ns = $x.SelectSingleNode('//RootNamespace').InnerText
    if ($ns -eq 'Erica' -or $ns.StartsWith('Erica.')) {
        $root = $x.Project.PropertyGroup | Where-Object { -not $_.Condition } | Select-Object -First 1
        $cg = $x.Project.PropertyGroup | Where-Object { $_.Condition -eq "'`$(Configuration)|`$(Platform)'=='nuget_release|x64'" } | Select-Object -First 1
        $ok = ($root.Configurations -match 'nuget_release') -and ($root.GeneratePackageOnBuild -eq 'true') -and ($root.PackageRequireLicenseAcceptance -eq 'true') -and ($null -ne $cg) -and ($cg.PackageOutputPath -ne '')
        if (-not $ok) { $err++ }
        Write-Host ('{0,-40} cfg={1} gen={2} lic={3} pop={4}' -f $ns, $root.Configurations.Contains('nuget_release'), $root.GeneratePackageOnBuild, $root.PackageRequireLicenseAcceptance, $cg.PackageOutputPath)
    }
}
Write-Host ('CHECK errors: ' + $err)
if (Test-Path 'G:\Erica\.nuget') { Write-Host '.nuget directory EXISTS' } else { Write-Host '.nuget MISSING' }
