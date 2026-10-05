param(
    [Parameter(Mandatory = $true)]
    [string]$PrivateRemoteUrl,

    [string]$Destination = (Join-Path (Split-Path -Parent (Get-Location)) 'Tessdeck-Commercial')
)

$ErrorActionPreference = 'Stop'

$SourceRepo = 'https://github.com/Eben-Builds/Tessdeck-for-Windows.git'
$SourceCommit = 'c48f906300087b16d8a6a8d35f921fc0b9b614f1'
$PublicRepoName = 'Eben-Builds/Tessdeck-for-Windows'

function Invoke-Git {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$GitArgs
    )

    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git failed: git $($GitArgs -join ' ')"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'git was not found.'
}

if ([string]::IsNullOrWhiteSpace($PrivateRemoteUrl)) {
    throw 'PrivateRemoteUrl is required.'
}

if ($PrivateRemoteUrl -match [regex]::Escape($PublicRepoName)) {
    throw 'Refusing to use the public Tessdeck repository as the commercial remote.'
}

if (Test-Path $Destination) {
    throw "Destination already exists: $Destination"
}

Write-Host 'Checking private remote access...'
$remoteRefs = & git ls-remote $PrivateRemoteUrl 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Private repository is not accessible: $PrivateRemoteUrl"
}
if (-not [string]::IsNullOrWhiteSpace(($remoteRefs | Out-String).Trim())) {
    throw 'Private repository is not empty. Use a newly created empty private repository.'
}

Write-Host 'Cloning public Tessdeck history...'
Invoke-Git clone $SourceRepo $Destination

Push-Location $Destination
try {
    Write-Host "Pinning commercial baseline to $SourceCommit..."
    Invoke-Git checkout --detach $SourceCommit
    Invoke-Git switch -c commercial-main

    Invoke-Git remote rename origin community
    Invoke-Git remote add origin $PrivateRemoteUrl

    $license = @'
Tessdeck Commercial Source License

Copyright (c) 2026 Eben-Builds. All rights reserved.

This repository and all source code added after the Tessdeck v1.3.0
open-source baseline are private and proprietary unless a file explicitly
states otherwise.

No permission is granted to copy, modify, publish, distribute, sublicense,
sell, or disclose proprietary portions of this repository without prior
written permission from the copyright holder.

Historical source code that was previously released under the MIT License
remains governed by the MIT License for those historical versions.
'@
    Set-Content -LiteralPath '.\LICENSE' -Value $license -Encoding UTF8

    $baseline = @"
# Tessdeck Commercial Baseline

This private repository was split from the public Tessdeck codebase for commercial development.

- Public project: `Eben-Builds/Tessdeck-for-Windows`
- Commercial baseline commit: `$SourceCommit`
- Product baseline: Tessdeck v1.3.0
- Public historical license: MIT
- New commercial development: proprietary / all rights reserved
- Planned first commercial release: v1.4.0
- Trial policy: 3-Day Full Trial
- Purchase model: One-time purchase, no subscription
- Public repository remains the historical/open-source edition.

## Guardrails

1. Do not push commercial source back to the public repository.
2. Do not restore the public MIT LICENSE at the commercial branch tip.
3. Do not commit payment secrets, license signing private keys, API secrets, or certificates.
4. Keep entitlement verification fail-safe for already activated customers when offline.
5. Replace the temporary repository license notice with the final commercial EULA before public sale.
"@
    Set-Content -LiteralPath '.\docs\COMMERCIAL_BASELINE.md' -Value $baseline -Encoding UTF8

    $readme = Get-Content -LiteralPath '.\README.md' -Raw -Encoding UTF8
    $banner = @"
> [!IMPORTANT]
> **Private Commercial Source**
>
> This repository is the private commercial development line of Tessdeck.
> The public MIT edition ends at v1.3.0. Commercial development starts with v1.4.0.

"@
    Set-Content -LiteralPath '.\README.md' -Value ($banner + $readme) -Encoding UTF8

    Invoke-Git add LICENSE README.md docs/COMMERCIAL_BASELINE.md
    Invoke-Git commit -m 'Initialize private commercial baseline'
    Invoke-Git branch -M main

    Write-Host 'Pushing commercial main only...'
    Invoke-Git push -u origin main

    Write-Host ''
    Write-Host 'Commercial repository bootstrap complete.' -ForegroundColor Green
    Write-Host "Private remote : $PrivateRemoteUrl"
    Write-Host "Local path     : $Destination"
    Write-Host "Base commit    : $SourceCommit"
    Write-Host 'Public tags were not pushed.'
}
finally {
    Pop-Location
}
