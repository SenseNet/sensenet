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
    [string]$SqlUser,
    [Parameter(Mandatory=$False)]
    [string]$SqlPsw,

	# Search service parameters
    [Parameter(Mandatory=$False)]
	[string]$SearchDockerImage="sensenetcsp/sn-searchservice",
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",
    [Parameter(Mandatory=$False)]
	[string]$SearchAppdataVolume="$($VolumeBasePath)/$($SearchContainerName)/appdata",
	[Parameter(Mandatory=$False)]
	[string]$SearchPublicHost="https://$($ProjectName)-search.$($Domain)",
	[Parameter(Mandatory=$False)]
	[int]$SearchHostPort=8083,
    [Parameter(Mandatory=$False)]
	[int]$SearchAppPort=443,	
    
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
	Write-Output "Restart search service..."
	Invoke-Cli -command "docker restart $($SearchContainerName)" -DryRun $DryRun
	return
}

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

if ($UseDbContainer) {
	$DataSource=$SqlContainerName;
}

Write-Output " "
Write-Output "#################################"
Write-Output "#   searchservice container   #"
Write-Output "#################################"
Write-Output "[$($date) INFO] Start search service"

if ($SearchDockerImage -Match "/") {
	Write-Output "pull $SearchDockerImage image from the registry"
	Invoke-Cli -command "docker pull $SearchDockerImage" -DryRun $DryRun
}

$aspnetUrls = "http://+:80"
if ($SearchAppPort -eq 443) {
	$aspnetUrls = "https://+:443;http://+:80"
}

$execFile = "docker"
$params = "run", "-it", "-d", "eol",
"--net", $NetworkName, "eol",
"--name", $SearchContainerName, "eol",
"-e", "`"ASPNETCORE_URLS=$aspnetUrls`"", "eol",
"-e", "`"ASPNETCORE_ENVIRONMENT=$AppEnvironment`"", "eol",
"-e", "ConnectionStrings__SecurityStorage=Persist Security Info=False;Initial Catalog=$($SqlDbName);Data Source=$($DataSource);User ID=$($SqlUser);Password=$($SqlPsw);TrustServerCertificate=true", "eol",
"-e", "sensenet__security__rabbitmq__ServiceUrl=$($RabbitServiceHost)", "eol"

if ($CertPath -ne "") {
	$params += "-e", "`"Kestrel__Certificates__Default__Path=$CertPath`"", "eol"
}

if ($CertPass -ne "") {
	$params += "-e", "`"Kestrel__Certificates__Default__Password=$CertPass`"", "eol"
}

if ($UseVolume -and $CertFolder -ne "") {
	$params += "-v", "$($CertFolder):/root/.aspnet/https:ro", "eol"
}

if ($UseVolume -and $UserSecrets -ne "") {
	$params += "-v", "$($UserSecrets):/root/.microsoft/usersecrets:ro", "eol"
}

if ($UseVolume) {
	$params += "-v", "$($SearchAppdataVolume):/app/App_Data", "eol"
}

if ($OpenPort) {
	$params += "-p", "$($SearchHostPort):$($SearchAppPort)", "eol"
}

$params += "$SearchDockerImage"


Invoke-Cli -execFile $execFile -params $params -dryRun $DryRun -ErrorAction stop

if (-not $UseVolume) {
	if (-not (Test-Path "./temp/certificates/$($CertName)")) {
		Write-Error "Certificate file missing!"
	}
	
	# if containers started without volume mounts upload the certificate to the container
	Invoke-Cli -execFile $execFile -params "exec", "-it", $SearchContainerName, "mkdir", "-p", "/root/.aspnet/https" -DryRun $DryRun -ErrorAction stop
	Invoke-Cli -execFile $execFile -params "cp", "./temp/certificates/$($CertName)", "$($SearchContainerName):/root/.aspnet/https/$($CertName)" -DryRun $DryRun -ErrorAction stop
}

if (-not $DryRun) {	
	if ($Debugging) {
		Write-Output " "
		Wait-For-It -Seconds 5 -Message "Prepare search container for debug..." -DryRun $DryRun
	 	Invoke-Cli -command "docker exec -it $SearchContainerName /bin/sh -c apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig" -DryRun $DryRun -ErrorAction stop
	}

	$SCIP=(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SearchContainerName)
	Write-Output "`n[$($date) INFO] Search Service Ip: $SCIP"
}