Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",

	# Docker
	[Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",

	# Sensenet App
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
	[string]$SqlContainerName="$($ProjectName)-snsql",
	[Parameter(Mandatory=$False)]
	# [string]$SqlVolume="/var/lib/docker/volumes/$($SensenetContainerName)/mssql",
	[string]$SqlVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/mssql"),
	# [Parameter(Mandatory=$False)]
	# [bool]$UseDbContainer=$True,

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

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

write-output " "
write-host "##############################"
write-host "## Docker Status and Cleanup #"
write-host "##############################"

# check docker
# write-output "[$($date) INFO] docker version"
# docker version

# stop and remove previous containers
write-output "[$($date) INFO] Stop container: $SqlContainerName"
Invoke-Cli -command "docker container stop $SqlContainerName" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Stop container: $IdentityContainerName"
Invoke-Cli -command "docker container stop $IdentityContainerName" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Stop container: $SensenetContainerName"
Invoke-Cli -command "docker container stop $SensenetContainerName" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Stop container: $SearchContainerName"
Invoke-Cli -command "docker container stop $SearchContainerName" -ErrorAction SilentlyContinue
if ($WithServices) {
	write-output "[$($date) INFO] Stop container: $RabbitContainerName"
	Invoke-Cli -command "docker container stop $RabbitContainerName" -ErrorAction SilentlyContinue
}
write-output " "
write-output "[$($date) INFO] Remove old containers:"
write-output "[$($date) INFO] Remove container: $SqlContainerName"
Invoke-Cli -command "docker container rm $SqlContainerName" -ErrorAction SilentlyContinue 
write-output "[$($date) INFO] Remove container: $IdentityContainerName"
Invoke-Cli -command "docker container rm $IdentityContainerName" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Remove container: $SensenetContainerName"
Invoke-Cli -command "docker container rm $SensenetContainerName" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Remove container: $SearchContainerName"
Invoke-Cli -command "docker container rm $SearchContainerName" -ErrorAction SilentlyContinue
if ($WithServices) {
	write-output "[$($date) INFO] Remove container: $RabbitContainerName"
	Invoke-Cli -command "docker container rm $RabbitContainerName" -ErrorAction SilentlyContinue
}

write-output "[$($date) INFO] Cleanup volume: $SensenetAppdataVolume"
Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SensenetAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -ErrorAction SilentlyContinue
write-output "[$($date) INFO] Cleanup volume: $SqlVolume"
Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SqlVolume):/var/opt/mssql", "alpine", "rm", "-rf", "/var/opt/mssql" -ErrorAction SilentlyContinue
if ($UseGrpc) {
	write-output "[$($date) INFO] Cleanup volume: $SearchAppdataVolume"
	Invoke-Cli -execFile "docker" -params "run", "--rm", "-v", "$($SearchAppdataVolume):/app/App_Data", "alpine", "rm", "-rf", "/app/App_Data" -ErrorAction SilentlyContinue
}

if ($DockerRegistry) {
	write-host "logout from docker registry..."
	Invoke-Cli -command "docker logout $DockerRegistry" -ErrorAction SilentlyContinue
}