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
    [string]$DataSource="$($HostName)",

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
	[Parameter(Mandatory=$False)]
	[bool]$Debugging=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

$SQL_SA_USER="dockertest"
$SQL_SA_PASSWORD="QWEasd123%"
$SQL_SN_DBNAME=$SqlDbName

switch ($SnType) {
	"InMem" { 
		$SensenetDockerImage="sn-api-inmem"
	}
	"InSql" { 
		$SensenetDockerImage="sn-api-sql"
	}
	"InSqlNlb" { 
		$SensenetDockerImage="sn-api-nlb"
	}
	Default {
		Write-Error "Invalid sensenet type!"
		# exit 1;
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
	Invoke-Cli -command "docker pull $SensenetDockerImage" -DryRun $DryRun
}

Test-Docker

# Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SensenetAppdataVolume):/app/App_Data", "alpine", "chmod", "777", "/app/App_Data" -DryRun $DryRun

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
		$IdentityPublicHostName=([System.Uri]$IdentityPublicHost).Host
		$params += "--add-host", "$($IdentityPublicHostName):host-gateway", "eol"
	}
}

if (-not $UseDbContainer -and -not $HostName) {
	$dsPrep = $DataSource.Split("\")[0]
	$params += "--add-host", "$($dsPrep):$DataSourceIp", "eol"
}

if (-not $UseDbContainer -and $HostName) {
	$params += "--add-host", "$($HostName):host-gateway", "eol"
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

if ($SnType -eq "InSql") {
	$params += "-v", "$($SensenetAppdataVolume):/app/App_Data", "eol"
}

if ($OpenPort) {
	$params += "-p", "`"$($SnHostPort):$($SnAppPort)`"", "eol"
}

$params += "$SensenetDockerImage"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop 
if (-not $DryRun) {
	if ($Debugging) {
		write-output " "
		Wait-For-It -Seconds 5 -Message "Prepare debugger apps for sensenet container" -DryRun $DryRun
	 	Invoke-Cli -command "docker exec -it $SensenetContainerName /bin/sh -c apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig" -DryRun $DryRun
	}

	$CRIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SensenetContainerName)
	write-output "`n[$($date) INFO] Sensenet App Ip: $CRIP"
	if ($OpenPort) {
		write-output "[$($date) INFO] Sensenet App url: https://localhost:$SnHostPort"
	}
}

