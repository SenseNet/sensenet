Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",

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
	
	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityDockerImage="sensenet-identityserver",
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-snis",
	[Parameter(Mandatory=$False)]
	[string]$IdentityPublicHost="https://$($ProjectName)-is.$($Domain)",	
	[Parameter(Mandatory=$False)]
	[int]$IsHostPort=8082,
    [Parameter(Mandatory=$False)]
	[int]$IsAppPort=443,

	# Certificate
	[Parameter(Mandatory=$False)]
	[string]$UserSecrets,
	[Parameter(Mandatory=$False)]
	[string]$CertFolder="$($VolumeBasePath)/certificates",
	[Parameter(Mandatory=$False)]
	[string]$CertPath="/root/.aspnet/https/aspnetapp.pfx",
	[Parameter(Mandatory=$False)]
	[string]$CertPass,
	
	# Technical
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
Write-Output "#   identity server container   #"
Write-Output "#################################"
Write-Output "[$($date) INFO] Start identity server"

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
"--net", "`"$NetworkName`"", "eol",
"--name", "`"$($IdentityContainerName)`"", "eol",
"-e", "`"ASPNETCORE_URLS=$aspnetUrls`"", "eol",
"-e", "`"ASPNETCORE_ENVIRONMENT=$AppEnvironment`"", "eol",
"-e", "`"sensenet__LoginPage__DisplayOtherRepositoryButton=true`"", "eol",
"-e", "`"sensenet__authentication__setDefaultClients=true`"", "eol",
"-e", "`"IdentityServer__IssuerUri=$($IdentityPublicHost)`"", "eol",
"-e", "`"sensenet__Clients__adminui__RepositoryHosts__0__PublicHost=$($SensenetPublicHost)`"", "eol",
"-e", "`"sensenet__Clients__spa__RepositoryHosts__0__PublicHost=$($SensenetPublicHost)`"", "eol",
"-e", "`"sensenet__Clients__client__RepositoryHosts__0__PublicHost=$($SensenetPublicHost)`"", "eol"

switch($Routing) {
	"cnt" {
		$params += "-e", "`"sensenet__Clients__adminui__RepositoryHosts__0__InternalHost=$($SensenetContainerHost)`"", "eol"
		$params += "-e", "`"sensenet__Clients__spa__RepositoryHosts__0__InternalHost=$($SensenetContainerHost)`"", "eol"
		$params += "-e", "`"sensenet__Clients__client__RepositoryHosts__0__InternalHost=$($SensenetContainerHost)`"", "eol"
	}
	"hst" {
		$SensenetPublicHost_HOST=([System.Uri]$SensenetPublicHost).Host
		$params += "--add-host", "$($SensenetPublicHost_HOST):host-gateway", "eol"
	}
}

if ($CertPath -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Path=`"$CertPath`"", "eol"
}

if ($CertPass -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Password=`"$CertPass`"", "eol"
}

if ($CertFolder -ne "") {
	$params += "-v", "$($CertFolder):/root/.aspnet/https:ro", "eol"
}

if ($UserSecrets -ne "") {
	$params += "-v", "$($UserSecrets):/root/.microsoft/usersecrets:ro", "eol"
}

if ($OpenPort) {
	$params += "-p", "`"$($IsHostPort):$($IsAppPort)`"", "eol"
}

$params += "$IdentityDockerImage"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
if (-not $DryRun) {
	if ($Debugging) {
		Write-Output " "
		Start-Sleep -s 5
		docker exec -it "$IdentityContainerName" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	$ISIP=(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $IdentityContainerName)
	Write-Output "`n[$($date) INFO] Identity server Ip: $ISIP"
	if ($OpenPort) {
		Write-Output "[$($date) INFO] Identity Server url: https://localhost:$IsHostPort"
	}
}