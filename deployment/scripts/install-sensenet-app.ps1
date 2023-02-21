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
	[string]$SnType="InSql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetDockerImage="sn-api-sql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
    [string]$SensenetAppdataVolume="/var/lib/docker/volumes/$($SensenetContainerName)/appdata",
	# [string]$SensenetAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/appdata"),
	[Parameter(Mandatory=$False)]
	[string]$SensenetPublicHost="https://$($ProjectName)-sn.$($Domain)",
	[Parameter(Mandatory=$False)]
	[int]$SnHostPort=8082,
	[Parameter(Mandatory=$False)]
	[int]$SnAppPort=443,

	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-snis",
	[Parameter(Mandatory=$False)]
	[string]$IdentityPublicHost="https://$($ProjectName)-is.$($Domain)",	
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerHost="http://$($IdentityContainerName)",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-snsql",
    [Parameter(Mandatory=$False)]
	[string]$SqlDbName="$($ProjectName)-sndb",
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$($HostName)\MSSQL2016",

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",	
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
	# [Parameter(Mandatory=$False)]
	# [bool]$UseGrpc=$False,
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
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

$SQL_SA_USER="dockertest"
$SQL_SA_PASSWORD="QWEasd123%"
$SQL_SN_DBNAME=$SqlDbName

switch ($SnType) {
	"InSql" { 
		$SensenetDockerImage="sn-api-inmem"
	}
	"InSql" { 
		$SensenetDockerImage="sn-api-sql"
	}
	"InSqlNlb" { 
		$SensenetDockerImage="sn-api-nlb"
	}
	Default {
		Write-Output "Invalid sensenet type!"
		exit;
	}
}

if ($UseDbContainer -eq $True) {
	$SQL_SA_USER="sa"
	$DataSource=$SqlContainerName;
}

write-output " "
write-host "################################"
write-host "#    sensenet app container    #"
write-host "################################"
write-output "[$($date) INFO] Install sensenet repository"

if ($SensenetDockerImage -Match "/") {
	write-host "pull $SensenetDockerImage image from the registry"
	docker pull $SensenetDockerImage
}

$aspnetUrls = "http://+:80"
if ($SnAppPort -eq 443) {
	$aspnetUrls = "https://+:443;http://+:80"
}

$execFile = "docker"
$params = "run", "-it", "-d", "eol",
"--net", "`"$NetworkName`"", "eol",
"--name", "`"$($SensenetContainerName)`"", "eol",
"-e", "`"ASPNETCORE_URLS=$aspnetUrls`"", "eol",
"-e", "`"AppEnvironment=$AppEnvironment`"", "eol",
"-e", "sensenet__Container__Name=$($SensenetContainerName)", "eol",
"-e", "sensenet__identityManagement__UserProfilesEnabled=false", "eol",
"-e", "sensenet__authentication__authority=$($IdentityPublicHost)", "eol",
"-e", "sensenet__authentication__repositoryUrl=$($SensenetPublicHost)", "eol"

if ($SnType -eq "InSql" -or 
	$SnType -eq "InSqlNlb") {
		$params += "-e", "ConnectionStrings__SnCrMsSql=Persist Security Info=False;Initial Catalog=$($SQL_SN_DBNAME);Data Source=$($DataSource);User ID=$($SQL_SA_USER);Password=$($SQL_SA_PASSWORD);TrustServerCertificate=true", "eol"
}

if ($SnType -eq "InSqlNlb") {
	$params += "-e", "sensenet__search__service__ServiceAddress=$($SearchServiceHost)", "eol",
	"-e", "sensenet__security__rabbitmq__ServiceUrl=$($RabbitserviceHost)", "eol",
	"-e", "sensenet__rabbitmq__ServiceUrl=$($RabbitserviceHost)", "eol"
}

switch($Routing) {
	"cnt" {
		$params += "-e", "sensenet__authentication__metadatahost=$($IdentityContainerHost)", "eol"
	}
	"hst" {
		$IdentityPublicHost_HOST=([System.Uri]$IdentityPublicHost).Host
		$params += "--add-host", "$($IdentityPublicHost_HOST):host-gateway", "eol"
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

$params += "-v", "$($SensenetAppdataVolume):/app/App_Data", "eol"


if ($OpenPort) {
	$params += "-p", "`"$($SnHostPort):$($SnAppPort)`"", "eol"
}

$params += "$SensenetDockerImage"

write-host "$execFile $($params -replace "eol", "```n`t")"
if (-not $DryRun) {
	& $execFile $($params -replace "eol", "")

	if ($Debugging) {
		write-output " "
		Start-Sleep -s 5
		docker exec -it "$SensenetContainerName" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	write-output " "
	$CRIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SensenetContainerName)
	write-output "[$($date) INFO] CRIP: $CRIP"
} else {
	write-host "`nDryRun"
}

