param(
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$packagePath = Join-Path $root "package.json"
$changelogPath = Join-Path $root "CHANGELOG.md"
$licensePath = Join-Path $root "LICENSE"

function Get-RelativePath([string]$Path) {
    $rootPath = [System.IO.Path]::GetFullPath($root).TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootUri = New-Object System.Uri($rootPath)
    $pathUri = New-Object System.Uri($fullPath)
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString()).Replace("\", "/")
}

function Assert-PackageDependency([object]$Package, [string]$Name, [string]$Version, [string]$Reason) {
    if ($null -eq $Package.dependencies) {
        throw "package.json dependencies are missing. Required '$Name' for $Reason."
    }

    $dependency = $Package.dependencies.PSObject.Properties[$Name]
    if ($null -eq $dependency) {
        throw "package.json dependencies must include '$Name' for $Reason."
    }

    if ($dependency.Value -ne $Version) {
        throw "package.json dependency '$Name' must be '$Version' but was '$($dependency.Value)'."
    }
}

if (!(Test-Path $packagePath)) { throw "package.json is missing." }
if (!(Test-Path $changelogPath)) { throw "CHANGELOG.md is missing." }
if (!(Test-Path $licensePath)) { throw "LICENSE is missing." }

$package = Get-Content $packagePath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($package.name)) { throw "package.json name is missing." }
if ([string]::IsNullOrWhiteSpace($package.version)) { throw "package.json version is missing." }
if ($ExpectedVersion -and $package.version -ne $ExpectedVersion) {
    throw "package.json version '$($package.version)' does not match expected '$ExpectedVersion'."
}

Assert-PackageDependency $package "com.unity.ugui" "2.0.0" "UnityEngine.UI and Unity.TextMeshPro runtime assemblies on Unity 6000.x"
& (Join-Path $PSScriptRoot "Assert-NoUnguardedLegacyInput.ps1") -Root $root

$changelog = Get-Content $changelogPath -Raw
if ($changelog -notmatch [regex]::Escape("## [$($package.version)]")) {
    throw "CHANGELOG.md does not contain an entry for version $($package.version)."
}

$required = @(
    "Assets/kkmia/TalkSystem/Scripts/kkmia.TalkSystem.Runtime.asmdef",
    "Assets/kkmia/TalkSystem/Editor/kkmia.TalkSystem.Editor.asmdef",
    "Assets/kkmia/TalkSystem/Tests/Editor/kkmia.TalkSystem.Tests.asmdef",
    "Assets/kkmia/TalkSystem/Tests/PlayMode/kkmia.TalkSystem.PlayModeTests.asmdef",
    "Tools/Validate-ConsumerInstall.ps1",
    "Tools/Assert-NoUnguardedLegacyInput.ps1",
    "Tools/Invoke-ReleaseChecks.ps1",
    "Documentation~/index.md",
    "Documentation~/release-checklist.md",
    "Samples~/FeatureTour/README.md"
)

foreach ($relative in $required) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path)) { throw "Required package file is missing: $relative" }
}

$runtimeAsmdefPath = Join-Path $root "Assets/kkmia/TalkSystem/Scripts/kkmia.TalkSystem.Runtime.asmdef"
$runtimeAsmdef = Get-Content $runtimeAsmdefPath -Raw | ConvertFrom-Json
$referenceDependencies = @{
    "Unity.TextMeshPro" = "com.unity.ugui"
    "UnityEngine.UI" = "com.unity.ugui"
}

foreach ($reference in $runtimeAsmdef.references) {
    if (!$referenceDependencies.ContainsKey($reference)) { continue }

    $dependencyName = $referenceDependencies[$reference]
    if ($null -eq $package.dependencies.PSObject.Properties[$dependencyName]) {
        throw "Runtime asmdef reference '$reference' requires package dependency '$dependencyName'."
    }
}

$sampleOnlyExternalReferences = @{
    "01614664b831546d2ae94a42149d80ac" = "com.unity.inputsystem InputSystemUIInputModule"
    "ca9f5fa95ffab41fb9a615ab714db018" = "com.unity.inputsystem default UI actions"
    "a79441f348de89743a2939f4d699eac1" = "com.unity.render-pipelines.universal camera data"
    "073797afb82c5a1438f328866b10b3f0" = "com.unity.render-pipelines.universal 2D light"
}

$sampleRoots = @(
    (Join-Path $root "Samples~"),
    (Join-Path $root "Assets/kkmia/TalkSystem/Demo")
)

foreach ($sampleRoot in $sampleRoots) {
    if (!(Test-Path $sampleRoot)) { continue }

    $assetFiles = Get-ChildItem $sampleRoot -Recurse -File -Include "*.unity", "*.prefab"
    foreach ($assetFile in $assetFiles) {
        $content = Get-Content $assetFile.FullName -Raw
        foreach ($guid in $sampleOnlyExternalReferences.Keys) {
            if ($content -match [regex]::Escape($guid)) {
                throw "Sample asset '$(Get-RelativePath $assetFile.FullName)' references $($sampleOnlyExternalReferences[$guid]). Keep samples clean-install safe or declare the dependency explicitly."
            }
        }
    }
}

Write-Host "Package validation passed for $($package.name)@$($package.version)."
