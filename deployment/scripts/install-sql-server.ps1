Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",

    # Hosting environment
    [Parameter(Mandatory=$False)]
    [string]$HostName="",

	# Common app settings
	[Parameter(Mandatory=$False)]
	[bool]$OpenPort=$False,

    # Sensenet App
    [Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,    
    [Parameter(Mandatory=$False)]
	[string]$SqlDockerImage="mcr.microsoft.com/mssql/server:2019-CU12-ubuntu-20.04",
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-snsql",
    [Parameter(Mandatory=$False)]
	# [string]$SqlVolume="/var/lib/docker/volumes/$($SensenetContainerName)/mssql",
    [string]$SqlVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/mssql"),
    [Parameter(Mandatory=$False)]
	[string]$SqlDbName="$($ProjectName)-sndb",
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$($HostName)\MSSQL2016",
    [Parameter(Mandatory=$False)]
	[int]$SqlHostPort=9999,
    [Parameter(Mandatory=$False)]
	[int]$SqlAppPort=1433,

   	# Technical
	[Parameter(Mandatory=$False)]
	[boolean]$DryRun=$False
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

# TODO: external database user parameters
# $SQL_SERVER=$DataSource
# $SQL_SA_USER="dockertest"

# TODO: conditional db port opening only on container + only if true
# $SQL_PORT=$SqlPort

$SQL_SA_PASSWORD="QWEasd123%"

write-output " "
write-host "############################"
write-host "#       mssql server       #"
write-host "############################"

if ($UseDbContainer) {
    write-output "[$($date) INFO] Permit mssql server volume"
    Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "chmod", "777", "/var/opt/mssql" -DryRun $DryRun -ErrorAction stop

    write-output "[$($date) INFO] Install mssql server"
    $execFile = "docker"
    $params = "run", "-d", "eol",
        "--net", "$NetworkName", "eol",
        "--name", "$SqlContainerName", "eol",
        "-e", "ACCEPT_EULA=Y", "eol",
        "-e", "SA_PASSWORD=$($SQL_SA_PASSWORD)", "eol",
        "-e", "MSSQL_PID=Express", "eol",
        "-v", "$($SqlVolume):/var/opt/mssql/data", "eol"

    if ($OpenPort) {
        $params += "-p", "`"$($SqlHostPort):$($SqlAppPort)`"", "eol"
    }

    $params += $SqlDockerImage
    Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
        
    Wait-For-It -Seconds 20 -Message "Waiting for MsSql server to be ready..." -DryRun $DryRun

    Invoke-Cli -command "docker exec $SqlContainerName /opt/mssql-tools/bin/sqlcmd -U sa -P $($SQL_SA_PASSWORD) -Q `"DROP DATABASE IF EXISTS [$($SqlDbName)];CREATE DATABASE [$($SqlDbName)]`"" -DryRun $DryRun -ErrorAction stop

    $msSqlIp = docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SqlContainerName
	write-output "`n[$($date) INFO] MsSql Server Ip: $msSqlIp"
    if ($OpenPort) {
		write-output "[$($date) INFO] MsSql Server: localhost,$SqlHostPort"
	}
} else {
	# standalone mssql server
    # ..\..\Ops\Create-EmptyDb.ps1 -ServerName "$SQL_SERVER" -CatalogName "$SqlDbName" -UserName $SQL_SA_USER -UserPsw $SQL_SA_PASSWORD
}