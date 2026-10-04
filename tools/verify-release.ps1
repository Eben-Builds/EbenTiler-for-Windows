param(
    [string]$Executable = '.\build\Tessdeck.exe',
    [string]$Installer = '.\dist\Tessdeck-Setup.exe',
    [string]$HashFile = '.\dist\Tessdeck-Setup.exe.sha256',
    [switch]$RequireCodeSigning
)

$ErrorActionPreference = 'Stop'

function Resolve-ExistingFile {
    param([string]$Path)

    $resolved = [IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Required release file not found: $resolved"
    }
    return $resolved
}

function Assert-CodeSigningSignature {
    param([string]$Path)

    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Authenticode signature is not valid for $Path (status: $($signature.Status))."
    }

    $certificate = $signature.SignerCertificate
    if ($null -eq $certificate) {
        throw "Signer certificate is missing for $Path."
    }

    $now = Get-Date
    if ($certificate.NotBefore -gt $now -or $certificate.NotAfter -le $now) {
        throw "Signer certificate is outside its validity period for $Path."
    }

    $hasCodeSigningEku = $false
    foreach ($extension in $certificate.Extensions) {
        if ($extension.Oid.Value -ne '2.5.29.37') { continue }

        $ekuExtension = $extension -as [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]
        if ($null -eq $ekuExtension) { continue }

        foreach ($oid in $ekuExtension.EnhancedKeyUsages) {
            if ($oid.Value -eq '1.3.6.1.5.5.7.3.3') {
                $hasCodeSigningEku = $true
                break
            }
        }
    }

    if (-not $hasCodeSigningEku) {
        throw "Signer certificate does not include the Code Signing EKU for $Path."
    }

    Write-Host "Signature OK: $([IO.Path]::GetFileName($Path))"
    Write-Host "  Subject: $($certificate.Subject)"
    Write-Host "  Thumbprint: $($certificate.Thumbprint)"
}

$exePath = Resolve-ExistingFile $Executable
$installerPath = Resolve-ExistingFile $Installer
$hashPath = Resolve-ExistingFile $HashFile

$expectedHash = ((Get-Content -LiteralPath $hashPath -Raw).Split()[0]).Trim().ToLowerInvariant()
if ($expectedHash -notmatch '^[0-9a-f]{64}$') {
    throw "SHA-256 manifest is malformed: $hashPath"
}

$actualHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($expectedHash -ne $actualHash) {
    throw "Installer SHA-256 mismatch. Expected $expectedHash but got $actualHash."
}
Write-Host "SHA-256 OK: $actualHash"

if ($RequireCodeSigning) {
    Assert-CodeSigningSignature $exePath
    Assert-CodeSigningSignature $installerPath
} else {
    Write-Host 'Code-signing verification was not required for this run.'
}

Write-Host 'Release verification passed.'
