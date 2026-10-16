Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="sensenet",

    # Hosting environment
    [Parameter(Mandatory=$False)]
    [string]$HostName="$Env:COMPUTERNAME",
    [Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

	# Common app settings
	[Parameter(Mandatory=$False)]
	[bool]$OpenPort=$True,

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
    if (-not $UseDbContainer -and -not $DryRun) {
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
        Invoke-Cli -execFile docker -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "chmod", "777", "/var/opt/mssql" -DryRun $DryRun -ErrorAction Stop
    }
    $params = "run", "-d", "--net", $NetworkName, "--name", $SqlContainerName,
        "-e", "ACCEPT_EULA=Y", "-e", "MSSQL_SA_PASSWORD=$SqlPsw", "-e", "MSSQL_PID=Express"
    # Preserve the original data-directory layout of existing PowerShell demos.
    if ($UseVolume) { $params += "-v", "$($SqlVolume):/var/opt/mssql/data" }
    if ($OpenPort) { $params += "-p", "127.0.0.1:$($SqlHostPort):$SqlAppPort" }
    $params += $SqlDockerImage
    $containerResult = Manage-Container -ContainerName $SqlContainerName -DockerRunParams $params -DryRun $DryRun
    Wait-Container -ContainerName $SqlContainerName -DryRun $DryRun -ErrorAction Stop
    $sqlcmd = "/opt/mssql-tools/bin/sqlcmd"
    if (-not $DryRun) {
        $sqlcmd = docker exec $SqlContainerName sh -c 'if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then echo /opt/mssql-tools18/bin/sqlcmd; else echo /opt/mssql-tools/bin/sqlcmd; fi'
        if ($LASTEXITCODE -ne 0) { throw "Cannot locate sqlcmd in $SqlContainerName." }
    }
    Wait-CntDbServer -ContainerName $SqlContainerName -SqlCmdPath $sqlcmd -UserName $SqlUser -UserPsw $SqlPsw -DryRun $DryRun -ErrorAction Stop
    Invoke-Cli -execFile docker -params "exec", $SqlContainerName, $sqlcmd, "-C", "-b", "-U", $SqlUser, "-P", $SqlPsw, "-Q", "IF DB_ID(N'$($SqlDbName.Replace("'", "''"))') IS NULL CREATE DATABASE [$($SqlDbName.Replace(']', ']]'))]" -DryRun $DryRun -ErrorAction Stop

    if (-not $DryRun) { $msSqlIp = docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $SqlContainerName }
	Write-Output "`n[$($date) INFO] MsSql Server Ip: $msSqlIp"
    if ($OpenPort) {
		Write-Output "[$($date) INFO] MsSql Server: localhost,$SqlHostPort"
	}
} else {
    # Wait-RemoteDbServer -ServerName "$DataSource" -UserName $SqlUser -UserPsw $SqlPsw -ErrorAction stop
	# standalone mssql server
    if (-not $DryRun) { New-Database -ServerName "$DataSource" -CatalogName "$SqlDbName" -UserName $SqlUser -UserPsw $SqlPsw -ErrorAction Stop }
    else { Write-Verbose "Would ensure host database $SqlDbName on $DataSource." }
}
