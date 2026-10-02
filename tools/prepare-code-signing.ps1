param(
    [switch]$RequireSigning
)

$ErrorActionPreference = 'Stop'

function Set-GitHubEnvironmentValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    Set-Item -Path "Env:$Name" -Value $Value
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_ENV)) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    }
}

function Assert-CodeSigningCertificate {
    param([Parameter(Mandatory = $true)][string]$Thumbprint)

    $normalized = ($Thumbprint -replace '\s', '').ToUpperInvariant()
    $certificate = Get-Item -LiteralPath ("Cert:\CurrentUser\My\" + $normalized) -ErrorAction SilentlyContinue
    if ($null -eq $certificate) {
        throw "Code-signing certificate was not found in Cert:\CurrentUser\My: $normalized"
    }
    if (-not $certificate.HasPrivateKey) {
        throw "Code-signing certificate does not expose a usable private key: $normalized"
    }

    $hasCodeSigningEku = $false
    foreach ($extension in $certificate.Extensions) {
        if ($extension.Oid.Value -ne '2.5.29.37') { continue }
        $eku = $extension -as [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]
        if ($null -eq $eku) { continue }
        foreach ($oid in $eku.EnhancedKeyUsages) {
            if ($oid.Value -eq '1.3.6.1.5.5.7.3.3') {
                $hasCodeSigningEku = $true
                break
            }
        }
    }
    if (-not $hasCodeSigningEku) {
        throw "Certificate does not contain the Code Signing EKU: $normalized"
    }

    $now = Get-Date
    if ($certificate.NotBefore -gt $now -or $certificate.NotAfter -le $now) {
        throw "Code-signing certificate is outside its validity period: $normalized"
    }

    Set-GitHubEnvironmentValue -Name 'EBENTILER_SIGNING_CERT_SHA1' -Value $normalized
    Write-Host "Code-signing identity ready: $($certificate.Subject)"
    Write-Host "Thumbprint: $normalized"
    return $certificate
}

# Preferred contract: a hardware token, HSM, cloud signing service, or self-hosted
# runner bootstrap makes the certificate available through the Windows certificate
# store and supplies its thumbprint. build-installer.ps1 can then use SignTool
# without knowing which provider protects the private key.
if (-not [string]::IsNullOrWhiteSpace($env:EBENTILER_SIGNING_CERT_SHA1)) {
    Assert-CodeSigningCertificate -Thumbprint $env:EBENTILER_SIGNING_CERT_SHA1 | Out-Null
    exit 0
}

# Compatibility path for an existing exportable PFX. Newly issued publicly trusted
# code-signing certificates generally keep private keys in hardware/HSM/signing
# services, so do not design a new certificate purchase around PFX export.
if (-not [string]::IsNullOrWhiteSpace($env:SIGNING_PFX_BASE64)) {
    if ([string]::IsNullOrWhiteSpace($env:SIGNING_PFX_PASSWORD)) {
        throw 'SIGNING_PFX_PASSWORD is required when SIGNING_PFX_BASE64 is configured.'
    }

    Write-Warning 'Using legacy/exportable PFX compatibility mode. Prefer a hardware/HSM/cloud signing provider for newly issued public certificates.'

    $tempRoot = if (-not [string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
    $pfxPath = Join-Path $tempRoot 'ebentiler-signing.pfx'
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:SIGNING_PFX_BASE64))

    try {
        $password = ConvertTo-SecureString $env:SIGNING_PFX_PASSWORD -AsPlainText -Force
        $certificate = Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation 'Cert:\CurrentUser\My' -Password $password -Exportable:$false
        if ($null -eq $certificate -or [string]::IsNullOrWhiteSpace($certificate.Thumbprint)) {
            throw 'The code-signing PFX could not be imported.'
        }
        Assert-CodeSigningCertificate -Thumbprint $certificate.Thumbprint | Out-Null
    }
    finally {
        Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    }
    exit 0
}

if ($RequireSigning) {
    throw @'
Public release signing is not configured.

Preferred 2026 setup:
1. Configure a hardware/HSM/cloud signing provider on this runner.
2. Make its code-signing certificate visible in Cert:\CurrentUser\My.
3. Set EBENTILER_SIGNING_CERT_SHA1 to that certificate thumbprint.

Legacy compatibility is available with SIGNING_PFX_BASE64 and SIGNING_PFX_PASSWORD only for an existing exportable PFX.
See docs/CODE_SIGNING.md before purchasing or configuring a certificate.
'@
}

Write-Host 'Code signing is not configured for this build; producing an unsigned development installer.'
