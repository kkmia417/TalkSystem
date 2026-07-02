param(
    [string]$Root = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

$rootPath = [System.IO.Path]::GetFullPath($Root)
$violations = New-Object System.Collections.Generic.List[string]

function Get-RelativePath([string]$Path) {
    $rootWithSlash = $rootPath.TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
    $rootUri = New-Object System.Uri($rootWithSlash)
    $pathUri = New-Object System.Uri([System.IO.Path]::GetFullPath($Path))
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString()).Replace("\", "/")
}

$files = Get-ChildItem $rootPath -Recurse -File -Filter "*.cs" |
    Where-Object {
        $_.FullName -notmatch "\\Library\\" -and
        $_.FullName -notmatch "\\Temp\\" -and
        $_.FullName -notmatch "\\Logs\\"
    }

foreach ($file in $files) {
    $legacyDepth = 0
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()

        if ($line -match '^#if\b' -or $line -match '^#elif\b') {
            if ($line -match 'ENABLE_LEGACY_INPUT_MANAGER') {
                $legacyDepth++
            }
        }
        elseif ($line -match '^#else\b') {
            if ($legacyDepth -gt 0) {
                $legacyDepth--
            }
        }
        elseif ($line -match '^#endif\b') {
            if ($legacyDepth -gt 0) {
                $legacyDepth--
            }
        }

        if ($line -cmatch '\bInput\.' -and $legacyDepth -le 0) {
            $violations.Add("$(Get-RelativePath $file.FullName):$($i + 1): unguarded UnityEngine.Input API call")
        }
    }
}

if ($violations.Count -gt 0) {
    throw "Unguarded legacy input calls found:`n$($violations -join [Environment]::NewLine)"
}

Write-Host "No unguarded legacy UnityEngine.Input API calls found."
