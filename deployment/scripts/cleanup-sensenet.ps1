Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",

	# Docker
	[Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",

	# Sensenet App
	[Parameter(Mandatory=$False)]
	[string]$SnType="InSql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
    [string]$SensenetAppdataVolume="/var/lib/docker/volumes/$($SensenetContainerName)/appdata",
	# [string]$SensenetAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/appdata"),

	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-snis",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-snsql",
	[Parameter(Mandatory=$False)]
	# [string]$SqlVolume="/var/lib/docker/volumes/$($SensenetContainerName)/mssql",
	[string]$SqlVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/mssql"),	

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",
	[Parameter(Mandatory=$False)]
    [string]$SearchAppdataVolume="/var/lib/docker/volumes/$($SearchContainerName)/appdata",
	# [string]$SearchAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SearchContainerName)/appdata"),

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
	
	# Technical    
	[Parameter(Mandatory=$False)]
	[bool]$WithServices=$False,
	[Parameter(Mandatory=$False)]
	[bool]$UseGrpc=$False
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

write-output " "
write-host "#########################"
write-host "##    Docker Cleanup    #"
write-host "#########################"

# stop and remove previous containers
if ($UseDbContainer -and
	($SnType -eq "InSql" -or 
	$SnType -eq "InSqlNlb")) {
	Invoke-Cli -command "docker container stop $SqlContainerName" -message "[$($date) INFO] Stop container: $SqlContainerName" -ErrorAction SilentlyContinue
}
Invoke-Cli -command "docker container stop $IdentityContainerName" -message "[$($date) INFO] Stop container: $IdentityContainerName" -ErrorAction SilentlyContinue
Invoke-Cli -command "docker container stop $SensenetContainerName" -message "[$($date) INFO] Stop container: $SensenetContainerName" -ErrorAction SilentlyContinue
if ($SnType -eq "InSqlNlb") {
	Invoke-Cli -command "docker container stop $SearchContainerName" -message "[$($date) INFO] Stop container: $SearchContainerName" -ErrorAction SilentlyContinue
}
if ($WithServices) {
	Invoke-Cli -command "docker container stop $RabbitContainerName" -message "[$($date) INFO] Stop container: $RabbitContainerName" -ErrorAction SilentlyContinue
}

write-output "`n"

if ($UseDbContainer -and
	($SnType -eq "InSql" -or 
	$SnType -eq "InSqlNlb")) {
	Invoke-Cli -command "docker container rm $SqlContainerName" -message "[$($date) INFO] Remove container: $SqlContainerName" -ErrorAction SilentlyContinue 
}
Invoke-Cli -command "docker container rm $IdentityContainerName" -message "[$($date) INFO] Remove container: $IdentityContainerName" -ErrorAction SilentlyContinue
Invoke-Cli -command "docker container rm $SensenetContainerName" -message "[$($date) INFO] Remove container: $SensenetContainerName" -ErrorAction SilentlyContinue
if ($SnType -eq "InSqlNlb") {
	Invoke-Cli -command "docker container rm $SearchContainerName" -message "[$($date) INFO] Remove container: $SearchContainerName" -ErrorAction SilentlyContinue
}
if ($WithServices) {
	Invoke-Cli -command "docker container rm $RabbitContainerName" -message "[$($date) INFO] Remove container: $RabbitContainerName" -ErrorAction SilentlyContinue
}

if ($SnType -eq "InSql") {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SensenetAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -message "[$($date) INFO] Cleanup volume: $SensenetAppdataVolume" -ErrorAction SilentlyContinue
}
if ($UseDbContainer -and
	($SnType -eq "InSql" -or 
	$SnType -eq "InSqlNlb")) {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "rm", "-rf", "/var/opt/mssql" -message "[$($date) INFO] Cleanup volume: $SqlVolume" -ErrorAction SilentlyContinue
}
if ($SnType -eq "InSqlNlb" -or $UseGrpc) {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SearchAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -message "[$($date) INFO] Cleanup volume: $SearchAppdataVolume" -ErrorAction SilentlyContinue
}

if ($DockerRegistry) {
	Invoke-Cli -command "docker logout $DockerRegistry" -message "logout from docker registry..." -ErrorAction SilentlyContinue
}