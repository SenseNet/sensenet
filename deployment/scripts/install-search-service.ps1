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
	[string]$SearchDockerImage="sensenet-searchservice",
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",
    [Parameter(Mandatory=$False)]
    [string]$SearchAppdataVolume="/var/lib/docker/volumes/$($SearchContainerName)/appdata",
	# [string]$SearchAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SearchContainerName)/appdata"),
	[Parameter(Mandatory=$False)]
	[string]$SearchPublicHost="https://$($ProjectName)-search.$($Domain)",
	[Parameter(Mandatory=$False)]
	[int]$SearchHostPort=8083,
    [Parameter(Mandatory=$False)]
	[int]$SearchAppPort=443,	
    
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

# examples
# 1. with visual studio cert
## .\install_sensenetdocker-is.ps1 -ProjectName locald -Domain locahost -SearchDockerImage sensenet-identityserver:feature-standalone-docker.2022.06.13 -AppEnvironment Development -SnCrPublicHost https://localhost:8082 -UserSecrets $env:HOME\AppData\Roaming\Microsoft\UserSecrets -CertFolder $env:HOME\AppData\Roaming\ASP.NET\Https
# 2. with manually created dev-cert
## .\install_sensenetdocker-is.ps1 -ProjectName locald -Domain locahost -SearchDockerImage sensenet-identityserver:feature-standalone-docker.2022.06.13 -AppEnvironment Development -SnCrPublicHost https://localhost:8082 -CertFolder $($env:USERPROFILE)\.aspnet\https\ -CertPath /root/.aspnet/https/aspnetapp.pfx -CertPass QWEasd123%

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
$SQL_SA_USER="dockertest"
$SQL_SA_PASSWORD="QWEasd123%"
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

if ($UseDbContainer -eq $True) {
	$SQL_SA_USER="sa"
	$DataSource=$SqlContainerName;
}

write-output " "
write-host "#################################"
write-host "#   searchservice container   #"
write-host "#################################"
write-output "[$($date) INFO] Start search service"

if ($SearchDockerImage -Match "/") {
	write-host "pull $SearchDockerImage image from the registry"
	docker pull $SearchDockerImage
}

$aspnetUrls = "http://+:80"
if ($SearchAppPort -eq 443) {
	$aspnetUrls = "https://+:443;http://+:80"
}

$execFile = "docker"
$params = "run", "-it", "-d", "eol",
"--net", "`"$NetworkName`"", "eol",
"--name", "`"$($SearchContainerName)`"", "eol",
"-e", "`"ASPNETCORE_URLS=$aspnetUrls`"", "eol",
"-e", "`"AppEnvironment=$AppEnvironment`"", "eol",
"-e", "ConnectionStrings__SecurityStorage=Persist Security Info=False;Initial Catalog=$($SqlDbName);Data Source=$($DataSource);User ID=$($SQL_SA_USER);Password=$($SQL_SA_PASSWORD);TrustServerCertificate=true", "eol",
"-e", "sensenet__security__rabbitmq__ServiceUrl=$($RabbitServiceHost)", "eol"

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

$params += "-v", "$($SearchAppdataVolume):/app/App_Data", "eol"

if ($OpenPort) {
	$params += "-p", "`"$($SearchHostPort):$($SearchAppPort)`"", "eol"
}

$params += "$SearchDockerImage"

write-host "$execFile $($params -replace "eol", "```n`t")"
if (-not $DryRun) {
	& $execFile $($params -replace "eol", "")

	if ($Debugging) {
		write-output " "
		Start-Sleep -s 5
		docker exec -it "$SearchContainerName" /bin/sh -c "apt-get update && apt-get install -y net-tools iputils-ping mc telnet wget && ifconfig"
	}

	write-output " "
	$ISIP=(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SearchContainerName)
	write-output "[$($date) INFO] ISIP: $ISIP"
} else {
	write-host "`nDryRun"
}