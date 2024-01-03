Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",

	# Docker
	[Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",

	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

	# Sensenet App
	[Parameter(Mandatory=$False)]
	[string]$SnType="InSql",
	[Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
	[string]$SensenetAppdataVolume="$($VolumeBasePath)/$($SensenetContainerName)/appdata",

	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-snis",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-snsql",
	[Parameter(Mandatory=$False)]
	[string]$SqlVolume="$($VolumeBasePath)/$($SensenetContainerName)/mssql",

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[bool]$SearchService=$False,
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-snsearch",
	[Parameter(Mandatory=$False)]
	[string]$SearchAppdataVolume="$($VolumeBasePath)/$($SearchContainerName)/appdata",

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
	
	# Technical
	[Parameter(Mandatory=$False)]
	[bool]$UseVolume=$False,
	[Parameter(Mandatory=$False)]
	[bool]$WithServices=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -DryRun $DryRun -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

Write-Output " "
Write-Output "#########################"
Write-Output "##    Docker Cleanup    #"
Write-Output "#########################"

# stop and remove previous containers
if ($UseDbContainer -and
	$SnType -eq "InSql") {
	Invoke-Cli -command "docker container stop $SqlContainerName" -message "[$($date) INFO] Stop container: $SqlContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
}
Invoke-Cli -command "docker container stop $IdentityContainerName" -message "[$($date) INFO] Stop container: $IdentityContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
Invoke-Cli -command "docker container stop $SensenetContainerName" -message "[$($date) INFO] Stop container: $SensenetContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
if ($SnType -eq "InSql" -and $SearchService) {
	Invoke-Cli -command "docker container stop $SearchContainerName" -message "[$($date) INFO] Stop container: $SearchContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
}
if ($WithServices) {
	Invoke-Cli -command "docker container stop $RabbitContainerName" -message "[$($date) INFO] Stop container: $RabbitContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
}

Write-Output "`n"

if ($UseDbContainer -and
	$SnType -eq "InSql") {
	Invoke-Cli -command "docker container rm $SqlContainerName" -message "[$($date) INFO] Remove container: $SqlContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue 
}
Invoke-Cli -command "docker container rm $IdentityContainerName" -message "[$($date) INFO] Remove container: $IdentityContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
Invoke-Cli -command "docker container rm $SensenetContainerName" -message "[$($date) INFO] Remove container: $SensenetContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
if ($SnType -eq "InSql" -and $SearchService) {
	Invoke-Cli -command "docker container rm $SearchContainerName" -message "[$($date) INFO] Remove container: $SearchContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
}
if ($WithServices) {
	Invoke-Cli -command "docker container rm $RabbitContainerName" -message "[$($date) INFO] Remove container: $RabbitContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
}

if ($UseVolume -and 
	$SensenetAppdataVolume -ne "" -and
	$SnType -eq "InSql") {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SensenetAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -message "[$($date) INFO] Cleanup volume: $SensenetAppdataVolume" -DryRun $DryRun -ErrorAction SilentlyContinue
}
if ($UseDbContainer -and
	$SqlVolume -ne "" -and
	$UseVolume -and
	$SnType -eq "InSql") {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "rm", "-rf", "/var/opt/mssql" -message "[$($date) INFO] Cleanup volume: $SqlVolume" -DryRun $DryRun -ErrorAction SilentlyContinue
}
if ($UseVolume -and 
	$SearchAppdataVolume -ne "" -and
	($SnType -eq "InSql" -and $SearchService)) {
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SearchAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -message "[$($date) INFO] Cleanup volume: $SearchAppdataVolume" -DryRun $DryRun -ErrorAction SilentlyContinue
}

if ($DockerRegistry) {
	Invoke-Cli -command "docker logout $DockerRegistry" -message "logout from docker registry..." -DryRun $DryRun -ErrorAction SilentlyContinue
}