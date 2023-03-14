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
	[string]$SnType="InSql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetDockerImage="sn-api-sql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
	[string]$SensenetAppdataVolume="$($VolumeBasePath)/$($SensenetContainerName)/appdata",
	[Parameter(Mandatory=$False)]
	[string]$SensenetPublicHost="https://$($ProjectName)-sn.$($Domain)",
	[Parameter(Mandatory=$False)]
	[int]$SnHostPort=8081,
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
	[Parameter(Mandatory=$False)]
    [string]$SqlUser="",
    [Parameter(Mandatory=$False)]
    [string]$SqlPsw="",

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[bool]$SearchService=$False,	
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",	
	[Parameter(Mandatory=$False)]
	[string]$SearchServiceHost="http://$($SearchContainerName)",

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitServiceHost,

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
	[bool]$Restart=$False,
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

if ($Restart) {
	# workaround if sensenet repository and search service preparation were too slow and snapp was terminated
	$cntStatus = $( docker container inspect -f "{{.State.Status}}" $SensenetContainerName )
	if ($cntStatus -ne "running") {
		Write-Output "Restart sensenet application..."
		Invoke-Cli -command "docker restart $($SensenetContainerName)" -DryRun $DryRun
		Wait-For-It -Seconds 5 -Silent $True -DryRun $DryRun
	}
	return
}

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

switch ($SnType) {
	"InMem" { 
		$SensenetDockerImage="sn-api-inmem"
	}
	"InSql" { 
		if ($SearchService) { 			
			$SensenetDockerImage="sn-api-nlb"
		}
		else {
			$SensenetDockerImage="sn-api-sql"
		}		
	}
	Default {
		Write-Error "Invalid sensenet type!"
		return
	}
}

if ($UseDbContainer) {
	$DataSource=$SqlContainerName;
}

Write-Output " "
Write-Output "################################"
Write-Output "#    sensenet app container    #"
Write-Output "################################"
Write-Output "[$($date) INFO] Install sensenet repository"

if ($SensenetDockerImage -Match "/") {
	Write-Output "pull $SensenetDockerImage image from the registry"
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
"-e", "`"ASPNETCORE_ENVIRONMENT=$AppEnvironment`"", "eol",
"-e", "sensenet__Container__Name=$($SensenetContainerName)", "eol",
"-e", "sensenet__identityManagement__UserProfilesEnabled=false", "eol",
"-e", "sensenet__authentication__authority=$($IdentityPublicHost)", "eol",
"-e", "sensenet__authentication__repositoryUrl=$($SensenetPublicHost)", "eol"

if ($SnType -eq "InSql") {
	$params += "-e", "ConnectionStrings__SnCrMsSql=Persist Security Info=False;Initial Catalog=$($SqlDbName);Data Source=$($DataSource);User ID=$($SqlUser);Password=$($SqlPsw);TrustServerCertificate=true", "eol"
}

if ($SnType -eq "InSql" -and $SearchService) {
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

if ($SnType -ne "InMem" -and -not $UseDbContainer) {
	if ($HostName) {
		$params += "--add-host", "$($HostName):host-gateway", "eol"
	} else {
		$dsPrep = $DataSource.Split("\")[0]
		$params += "--add-host", "$($dsPrep):$DataSourceIp", "eol"
	}
}

if ($CertPath -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Path=`"$CertPath`"", "eol"
}

if ($CertPass -ne "") {
	$params += "-e", "Kestrel__Certificates__Default__Password=`"$CertPass`"", "eol"
}

if ($UseVolume -and $CertFolder -ne "") {
	$params += "-v", "$($CertFolder):/root/.aspnet/https:ro", "eol"
}

if ($UseVolume -and $UserSecrets -ne "") {
	$params += "-v", "$($UserSecrets):/root/.microsoft/usersecrets:ro", "eol"
}

if ($UseVolume -and $SnType -eq "InSql") {
	$params += "-v", "$($SensenetAppdataVolume):/app/App_Data", "eol"
}

if ($OpenPort) {
	$params += "-p", "`"$($SnHostPort):$($SnAppPort)`"", "eol"
}

$params += "$SensenetDockerImage"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop 

if (-not $UseVolume) {
	if (-not (Test-Path "./temp/certificates/$($CertName)")) {
		Write-Error "Certificate file missing!"
	}
	
	# if containers started without volume mounts upload the certificate to the container
	Invoke-Cli -execFile $execFile -params "exec", "-it", $SensenetContainerName, "mkdir", "-p", "/root/.aspnet/https" -DryRun $DryRun -ErrorAction stop
	Invoke-Cli -execFile $execFile -params "cp", "./temp/certificates/$($CertName)", "$($SensenetContainerName):/root/.aspnet/https/$($CertName)" -DryRun $DryRun -ErrorAction stop
}

if (-not $DryRun) {
	if ($Debugging) {
		Write-Output " "
		Wait-For-It -Seconds 5 -Message "Prepare debugger apps for sensenet container" -DryRun $DryRun
	 	Invoke-Cli -command "docker exec -it $SensenetContainerName /bin/sh -c apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"  -DryRun $DryRun -ErrorAction stop
	}

	$CRIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SensenetContainerName)
	Write-Output "`n[$($date) INFO] Sensenet App Ip: $CRIP"
	if ($OpenPort) {
		Write-Output "[$($date) INFO] Sensenet App url: https://localhost:$SnHostPort"
	}
}

