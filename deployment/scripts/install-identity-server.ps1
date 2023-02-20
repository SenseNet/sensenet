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
	[string]$IdentityContainerName="$($ProjectName)-is",
	[Parameter(Mandatory=$False)]
	[string]$IdentityPublicHost="https://$($ProjectName)-is.$($Domain)",	
	[Parameter(Mandatory=$False)]
	[int]$IsHostPort=8083,
    [Parameter(Mandatory=$False)]
	[int]$IsAppPort=443,

	# Certificate
	[Parameter(Mandatory=$False)]
	[string]$UserSecrets,
	[Parameter(Mandatory=$False)]
	[string]$CertFolder,
	[Parameter(Mandatory=$False)]
	[string]$CertPath,
	[Parameter(Mandatory=$False)]
	[string]$CertPass,
	
	# Technical
	[Parameter(Mandatory=$False)]
	[bool]$Debugging=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False

	# not in use?
	# [Parameter(Mandatory=$False)]
	# [string]$DockerUser,
	# [Parameter(Mandatory=$False)]
	# [string]$DockerPsw
)

# examples
# 1. with visual studio cert
# .\install_sensenetdocker-is.ps1 -ProjectName locald -Domain locahost -IdentityDockerImage sensenet-identityserver:feature-standalone-docker.2022.06.13 -AppEnvironment Development -SensenetPublicHost https://localhost:8082 -UserSecrets $env:HOME\AppData\Roaming\Microsoft\UserSecrets -CertFolder $env:HOME\AppData\Roaming\ASP.NET\Https
# 2. with manually created dev-cert
# .\install_sensenetdocker-is.ps1 -ProjectName locald -Domain locahost -IdentityDockerImage sensenet-identityserver:feature-standalone-docker.2022.06.13 -AppEnvironment Development -SensenetPublicHost https://localhost:8082 -CertFolder $($env:USERPROFILE)\.aspnet\https\ -CertPath /root/.aspnet/https/aspnetapp.pfx -CertPass QWEasd123%

# manual dev-cert creation:
# dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p QWEasd123%
# dotnet dev-certs https --trust

# manual dev-cert cleanup:
# dotnet dev-certs https --clean

# https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide

# kestrel cert path + pass is for dev cert
# usersecrets volume is for visual studio cert

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

# $SensenetPublicHost=$($SensenetPublicHost)
# $SensenetContainerHost=$($SensenetContainerHost)
# $IdentityPublicHost=$($IdentityPublicHost)

write-output " "
write-host "#################################"
write-host "#   identity server container   #"
write-host "#################################"
write-output "[$($date) INFO] Start identity server"

if ($IdentityDockerImage -Match "/") {
	write-host "pull $IdentityDockerImage image from the registry"
	docker pull $IdentityDockerImage
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
"-e", "`"AppEnvironment=$AppEnvironment`"", "eol",
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

write-host "$execFile $($params -replace "eol", "```n`t")"
if (-not $DryRun) {
	& $execFile $($params -replace "eol", "")

	if ($Debugging) {
		write-output " "
		Start-Sleep -s 5
		docker exec -it "$IdentityContainerName" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	write-output " "
	$ISIP=(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $IdentityContainerName)
	write-output "[$($date) INFO] ISIP: $ISIP"
} else {
	write-host "`nDryRun"
}