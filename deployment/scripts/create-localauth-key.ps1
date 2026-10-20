#requires -Version 7.2
[CmdletBinding()]
Param (
    [string]$KeyPath="$PSScriptRoot/../temp/localauth/signing-key.pem",
    [bool]$DryRun=$False
)

if (Test-Path -LiteralPath $KeyPath) {
    if (-not (Test-Path -LiteralPath $KeyPath -PathType Leaf)) {
        throw "The LocalAuth key path is a directory: $KeyPath. Remove that empty directory and rerun this helper."
    }
    return
}
if ($DryRun) { Write-Verbose "Would generate the LocalAuth demo RSA key at $KeyPath."; return }
$null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $KeyPath)
$rsa = [System.Security.Cryptography.RSA]::Create(3072)
try { [IO.File]::WriteAllText($KeyPath, $rsa.ExportPkcs8PrivateKeyPem()) }
finally { $rsa.Dispose() }
Write-Host "Created the LocalAuth demo signing key at $KeyPath."
