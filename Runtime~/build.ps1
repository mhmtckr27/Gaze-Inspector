<#
.SYNOPSIS
    Builds the Gaze runtime assemblies and copies them into Runtime/Plugins/.

.DESCRIPTION
    Compiles GrayWolf.Gaze and GrayWolf.Gaze.Collections against the Unity version named in
    package.json (the oldest one Gaze supports), stamps them with the package version, and
    copies each .dll, .xml (IntelliSense docs) and .pdb (debug symbols) into Runtime/Plugins/.

    Run it after every change under Runtime~/, and before every release. Unity picks up the
    new DLLs on its next refresh.

.PARAMETER UnityEditorDir
    The Editor folder of that Unity install, e.g. "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor".
    Defaults to $env:GAZE_UNITY_EDITOR, then to Unity Hub's install folders.

.EXAMPLE
    ./build.ps1
.EXAMPLE
    ./build.ps1 -UnityEditorDir "D:\Unity\2022.3.62f3\Editor"
#>
param(
    [string]$UnityEditorDir = $env:GAZE_UNITY_EDITOR,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$sourceDir  = $PSScriptRoot
$packageDir = Split-Path $sourceDir -Parent
$pluginsDir = Join-Path $packageDir "Runtime/Plugins"
$package    = Get-Content (Join-Path $packageDir "package.json") -Raw | ConvertFrom-Json
$unityVersion = "$($package.unity).$($package.unityRelease)"

if (-not $UnityEditorDir) {
    $roots = @(Join-Path $env:ProgramFiles "Unity/Hub/Editor")
    $hubSecondary = Join-Path $env:APPDATA "UnityHub/secondaryInstallPath.json"
    if (Test-Path $hubSecondary) {
        $secondary = Get-Content $hubSecondary -Raw | ConvertFrom-Json
        if ($secondary) { $roots += $secondary }
    }
    foreach ($root in $roots) {
        $candidate = Join-Path $root "$unityVersion/Editor"
        if (Test-Path (Join-Path $candidate "Data/Managed/UnityEngine/UnityEngine.CoreModule.dll")) {
            $UnityEditorDir = $candidate
            break
        }
    }
    if (-not $UnityEditorDir) {
        throw "Unity $unityVersion not found in: $($roots -join ', '). Install it, or pass -UnityEditorDir (or set GAZE_UNITY_EDITOR)."
    }
}

Write-Host "Gaze $($package.version), building against Unity $unityVersion ($UnityEditorDir)"

# A machine-wide MSBuildSDKsPath left behind by an uninstalled .NET SDK makes every
# "dotnet build" fail with "SDK 'Microsoft.NET.Sdk' could not be found". Ignore it for this run.
if ($env:MSBuildSDKsPath -and -not (Test-Path $env:MSBuildSDKsPath)) {
    Write-Warning "Ignoring MSBuildSDKsPath=$env:MSBuildSDKsPath (folder does not exist)."
    Remove-Item Env:MSBuildSDKsPath
}

$assemblies = @("GrayWolf.Gaze", "GrayWolf.Gaze.Collections")
foreach ($name in $assemblies) {
    & dotnet build (Join-Path $sourceDir "$name.csproj") `
        --configuration $Configuration --nologo `
        "-p:UnityEditorDir=$UnityEditorDir" `
        "-p:Version=$($package.version)"
    if ($LASTEXITCODE -ne 0) { throw "Build of $name failed." }
}

New-Item -ItemType Directory -Force $pluginsDir | Out-Null
$outputDir = Join-Path $sourceDir "bin/$Configuration"
foreach ($name in $assemblies) {
    foreach ($ext in @("dll", "xml", "pdb")) {
        Copy-Item (Join-Path $outputDir "$name.$ext") $pluginsDir -Force
    }
}

Write-Host "Copied to $pluginsDir"
Get-ChildItem $pluginsDir -File | Where-Object { $_.Extension -ne ".meta" } |
    Format-Table Name, Length, LastWriteTime -AutoSize
