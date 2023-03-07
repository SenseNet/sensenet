Param (
	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

    # Common app settings
    [Parameter(Mandatory=$False)]
	[string]$CertPsw,

    # Technical
    [Parameter(Mandatory=$False)]
	[boolean]$Uninstall=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

# https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide

if (-not (Get-Command "Invoke-Cli" -DryRun $DryRun -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

if ($Uninstall) {
    cleanup
    Invoke-Cli -command "dotnet dev-certs https --clean"
    # TODO: delete file
    return
}

Invoke-Cli -execFile "dotnet" -params "dev-certs", "https", "-ep", "$($VolumeBasePath)/certificates/aspnetapp.pfx", "-p", "$CertPsw"
Invoke-Cli -command "dotnet dev-certs https --trust"
