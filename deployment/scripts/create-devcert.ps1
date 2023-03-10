Param (
	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

    # Common app settings
    [Parameter(Mandatory=$False)]
	[string]$CertPsw,

    # Technical
	[Parameter(Mandatory=$False)]
	[bool]$UseVolume=$False,
    [Parameter(Mandatory=$False)]
	[bool]$Uninstall=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

# https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide

if (-not (Get-Command "Invoke-Cli" -DryRun $DryRun -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

if ($Uninstall) {
    Invoke-Cli -command "dotnet dev-certs https --clean"
    # TODO: delete file
    return
}

$dummyContainerName = -join ((65..90) + (97..122) | Get-Random -Count 10 | % {[char]$_})

if (Test-Path "./certificates/aspnetapp.pfx") {
	Write-Verbose "Certificate already exists"
} else {	
	Invoke-Cli -execFile "dotnet" -params "dev-certs", "https", "-ep", "./temp/certificates/aspnetapp.pfx", "-p", "$CertPsw"
	Invoke-Cli -command "dotnet dev-certs https --trust"
}

if ($UseVolume) {
	# copy the certificate through dummy container volume mount to the exact place where the app will be use it
	Invoke-Cli -execFile "docker" -params "run", "-d", "--rm", "--name", $dummyContainerName, "-v", "$($VolumeBasePath)/certificates/:/root", "alpine", "tail", "-f", "/dev/null"
	$isCertExists = (docker exec -it $dummyContainerName sh -c "test -f /root/aspnetapp.pfx && echo 'FileExists'") 
	if ($isCertExists -eq "FileExists") {
		Write-Verbose "Certificate is at the right place!"
	} else {
		Invoke-Cli -execFile "docker" -params "cp", "./temp/certificates/aspnetapp.pfx", "$($dummyContainerName):/root/aspnetapp.pfx"
	}
	Invoke-Cli -execFile "docker" -params "stop", $dummyContainerName
}


