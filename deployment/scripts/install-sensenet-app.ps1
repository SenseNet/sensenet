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
	[string]$SensenetDockerImage="sn-api-sql-ss",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
    # [string]$SensenetAppdataVolume="/var/lib/docker/volumes/$($SensenetContainerName)/appdata",
	[string]$SensenetAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/appdata"),
	[Parameter(Mandatory=$False)]
	[string]$SensenetPublicHost="https://$($ProjectName)-sn.$($Domain)",
	[Parameter(Mandatory=$False)]
	[int]$SnHostPort=8082,
	[Parameter(Mandatory=$False)]
	[int]$SnAppPort=443,

	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-is",
	[Parameter(Mandatory=$False)]
	[string]$IdentityPublicHost="https://$($ProjectName)-is.$($Domain)",	
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerHost="http://$($IdentityContainerName)",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-sql",
    [Parameter(Mandatory=$False)]
	[string]$SqlDbName="$($ProjectName)-sndb",
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$($HostName)\MSSQL2016",

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-Search",	
	[Parameter(Mandatory=$False)]
	[string]$SearchServiceHost="http://$($SearchContainerName)",

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitServiceHost="amqp://admin:QWEasd123%@sn-rabbit/",

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
	[bool]$UseGrpc=$False,
	[Parameter(Mandatory=$False)]
	[bool]$Debugging=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

# db requred, e.g.
# .\install_sensenetdocker-sql.ps1 -ProjectName locald 

# examples
# 1. with visual studio cert
# .\install_sensenetdocker-sn.ps1 -ProjectName locald -Domain locahost -SensenetDockerImage sn-api-sql:feature-standalone-docker.2022.06.10 -AppEnvironment Development -SensenetPublicHost https://localhost:8082 -IdentityPublicHost https://localhost:8083 -UserSecrets $env:HOME\AppData\Roaming\Microsoft\UserSecrets -CertFolder=$env:HOME\AppData\Roaming\ASP.NET\Https
# 2. with manually created dev-cert
# .\install_sensenetdocker-sn.ps1 -ProjectName locald -Domain locahost -SensenetDockerImage sn-api-sql:feature-standalone-docker.2022.06.10 -AppEnvironment Development -SensenetPublicHost https://localhost:8082 -IdentityPublicHost https://localhost:8083 -CertFolder $env:HOME\.aspnet\https\ -CertPath /root/.aspnet/https/aspnetapp.pfx -CertPass QWEasd123% 

#############################
##    Variables section     #
#############################
$Sensenet_PURL="$($SensenetPublicHost)"
$Identity_PURL="$($IdentityPublicHost)"
$Identity_IURL="$($IdentityContainerHost)"
$Sensenet_HOST_PORT=$SnHostPort
$Sensenet_APP_PORT=$SnAppPort
$SN_NETWORKNAME=$NetworkName
$SQL_SERVER=$DataSource
$SQL_SA_USER="dockertest"
$SQL_SA_PASSWORD="QWEasd123%"
$SQL_SN_DBNAME=$SqlDbName
$Sensenet_CONTAINERNAME=$SensenetContainerName
$Sensenet_APPDATA_VOLUME=$SensenetAppdataVolume
$Sensenet_DOCKERIMAGE=$SensenetDockerImage
$ASPNETCORE_ENVIRONMENT=$AppEnvironment
$Search_URL=$SearchServiceHost
$RBBT_URL=$RabbitserviceHost
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

if ($UseDbContainer -eq $True) {
	$SQL_SA_USER="sa"
	$SQL_SERVER=$SqlContainerName;
}

write-output " "
write-host "################################"
write-host "#    sensenet app container    #"
write-host "################################"
write-output "[$($date) INFO] Install sensenet repository"

if ($Sensenet_DOCKERIMAGE -Match "/") {
	write-host "pull $Sensenet_DOCKERIMAGE image from the registry"
	docker pull $Sensenet_DOCKERIMAGE
}

$aspnetUrls = "http://+:80"
if ($Sensenet_APP_PORT -eq 443) {
	$aspnetUrls = "https://+:443;http://+:80"
}

$execFile = "docker"
$params = "run", "-it", "-d", "eol",
"--net", "`"$SN_NETWORKNAME`"", "eol",
"--name", "`"$($Sensenet_CONTAINERNAME)`"", "eol",
"-e", "`"ASPNETCORE_URLS=$aspnetUrls`"", "eol",
"-e", "`"ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT`"", "eol",
"-e", "ConnectionStrings__SnCrMsSql=Persist Security Info=False;Initial Catalog=$($SQL_SN_DBNAME);Data Source=$($SQL_SERVER);User ID=$($SQL_SA_USER);Password=$($SQL_SA_PASSWORD);TrustServerCertificate=true", "eol",
"-e", "sensenet__Container__Name=$($Sensenet_CONTAINERNAME)", "eol",
"-e", "sensenet__identityManagement__UserProfilesEnabled=false", "eol",
"-e", "sensenet__authentication__authority=$($Identity_PURL)", "eol",
"-e", "sensenet__authentication__repositoryUrl=$($Sensenet_PURL)", "eol"

if ($UseGrpc -ne "") {
	$params += "-e", "sensenet__search__service__ServiceAddress=$($Search_URL)", "eol",
	"-e", "sensenet__security__rabbitmq__ServiceUrl=$($RBBT_URL)", "eol",
	"-e", "sensenet__rabbitmq__ServiceUrl=$($RBBT_URL)", "eol"
}

switch($Routing) {
	"cnt" {
		$params += "-e", "sensenet__authentication__metadatahost=$($Identity_IURL)", "eol"
	}
	"hst" {
		$Identity_PURL_HOST=([System.Uri]$Identity_PURL).Host
		$params += "--add-host", "$($Identity_PURL_HOST):host-gateway", "eol"
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

$params += "-v", "$($Sensenet_APPDATA_VOLUME):/app/App_Data", "eol"


if ($OpenPort) {
	$params += "-p", "`"$($Sensenet_HOST_PORT):$($Sensenet_APP_PORT)`"", "eol"
}

$params += "$Sensenet_DOCKERIMAGE"

write-host "$execFile $($params -replace "eol", "```n`t")"
if (-not $DryRun) {
	& $execFile $($params -replace "eol", "")

	if ($Debugging) {
		write-output " "
		Start-Sleep -s 5
		docker exec -it "$Sensenet_CONTAINERNAME" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	write-output " "
	$CRIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($SN_NETWORKNAME).IPAddress }}" $Sensenet_CONTAINERNAME)
	write-output "[$($date) INFO] CRIP: $CRIP"
} else {
	write-host "`nDryRun"
}

