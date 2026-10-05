param(
    [Parameter(Mandatory = $false)]
    [string]$Version = '1.3.0',
    [switch]$PreflightOnly
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$GitArgs
    )

    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git command failed: git $($GitArgs -join ' ')"
    }
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must use MAJOR.MINOR.PATCH, for example 1.0.0: $Version"
}

$tag = 'v' + $Version

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'git.exe was not found.'
}

$status = (& git status --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'Could not read git status.' }
if (-not [string]::IsNullOrWhiteSpace(($status -join "`n"))) {
    throw 'Working tree is not clean. Commit or stash local changes before publishing a release.'
}

$branch = (& git branch --show-current).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not determine the current branch.' }
if ($branch -ne 'main') {
    throw "Release tags must be created from main. Current branch: $branch"
}

Write-Host 'Fetching the latest protected main and existing tags...'
Invoke-Git -GitArgs @('fetch', 'origin', 'main', '--tags')

$head = (& git rev-parse HEAD).Trim()
$remoteMain = (& git rev-parse origin/main).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not resolve origin/main.' }
if ($head -ne $remoteMain) {
    throw "Local main is not exactly origin/main. Local: $head Remote: $remoteMain. Run git pull origin main and retry."
}

$assemblyInfoPath = Join-Path $root 'src\AssemblyInfo.cs'
$assemblyInfo = Get-Content -LiteralPath $assemblyInfoPath -Raw
if ($assemblyInfo -notmatch 'AssemblyFileVersion\("(\d+)\.(\d+)\.(\d+)\.\d+"\)') {
    throw 'AssemblyFileVersion was not found.'
}
$appVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
if ($appVersion -ne $Version) {
    throw "Requested release $Version does not match AssemblyFileVersion $appVersion."
}

$installerScript = Get-Content -LiteralPath (Join-Path $root 'installer\Tessdeck.iss') -Raw
$installerVersionPattern = '#define AppVersion\s+"' + [regex]::Escape($Version) + '"'
if ($installerScript -notmatch $installerVersionPattern) {
    throw "installer/Tessdeck.iss does not declare AppVersion $Version."
}

& git rev-parse -q --verify "refs/tags/$tag" *> $null
if ($LASTEXITCODE -eq 0) {
    throw "Tag already exists locally: $tag"
}

$existingRemote = & git ls-remote --tags origin "refs/tags/$tag"
if ($LASTEXITCODE -ne 0) { throw 'Could not check remote tags.' }
if (-not [string]::IsNullOrWhiteSpace(($existingRemote -join "`n"))) {
    throw "Tag already exists on origin: $tag"
}

$workflowPath = Join-Path $root '.github\workflows\release.yml'
$workflow = Get-Content -LiteralPath $workflowPath -Raw
if ($workflow -notmatch 'name:\s*Publish Release' -or $workflow -notmatch "tags:\s*\r?\n\s*- 'v\*'") {
    throw 'The release workflow does not appear to be configured for protected v* release tags.'
}

Write-Host ''
Write-Host "Release preflight passed for $tag"
Write-Host "Commit: $head"
Write-Host 'Mode: unsigned unless a valid code-signing identity is configured in GitHub Actions.'
Write-Host 'The GitHub Release will disclose the unsigned state and publish a SHA-256 checksum.'
Write-Host ''

if ($PreflightOnly) {
    Write-Host 'Preflight-only mode: no tag was created or pushed.'
    exit 0
}

Invoke-Git -GitArgs @('tag', '-a', $tag, '-m', "Tessdeck $Version")
try {
    Invoke-Git -GitArgs @('push', 'origin', $tag)
}
catch {
    Write-Warning "The local tag $tag was created, but push failed. The tag was not deleted automatically."
    throw
}

Write-Host ''
Write-Host "Published tag: $tag"
Write-Host 'GitHub Actions will now build, verify, and publish Tessdeck-Setup.exe plus its SHA-256 checksum.'
