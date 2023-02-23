Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",

    # Hosting environment
    [Parameter(Mandatory=$False)]
    [string]$HostName="",

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
    [string]$DataSource="$($HostName)\MSSQL2016"
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
    Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "chmod", "777", "/var/opt/mssql"

    write-output "[$($date) INFO] Install mssql server"
    $execFile = "docker"
    $params = "run", "-d", "eol",
        "--net", "$NetworkName", "eol",
        "--name", "$SqlContainerName", "eol",
        "-e", "ACCEPT_EULA=Y", "eol",
        "-e", "SA_PASSWORD=$($SQL_SA_PASSWORD)", "eol",
        "-e", "MSSQL_PID=Express", "eol",
        "-v", "$($SqlVolume):/var/opt/mssql/data", "eol",
        $SqlDockerImage
    Invoke-Cli -execFile $execFile -params $params -ErrorAction stop

        # -v "$($SqlVolume):/var/opt/mssql", "eol"
        # -p "$($SQL_PORT):1433", "eol"

    Wait-For-It -Seconds 20 

    Invoke-Cli -command "docker exec $SqlContainerName /opt/mssql-tools/bin/sqlcmd -U sa -P $($SQL_SA_PASSWORD) -Q `"DROP DATABASE IF EXISTS [$($SqlDbName)];CREATE DATABASE [$($SqlDbName)]`"" -ErrorAction stop
    
    Wait-For-It -Seconds 5

    $msSqlIp = docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SqlContainerName
	write-output "[$($date) INFO] SQLIP: $msSqlIp"
} else {
	# standalone mssql server
    # ..\..\Ops\Create-EmptyDb.ps1 -ServerName "$SQL_SERVER" -CatalogName "$SqlDbName" -UserName $SQL_SA_USER -UserPsw $SQL_SA_PASSWORD
}