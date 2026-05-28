# ============================================================================
#  test_shader.ps1
#  ----------------------------------------------------------------------------
#  Primary harness entrypoint on Windows. The Codex agent calls THIS, not
#  Unity directly.
#
#  Usage:
#      .\tools\test_shader.ps1 -TestName CelShading_Frontlit
#      .\tools\test_shader.ps1 -TestName CelShading_Frontlit -CompileOnly
#      .\tools\test_shader.ps1 -TestName CelShading_Frontlit -UpdateReference
#
#  Exit codes:
#      0 - PASS
#      1 - FAIL (image mismatch)
#      2 - ERROR (Unity crash, missing file, etc.)
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$TestName,

    [string]$UnityExe = $env:UNITY_EXE,
    [string]$ProjectPath = (Get-Location).Path,
    [double]$Threshold = 0.02,
    [switch]$CompileOnly,
    [switch]$UpdateReference
)

$ErrorActionPreference = "Stop"

function Resolve-UnityExecutable {
    $explicitCandidates = @(
        "D:\Program\Unity\Unity_Editor\2022.3.62f3\Editor\Unity.exe",
        "D:\Program\Unity\Unity_Editor\2022.3.40f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\2022.3.40f1\Editor\Unity.exe"
    )

    foreach ($candidate in $explicitCandidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    $editorRoots = @(
        "D:\Program\Unity\Unity_Editor",
        "C:\Program Files\Unity\Hub\Editor"
    )

    foreach ($root in $editorRoots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        $editors = Get-ChildItem -Path $root -Directory -Filter "2022.3.*" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending

        foreach ($editor in $editors) {
            $candidate = Join-Path $editor.FullName "Editor\Unity.exe"
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    return $null
}

# ---- Resolve paths ---------------------------------------------------------
$PackageRoot  = Split-Path -Parent $PSScriptRoot
$OutputDir    = Join-Path $ProjectPath "HarnessOutput"
$ReferenceDir = Join-Path $PackageRoot "Samples\Harness\References"
$CurrentPng   = Join-Path $OutputDir "$TestName.png"
$ReferencePng = Join-Path $ReferenceDir "$TestName.png"
$DiffPng      = Join-Path $OutputDir "$TestName.diff.png"
$LogFile      = Join-Path $OutputDir "$TestName.log"

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityExe = Resolve-UnityExecutable
}

if (-not (Test-Path $OutputDir))    { New-Item -ItemType Directory -Path $OutputDir    | Out-Null }
if (-not (Test-Path $ReferenceDir)) { New-Item -ItemType Directory -Path $ReferenceDir | Out-Null }

if ([string]::IsNullOrWhiteSpace($UnityExe) -or -not (Test-Path -LiteralPath $UnityExe)) {
    Write-Host "ERROR: Unity executable not found: $UnityExe" -ForegroundColor Red
    Write-Host "       Pass -UnityExe '<path>' or set UNITY_EXE to your Unity Editor path." -ForegroundColor Yellow
    exit 2
}

# ---- Pick the entry method -------------------------------------------------
$Method = if ($CompileOnly) {
    "DToon.Editor.Harness.HarnessRunner.CompileCheck"
} else {
    "DToon.Editor.Harness.HarnessRunner.RenderTest"
}

Write-Host "==> [$TestName] Method: $Method"
Write-Host "==> [$TestName] Project: $ProjectPath"

# ---- Invoke Unity in batch mode -------------------------------------------
$unityArgs = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-executeMethod", $Method,
    "-testName", $TestName,
    "-outputPath", $CurrentPng,
    "-logFile", $LogFile,
    "-quit"
)

$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow
$unityExit = $proc.ExitCode

if ($unityExit -ne 0) {
    Write-Host "==> Unity exited with code $unityExit" -ForegroundColor Red
    Write-Host "    Tail of log ($LogFile):" -ForegroundColor Yellow
    if (Test-Path $LogFile) {
        Get-Content $LogFile -Tail 30 | ForEach-Object { Write-Host "    $_" }
    }
    exit 2
}

if ($CompileOnly) {
    Write-Host "==> [$TestName] CompileCheck PASS" -ForegroundColor Green
    exit 0
}

# ---- Optionally update reference -------------------------------------------
if ($UpdateReference) {
    Copy-Item -Path $CurrentPng -Destination $ReferencePng -Force
    Write-Host "==> [$TestName] Reference updated -> $ReferencePng" -ForegroundColor Cyan
    exit 0
}

# ---- First-time reference (no existing reference) --------------------------
if (-not (Test-Path $ReferencePng)) {
    Write-Host "==> [$TestName] No reference image yet at:" -ForegroundColor Yellow
    Write-Host "    $ReferencePng"
    Write-Host "    Inspect $CurrentPng. If it's correct, re-run with -UpdateReference."
    exit 1
}

# ---- Compare ---------------------------------------------------------------
$compareArgs = @(
    (Join-Path $PackageRoot "tools\compare.py"),
    $CurrentPng,
    $ReferencePng,
    "--threshold", $Threshold,
    "--diff-out", $DiffPng
)
& python @compareArgs
$cmpExit = $LASTEXITCODE

if ($cmpExit -eq 0) {
    Write-Host "==> [$TestName] PASS" -ForegroundColor Green
} elseif ($cmpExit -eq 1) {
    Write-Host "==> [$TestName] FAIL (see $DiffPng)" -ForegroundColor Red
} else {
    Write-Host "==> [$TestName] compare.py ERROR" -ForegroundColor Red
}

# Auto-sync to GitHub so Claude sees results without manual push
Push-Location $PSScriptRoot\..\..
$commitMessage = "harness $TestName $(Get-Date -Format 'MM-dd HH:mm')"
cmd /c "git add -A 1>NUL 2>NUL"
cmd /c "git commit -m `"$commitMessage`" 1>NUL 2>NUL"
cmd /c "git push 1>NUL 2>NUL"
Pop-Location

exit $cmpExit
