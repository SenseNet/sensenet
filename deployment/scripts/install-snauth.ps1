Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="sensenet",

	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$Domain="",
	[Parameter(Mandatory=$False)]
	[string]$HostIp="",
    [Parameter(Mandatory=$False)]
	[string]$HostName="",
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

	# Common app settings
	[Parameter(Mandatory=$False)]
	[string]$Routing,
	[Parameter(Mandatory=$False)]
	[string]$AppEnvironment="Production",
	[Parameter(Mandatory=$False)]
	[bool]$OpenPort=$False,

	# Sensenet App
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerHost="http://$($SensenetContainerName)",
	[Parameter(Mandatory=$False)]
	[string]$SensenetPublicHost="https://$($ProjectName)-sn.$($Domain)",
	
	# SnAuth server
	[Parameter(Mandatory=$False)]
	[string]$IdentityDockerImage="sensenetcsp/sn-auth:latest",
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-snis",
	[Parameter(Mandatory=$False)]
	[string]$IdentityPublicHost="https://$($ProjectName)-is.$($Domain)",	
	[Parameter(Mandatory=$False)]
	[int]$IsHostPort=8082,
    [Parameter(Mandatory=$False)]
	[int]$IsAppPort=443,
	[Parameter(Mandatory=$False)]
	[string]$ApiKey = "pr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Ted",
	[Parameter(Mandatory=$False)]
	[string]$SecretKey = "pr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Ted",
	[Parameter(Mandatory=$False)]
	[string]$RecaptchaSiteKey = "--to-be-set--",
	[Parameter(Mandatory=$False)]
	[string]$RecaptchaSecretKey = "--to-be-set--",

	# Certificate
	[Parameter(Mandatory=$False)]
	[string]$UserSecrets,
	[Parameter(Mandatory=$False)]
	[string]$CertFolder="$($VolumeBasePath)/certificates",
	[Parameter(Mandatory=$False)]
	[string]$CertName="snapp.pfx",
	[Parameter(Mandatory=$False)]
	[string]$CertPath="/root/.aspnet/https/$($CertName)",
	[Parameter(Mandatory=$False)]
	[string]$CertPass,
	
	# Technical
	[Parameter(Mandatory=$False)]
	[bool]$UseVolume=$False,
	[Parameter(Mandatory=$False)]
	[bool]$Debugging=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

Write-Output " "
Write-Output "#################################"
Write-Output "#   snauth server container   #"
Write-Output "#################################"
Write-Output "[$($date) INFO] Start snauth server"

if ($IdentityDockerImage -Match "/") {
	Write-Output "pull $IdentityDockerImage image from the registry"
	Invoke-Cli -command "docker pull $IdentityDockerImage" -DryRun $DryRun
}

$aspnetUrls = "http://+:80"
if ($IsAppPort -eq 443) {
	$aspnetUrls = "https://+:443;http://+:80"
}

$execFile = "docker"
$params = "run", "-it", "-d", "eol",
"--net", $NetworkName, "eol",
"--name", $IdentityContainerName, "eol",
"-e", "ASPNETCORE_URLS=$aspnetUrls", "eol",
"-e", "ASPNETCORE_ENVIRONMENT=$AppEnvironment", "eol",
"-e", "Sensenet__Repository__Url=$($SensenetPublicHost)", "eol",
"-e", "Sensenet__Repository__Authentication__ApiKey=$($ApiKey)", "eol",
"-e", "JwtSettings__Issuer=$($IdentityPublicHost)", "eol",
"-e", "JwtSettings__Audience=sensenet", "eol",
"-e", "JwtSettings__SecretKey=$($SecretKey)", "eol",
"-e", "JwtSettings__AuthTokenExpiryMinutes=300", "eol",
"-e", "JwtSettings__MultiFactorAuthExpiryMinutes=300", "eol",
"-e", "JwtSettings__TokenExpiryMinutes=300", "eol",
"-e", "JwtSettings__RefreshTokenExpiryDays=15", "eol",
"-e", "PasswordRecovery__TokenExpiryMinutes=60", "eol",
"-e", "Registration__IsEnabled=false", "eol",
"-e", "Recaptcha__SiteKey=$($RecaptchaSiteKey)", "eol",
"-e", "Recaptcha__SecretKey=$($RecaptchaSecretKey)", "eol",
"-e", "Application__Url=$($IdentityPublicHost)", "eol",
"-e", "Application__AllowedHosts__0=https://adminui.test.sensenet.com", "eol",
"-e", "Application__AllowedHosts__1=$($SensenetPublicHost)", "eol",
"-e", "Application__AllowedHosts__2=$($SensenetContainerHost)", "eol"

switch($Routing) {
	"cnt" {
		$params += "-e", "Sensenet__Repository__InnerUrl=$($SensenetContainerHost)", "eol"
	}
	"hst" {
		$SensenetPublicHost_HOST=([System.Uri]$SensenetPublicHost).Host
		$params += "--add-host", "$($SensenetPublicHost_HOST):host-gateway", "eol"
	}
}

if ($CertPath -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Path=$CertPath", "eol"
}

if ($CertPass -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Password=$CertPass", "eol"
}

if ($UseVolume -and $CertFolder -ne "") {
	$params += "-v", "$($CertFolder):/root/.aspnet/https:ro", "eol"
}

if ($UseVolume -and $UserSecrets -ne "") {
	$params += "-v", "$($UserSecrets):/root/.microsoft/usersecrets:ro", "eol"
}

if ($OpenPort) {
	$params += "-p", "$($IsHostPort):$($IsAppPort)", "eol"
}

$params += "$IdentityDockerImage"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
if (-not $UseVolume) {
	if (-not (Test-Path "./temp/certificates/$($CertName)")) {
		Write-Error "Certificate file missing!"
	}

	# if containers started without volume mounts upload the certificate to the container
	Invoke-Cli -execFile $execFile -params "exec", "-it", $IdentityContainerName, "mkdir", "-p", "/root/.aspnet/https" -DryRun $DryRun -ErrorAction stop
	Invoke-Cli -execFile $execFile -params "cp", "./temp/certificates/$($CertName)", "$($IdentityContainerName):/root/.aspnet/https/$($CertName)" -DryRun $DryRun -ErrorAction stop
}

if (-not $DryRun) {
	if ($Debugging) {
		Write-Output " "
		Start-Sleep -s 5
		docker exec -it "$IdentityContainerName" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	$ISIP=(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $IdentityContainerName)
	if ($ISIP -is [array] -and $ISIP[1] -is [string]) {
		# workaround for "failed to get console mode for stdout: The handle is invalid."
		$ISIP = $ISIP[1]
	}
	Write-Output "`n[$($date) INFO] SnAuth server Ip: $ISIP"
	if ($OpenPort) {
		Write-Output "[$($date) INFO] SnAuth Server url: https://localhost:$IsHostPort"
	}
}