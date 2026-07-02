param(
    [string]$UnityPath = "",
    [string]$UnityVersion = "6000.0.40f1",
    [string]$ExpectedVersion = "",
    [int]$TimeoutSeconds = 900,
    [switch]$SkipUnity,
    [switch]$SkipConsumerInstall,
    [switch]$ImportSamples
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $root "Temp/ReleaseChecks"

function Invoke-Check([string]$Name, [scriptblock]$Body) {
    Write-Host "==> $Name"
    & $Body
    Write-Host "OK: $Name"
}

function Find-UnityEditor([string]$RequestedPath, [string]$RequestedVersion) {
    if (![string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (!(Test-Path $RequestedPath)) { throw "Unity editor was not found: $RequestedPath" }
        return (Resolve-Path $RequestedPath).Path
    }

    if (![string]::IsNullOrWhiteSpace($env:UNITY_EDITOR) -and (Test-Path $env:UNITY_EDITOR)) {
        return (Resolve-Path $env:UNITY_EDITOR).Path
    }

    $commandNames = @("unity-editor", "Unity")
    foreach ($commandName in $commandNames) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($null -ne $command) { return $command.Source }
    }

    $isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
    $isMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)

    $candidates = @()
    if ($isWindows) {
        $hubRoot = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor"
        if (Test-Path $hubRoot) {
            $exact = Join-Path $hubRoot "$RequestedVersion/Editor/Unity.exe"
            if (Test-Path $exact) { return $exact }

            $candidates += Get-ChildItem $hubRoot -Directory -Filter "6000.*" |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName "Editor/Unity.exe" }
        }
    }
    elseif ($isMacOS) {
        $hubRoot = "/Applications/Unity/Hub/Editor"
        if (Test-Path $hubRoot) {
            $exact = Join-Path $hubRoot "$RequestedVersion/Unity.app/Contents/MacOS/Unity"
            if (Test-Path $exact) { return $exact }

            $candidates += Get-ChildItem $hubRoot -Directory -Filter "6000.*" |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName "Unity.app/Contents/MacOS/Unity" }
        }
    }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw "Unity 6000.x editor was not found. Pass -UnityPath, set UNITY_EDITOR, or use -SkipUnity."
}

function Join-ProcessArguments([string[]]$Arguments) {
    return ($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + $_.Replace('"', '\"') + '"'
        }
        else {
            $_
        }
    }) -join " "
}

function Stop-ProcessTree([int]$ProcessId) {
    $children = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.ParentProcessId -eq $ProcessId }
    foreach ($child in $children) {
        Stop-ProcessTree $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Invoke-UnityTests([string]$UnityEditor, [string]$Mode) {
    if (!(Test-Path $artifactsRoot)) {
        New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
    }

    $logPath = Join-Path $artifactsRoot "unity-$Mode.log"
    $resultPath = Join-Path $artifactsRoot "TestResults-$Mode.xml"
    $arguments = @(
        "-batchmode",
        "-quit",
        "-projectPath", $root,
        "-runTests",
        "-testPlatform", $Mode,
        "-testResults", $resultPath,
        "-logFile", $logPath
    )

    $startArguments = @{
        FilePath = $UnityEditor
        ArgumentList = (Join-ProcessArguments $arguments)
        PassThru = $true
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $startArguments["WindowStyle"] = "Hidden"
    }

    $process = Start-Process @startArguments
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-ProcessTree $process.Id
        $tail = ""
        if (Test-Path $logPath) {
            $tail = (Get-Content $logPath -Tail 120) -join [Environment]::NewLine
        }
        throw "Unity $Mode tests timed out after $TimeoutSeconds seconds. Log tail:$([Environment]::NewLine)$tail"
    }

    if ($process.ExitCode -ne 0) {
        $tail = ""
        if (Test-Path $logPath) {
            $tail = (Get-Content $logPath -Tail 120) -join [Environment]::NewLine
        }
        throw "Unity $Mode tests exited with code $($process.ExitCode). Log tail:$([Environment]::NewLine)$tail"
    }

    if (!(Test-Path $resultPath)) {
        throw "Unity $Mode tests did not produce $resultPath."
    }

    $result = [xml](Get-Content $resultPath -Raw)
    if ($result.'test-run'.result -ne "Passed") {
        throw "Unity $Mode tests did not pass. Result: $($result.'test-run'.result)"
    }
}

function Test-DocumentationLinks {
    $requiredFiles = @(
        "README.md",
        "Documentation~/index.md",
        "Documentation~/installation.md",
        "Documentation~/runtime-api.md",
        "Documentation~/editor-tools.md",
        "Documentation~/release-checklist.md",
        "Documentation~/images/CAPTURE_GUIDE.md"
    )

    foreach ($relative in $requiredFiles) {
        $path = Join-Path $root $relative
        if (!(Test-Path $path)) { throw "Required documentation file is missing: $relative" }
    }

    $markdownFiles = Get-ChildItem $root -Recurse -File -Include "*.md" |
        Where-Object {
            $_.FullName -notmatch "\\Library\\" -and
            $_.FullName -notmatch "\\Temp\\" -and
            $_.FullName -notmatch "\\Logs\\"
        }

    $linkPattern = [regex]'\]\(([^)]+)\)'
    foreach ($file in $markdownFiles) {
        $content = Get-Content $file.FullName -Raw
        $content = [regex]::Replace($content, '<!--.*?-->', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
        foreach ($match in $linkPattern.Matches($content)) {
            $target = $match.Groups[1].Value.Trim()
            if ($target.Length -eq 0 -or $target.StartsWith("#")) { continue }
            if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') { continue }

            $pathOnly = $target.Split("#")[0]
            if ($pathOnly.Length -eq 0) { continue }

            $resolved = Join-Path $file.DirectoryName $pathOnly
            if (!(Test-Path $resolved)) {
                throw "Broken markdown link in $($file.FullName): $target"
            }
        }
    }
}

Invoke-Check "Package metadata" {
    $packageArgs = @()
    if ($ExpectedVersion) { $packageArgs += @("-ExpectedVersion", $ExpectedVersion) }
    & (Join-Path $PSScriptRoot "Validate-Package.ps1") @packageArgs
}

Invoke-Check "Documentation links" {
    Test-DocumentationLinks
}

if (!$SkipUnity) {
    $unityEditor = Find-UnityEditor $UnityPath $UnityVersion
    Write-Host "Using Unity editor: $unityEditor"

    Invoke-Check "Unity EditMode tests" {
        Invoke-UnityTests $unityEditor "EditMode"
    }

    Invoke-Check "Unity PlayMode tests" {
        Invoke-UnityTests $unityEditor "PlayMode"
    }
}
else {
    Write-Host "Skipping Unity EditMode/PlayMode tests."
}

if (!$SkipConsumerInstall) {
    Invoke-Check "Clean consumer install" {
        $consumerArgs = @("-UnityVersion", $UnityVersion, "-TimeoutSeconds", $TimeoutSeconds)
        if ($UnityPath) { $consumerArgs += @("-UnityPath", $UnityPath) }
        if ($ImportSamples) { $consumerArgs += "-ImportSamples" }
        & (Join-Path $PSScriptRoot "Validate-ConsumerInstall.ps1") @consumerArgs
    }
}
else {
    Write-Host "Skipping clean consumer install."
}

Write-Host "Release checks completed."
