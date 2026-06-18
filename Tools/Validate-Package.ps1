param(
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$packagePath = Join-Path $root "package.json"
$changelogPath = Join-Path $root "CHANGELOG.md"
$licensePath = Join-Path $root "LICENSE"

if (!(Test-Path $packagePath)) { throw "package.json is missing." }
if (!(Test-Path $changelogPath)) { throw "CHANGELOG.md is missing." }
if (!(Test-Path $licensePath)) { throw "LICENSE is missing." }

$package = Get-Content $packagePath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($package.name)) { throw "package.json name is missing." }
if ([string]::IsNullOrWhiteSpace($package.version)) { throw "package.json version is missing." }
if ($ExpectedVersion -and $package.version -ne $ExpectedVersion) {
    throw "package.json version '$($package.version)' does not match expected '$ExpectedVersion'."
}

$changelog = Get-Content $changelogPath -Raw
if ($changelog -notmatch [regex]::Escape("## [$($package.version)]")) {
    throw "CHANGELOG.md does not contain an entry for version $($package.version)."
}

$required = @(
    "Assets/kkmia/TalkSystem/Scripts/kkmia.TalkSystem.Runtime.asmdef",
    "Assets/kkmia/TalkSystem/Editor/kkmia.TalkSystem.Editor.asmdef",
    "Assets/kkmia/TalkSystem/Tests/Editor/kkmia.TalkSystem.Tests.asmdef",
    "Documentation~/index.md",
    "Samples~/FeatureTour/README.md"
)

foreach ($relative in $required) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path)) { throw "Required package file is missing: $relative" }
}

Write-Host "Package validation passed for $($package.name)@$($package.version)."
