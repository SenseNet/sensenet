Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",

    # Hosting environment
    [Parameter(Mandatory=$False)]
    [string]$HostName="$Env:COMPUTERNAME",
    [Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

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
    [string]$SqlVolume="$($VolumeBasePath)/$($SensenetContainerName)/mssql",
    [Parameter(Mandatory=$False)]
	[string]$SqlDbName="$($ProjectName)-sndb",
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$HostName",
    [Parameter(Mandatory=$False)]
    [string]$SqlUser,
    [Parameter(Mandatory=$False)]
    [string]$SqlPsw,
    [Parameter(Mandatory=$False)]
	[int]$SqlHostPort=9999,
    [Parameter(Mandatory=$False)]
	[int]$SqlAppPort=1433,

   	# Technical
    [Parameter(Mandatory=$False)]
    [bool]$Cleanup=$False,
    [Parameter(Mandatory=$False)]
	[bool]$Uninstall=$False,
    [Parameter(Mandatory=$False)]
	[bool]$UseVolume=$False,
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

if ($Cleanup -or $Uninstall) {
    if (-not $UseDbContainer) {
        Remove-Database -ServerName "$DataSource" -CatalogName "$SqlDbName" -UserName $SqlUser -UserPsw $SqlPsw -ErrorAction stop
    }
    if ($Uninstall) {
        return
    }
}

Write-Output "`r`n"
Write-Output "############################"
Write-Output "#       mssql server       #"
Write-Output "############################"

if ($UseDbContainer) {
    Test-Docker 

    if ($UseVolume) {
        # add permission to volume folder
        Write-Output "[$($date) INFO] Permit mssql server volume"
        Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "chmod", "777", "/var/opt/mssql" -DryRun $DryRun -ErrorAction stop
    }

    Write-Output "[$($date) INFO] Install mssql server"
    $execFile = "docker"
    $params = "run", "-d", "eol",
        "--net", "$NetworkName", "eol",
        "--name", "$SqlContainerName", "eol",
        "-e", "ACCEPT_EULA=Y", "eol",
        "-e", "MSSQL_SA_PASSWORD=$($SqlPsw)", "eol",
        "-e", "MSSQL_PID=Express", "eol"
    
    if ($UseVolume) {
        $params += "-v", "$($SqlVolume):/var/opt/mssql/data", "eol"
    }

    if ($OpenPort) {
        $params += "-p", "`"$($SqlHostPort):$($SqlAppPort)`"", "eol"
    }

    $params += $SqlDockerImage
    Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
        
    # wait for docker container to be started
    Wait-Container -ContainerName $SqlContainerName -DryRun $DryRun -ErrorAction stop

    # wait for sql server to be available
    Wait-CntDbServer -ContainerName $SqlContainerName -UserName $($SqlUser) -UserPsw $($SqlPsw) -DryRun $DryRun -ErrorAction stop

    # create empyt database
    Invoke-Cli -command "docker exec $SqlContainerName /opt/mssql-tools/bin/sqlcmd -U $($SqlUser) -P $($SqlPsw) -Q `"CREATE DATABASE [$($SqlDbName)]`"" -DryRun $DryRun -ErrorAction stop

    $msSqlIp = docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SqlContainerName
	Write-Output "`n[$($date) INFO] MsSql Server Ip: $msSqlIp"
    if ($OpenPort) {
		Write-Output "[$($date) INFO] MsSql Server: localhost,$SqlHostPort"
	}
} else {
    # Wait-RemoteDbServer -ServerName "$DataSource" -UserName $SqlUser -UserPsw $SqlPsw -ErrorAction stop
	# standalone mssql server
    New-Database -ServerName "$DataSource" -CatalogName "$SqlDbName" -UserName $SqlUser -UserPsw $SqlPsw -ErrorAction stop
}