param(
    [string]$UnityPath = "",
    [string]$PackageSource = "",
    [string]$UnityVersion = "6000.0.40f1",
    [int]$TimeoutSeconds = 600,
    [ValidateSet("Old", "New", "Both")]
    [string]$ActiveInputHandling = "Old",
    [switch]$InstallInputSystem,
    [switch]$ImportSamples,
    [switch]$KeepProject
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$useStagedLocalPackage = [string]::IsNullOrWhiteSpace($PackageSource)

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

    throw "Unity 6000.x editor was not found. Pass -UnityPath or set UNITY_EDITOR."
}

function Write-TextFile([string]$Path, [string]$Value) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Write-JsonFile([string]$Path, [object]$Value) {
    Write-TextFile $Path ($Value | ConvertTo-Json -Depth 16)
}

function Get-ActiveInputHandlerValue([string]$Mode) {
    switch ($Mode) {
        "Old" { return 0 }
        "New" { return 1 }
        "Both" { return 2 }
        default { throw "Unsupported Active Input Handling mode: $Mode" }
    }
}

function Copy-IfExists([string]$SourceRoot, [string]$StageRoot, [string]$RelativePath) {
    $source = Join-Path $SourceRoot $RelativePath
    if (!(Test-Path $source)) { return }

    $destination = Join-Path $StageRoot $RelativePath
    $destinationParent = Split-Path -Parent $destination
    if (!(Test-Path $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
}

function Copy-PackageToStage([string]$SourceRoot, [string]$StageRoot) {
    New-Item -ItemType Directory -Path $StageRoot -Force | Out-Null

    $rootPackageFiles = @(
        "package.json",
        "package.json.meta",
        "README.md",
        "README.md.meta",
        "CHANGELOG.md",
        "CHANGELOG.md.meta",
        "LICENSE",
        "LICENSE.meta",
        "CONTRIBUTING.md",
        "CONTRIBUTING.md.meta",
        "Documentation~",
        "Documentation~.meta",
        "Samples~",
        "Samples~.meta",
        "Tools",
        "Tools.meta",
        "Assets.meta"
    )

    foreach ($relativePath in $rootPackageFiles) {
        Copy-IfExists $SourceRoot $StageRoot $relativePath
    }

    New-Item -ItemType Directory -Path (Join-Path $StageRoot "Assets") -Force | Out-Null
    Copy-IfExists $SourceRoot $StageRoot "Assets/kkmia"
    Copy-IfExists $SourceRoot $StageRoot "Assets/kkmia.meta"
}

function Test-IsWindows() {
    return [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
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

function Test-UnityLog([string]$LogPath) {
    if (!(Test-Path $LogPath)) { return }

    $content = Get-Content $LogPath -Raw
    $errorPatterns = @(
        "Compilation failed",
        "Scripts have compiler errors",
        "error CS[0-9]{4}",
        "Failed to resolve packages"
    )

    foreach ($pattern in $errorPatterns) {
        if ($content -match $pattern) {
            $tail = (Get-Content $LogPath -Tail 120) -join [Environment]::NewLine
            throw "Unity reported an install or compile error matching '$pattern'. Log tail:$([Environment]::NewLine)$tail"
        }
    }
}

function Stop-ProcessTree([int]$ProcessId) {
    $children = Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $ProcessId }
    foreach ($child in $children) {
        Stop-ProcessTree $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Assert-ConsumerResolution([string]$ProjectPath) {
    $lockPath = Join-Path $ProjectPath "Packages/packages-lock.json"
    if (!(Test-Path $lockPath)) {
        throw "Consumer project did not produce Packages/packages-lock.json."
    }

    $lock = Get-Content $lockPath -Raw | ConvertFrom-Json
    foreach ($packageName in @("com.kkmia.talksystem", "com.unity.ugui")) {
        if ($null -eq $lock.dependencies.PSObject.Properties[$packageName]) {
            throw "Consumer package lock is missing '$packageName'."
        }
    }
}

function Invoke-UnityCheck([string]$UnityEditor, [string]$ProjectPath, [string]$LogPath, [string[]]$ExtraArgs) {
    $arguments = @(
        "-batchmode",
        "-quit",
        "-nographics",
        "-projectPath", $ProjectPath,
        "-logFile", $LogPath
    ) + $ExtraArgs

    $startArguments = @{
        FilePath = $UnityEditor
        ArgumentList = (Join-ProcessArguments $arguments)
        PassThru = $true
    }

    if (Test-IsWindows) {
        $startArguments["WindowStyle"] = "Hidden"
    }

    $process = Start-Process @startArguments
    if ($null -eq $process) {
        throw "Unity process did not start: $UnityEditor"
    }

    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-ProcessTree $process.Id
        $tail = ""
        if (Test-Path $LogPath) {
            $tail = (Get-Content $LogPath -Tail 120) -join [Environment]::NewLine
        }

        throw "Unity timed out after $TimeoutSeconds seconds. Log tail:$([Environment]::NewLine)$tail"
    }

    if ($process.ExitCode -ne 0) {
        $tail = ""
        if (Test-Path $LogPath) {
            $tail = (Get-Content $LogPath -Tail 120) -join [Environment]::NewLine
        }

        throw "Unity exited with code $($process.ExitCode). Log tail:$([Environment]::NewLine)$tail"
    }

    Test-UnityLog $LogPath
}

& (Join-Path $PSScriptRoot "Validate-Package.ps1")

$unityEditor = Find-UnityEditor $UnityPath $UnityVersion
Write-Host "Using Unity editor: $unityEditor"
$consumerRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("TalkSystemConsumerInstall-" + [System.Guid]::NewGuid().ToString("N"))
$packageStageRoot = $null
$logPath = Join-Path $consumerRoot "unity-consumer-install.log"

try {
    if ($useStagedLocalPackage) {
        $packageStageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("TalkSystemPackageStage-" + [System.Guid]::NewGuid().ToString("N"))
        Copy-PackageToStage $root $packageStageRoot
        $PackageSource = "file:" + $packageStageRoot.Replace("\", "/")
        Write-Host "Using staged package source: $PackageSource"
    }

    New-Item -ItemType Directory -Path (Join-Path $consumerRoot "Assets") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $consumerRoot "Packages") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $consumerRoot "ProjectSettings") -Force | Out-Null

    $manifest = [ordered]@{
        dependencies = [ordered]@{
            "com.kkmia.talksystem" = $PackageSource
        }
    }
    if ($InstallInputSystem) {
        $manifest.dependencies["com.unity.inputsystem"] = "1.11.2"
    }
    Write-JsonFile (Join-Path $consumerRoot "Packages/manifest.json") $manifest

    Write-TextFile (Join-Path $consumerRoot "ProjectSettings/ProjectVersion.txt") @"
m_EditorVersion: $UnityVersion
m_EditorVersionWithRevision: $UnityVersion
"@

    $sourceProjectSettings = Join-Path $root "ProjectSettings/ProjectSettings.asset"
    $targetProjectSettings = Join-Path $consumerRoot "ProjectSettings/ProjectSettings.asset"
    $activeInputHandler = Get-ActiveInputHandlerValue $ActiveInputHandling
    if (Test-Path $sourceProjectSettings) {
        $settings = Get-Content $sourceProjectSettings -Raw
        $settings = [regex]::Replace($settings, 'activeInputHandler:\s*\d+', "activeInputHandler: $activeInputHandler")
        Write-TextFile $targetProjectSettings $settings
    }

    Invoke-UnityCheck $unityEditor $consumerRoot $logPath @()
    Assert-ConsumerResolution $consumerRoot

    if ($ImportSamples) {
        New-Item -ItemType Directory -Path (Join-Path $consumerRoot "Assets/Editor") -Force | Out-Null
        Write-TextFile (Join-Path $consumerRoot "Assets/Editor/TalkSystemConsumerInstallCheck.cs") @"
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using kkmia.TalkSystem;

public static class TalkSystemConsumerInstallCheck
{
    public static void VerifyPackageAndExit()
    {
        var type = typeof(DialogueManager);
        var packageInfo = PackageInfo.FindForPackageName("com.kkmia.talksystem");
        if (packageInfo == null)
            throw new Exception("com.kkmia.talksystem was not resolved in the consumer project.");

        File.WriteAllText("Assets/TalkSystemConsumerInstallCheck.txt", packageInfo.version + " " + type.FullName);
        EditorApplication.Exit(0);
    }

    public static void ImportSamplesAndExit()
    {
        var packageInfo = PackageInfo.FindForPackageName("com.kkmia.talksystem");
        if (packageInfo == null)
            throw new Exception("com.kkmia.talksystem was not resolved before sample import.");

        var samples = Sample.FindByPackage(packageInfo.name, packageInfo.version).ToArray();
        if (samples.Length == 0)
            throw new Exception("com.kkmia.talksystem exposes no importable samples.");

        foreach (var sample in samples)
            sample.Import(Sample.ImportOptions.OverridePreviousImports);

        AssetDatabase.Refresh();
        File.WriteAllText("Assets/TalkSystemConsumerSamplesImported.txt", string.Join(Environment.NewLine, samples.Select(sample => sample.displayName)));
        EditorApplication.Exit(0);
    }
}
"@

        Invoke-UnityCheck $unityEditor $consumerRoot $logPath @("-executeMethod", "TalkSystemConsumerInstallCheck.VerifyPackageAndExit")
        Invoke-UnityCheck $unityEditor $consumerRoot $logPath @("-executeMethod", "TalkSystemConsumerInstallCheck.ImportSamplesAndExit")
        Invoke-UnityCheck $unityEditor $consumerRoot $logPath @()
    }

    Write-Host "Consumer install validation passed with package source '$PackageSource' (ActiveInputHandling=$ActiveInputHandling, InstallInputSystem=$InstallInputSystem)."
}
finally {
    if ($KeepProject) {
        Write-Host "Consumer project kept at $consumerRoot"
    }
    elseif (Test-Path $consumerRoot) {
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $resolvedConsumerRoot = [System.IO.Path]::GetFullPath($consumerRoot)
        if (!$resolvedConsumerRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to delete unexpected path: $resolvedConsumerRoot"
        }

        Remove-Item -LiteralPath $consumerRoot -Recurse -Force
    }

    if ($packageStageRoot -and (Test-Path $packageStageRoot)) {
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $resolvedPackageStageRoot = [System.IO.Path]::GetFullPath($packageStageRoot)
        if (!$resolvedPackageStageRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to delete unexpected path: $resolvedPackageStageRoot"
        }

        Remove-Item -LiteralPath $packageStageRoot -Recurse -Force
    }
}
