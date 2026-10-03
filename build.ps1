# EbenTiler for Windows build script
# Uses the .NET Framework compiler already available on Windows.
#
#   powershell -ExecutionPolicy Bypass -File build.ps1
#
# Output: build\EbenTiler.exe

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root 'src'
$outDir = Join-Path $root 'build'
$exePath = Join-Path $outDir 'EbenTiler.exe'
$appConfigPath = Join-Path $root 'app.config'
$runtimeConfigPath = Join-Path $outDir 'EbenTiler.exe.config'
$iconPath = Join-Path $root 'assets\app.ico'
$iconScript = Join-Path $root 'tools\make-appicon.ps1'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw '.NET Framework compiler (csc.exe) was not found.'
}

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# Fail early with a useful message when the local build output is still running.
if (Test-Path $exePath) {
    $probe = $null
    try {
        $probe = [System.IO.File]::Open(
            $exePath,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw "build\EbenTiler.exe is currently in use. Exit EbenTiler from the tray, or run 'Get-Process EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force', then build again."
    }
    finally {
        if ($null -ne $probe) { $probe.Dispose() }
    }
}

if (-not (Test-Path $appConfigPath)) {
    throw "Runtime configuration was not found: $appConfigPath"
}

# Generate the EXE/installer icon from the same visual definition used by the website favicon.
if (-not (Test-Path $iconScript)) {
    throw "App icon generator was not found: $iconScript"
}
& powershell -NoProfile -ExecutionPolicy Bypass -File $iconScript -NoPreview
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $iconPath)) {
    throw 'App icon generation failed.'
}

$sources = Get-ChildItem -Path $srcDir -Filter *.cs | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) {
    throw 'No C# source files were found in src.'
}

$cscArgs = @(
    '/nologo'
    '/target:winexe'
    '/platform:x64'
    '/optimize+'
    '/warn:4'
    '/warnaserror-'
    "/out:$exePath"
    "/win32icon:$iconPath"
    '/reference:System.dll'
    '/reference:System.Core.dll'
    '/reference:System.Drawing.dll'
    '/reference:System.Windows.Forms.dll'
) + $sources

Write-Host 'Compiling EbenTiler...'
& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed (exit $LASTEXITCODE)"
}

# .NET Framework reads DPI behavior from EbenTiler.exe.config next to the executable.
Copy-Item -Path $appConfigPath -Destination $runtimeConfigPath -Force

$size = [Math]::Round((Get-Item $exePath).Length / 1KB, 1)
Write-Host ''
Write-Host "Build complete: $exePath ($size KB)"
Write-Host "Runtime config: $runtimeConfigPath"
