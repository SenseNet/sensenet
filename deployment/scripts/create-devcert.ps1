Param (
	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

    # Common app settings
	[Parameter(Mandatory=$False)]
	[string]$CertName="snapp.pfx",	
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

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

if ($Uninstall) {
    Invoke-Cli -command "dotnet dev-certs https --clean" -DryRun $DryRun -ErrorAction Stop
    # TODO: delete file
    return
}

#############################
##    Variables section     #
#############################
$dummyContainerName = -join ((65..90) + (97..122) | Get-Random -Count 10 | % {[char]$_})
$tempCertFolder = "$PSScriptRoot/../temp/certificates"
$tempCertPath = "$($tempCertFolder)/$($CertName)"
if (-not $DryRun -and -not (Test-Path $tempCertFolder)) {
	New-Item -ItemType Directory -Path $tempCertFolder
}

Write-Output " "
Write-Output "###############################"
Write-Output "#      check certificate      #"
Write-Output "###############################"

if (Test-Path $tempCertPath) {
	Write-Output "Certificate already exists"
} else {	
	Invoke-Cli -execFile "dotnet" -params "dev-certs", "https", "-ep", $tempCertPath, "-p", "$CertPsw" -DryRun $DryRun -ErrorAction Stop
	Invoke-Cli -command "dotnet dev-certs https --trust" -DryRun $DryRun -ErrorAction Stop
}

if ($UseVolume) {
	# copy the certificate through dummy container volume mount to the exact place from where the app will use it
	Invoke-Cli -execFile "docker" -params "run", "-d", "--rm", "--name", $dummyContainerName, "-v", "$($VolumeBasePath)/certificates/:/root", "alpine", "tail", "-f", "/dev/null" -DryRun $DryRun -ErrorAction Stop
	$isCertExists = $null
	if (-not $DryRun) { $isCertExists = (docker exec $dummyContainerName sh -c "test -f /root/$($CertName) && echo 'FileExists'") }
	if ($isCertExists -eq "FileExists") {
		Write-Verbose "Certificate is at the right place!"
	} else {
		Invoke-Cli -execFile "docker" -params "cp", $tempCertPath, "$($dummyContainerName):/root/$($CertName)" -DryRun $DryRun -ErrorAction Stop
	}
	Invoke-Cli -execFile "docker" -params "stop", $dummyContainerName -DryRun $DryRun -ErrorAction Stop
}


