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

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
	
	# Technical    
	[Parameter(Mandatory=$False)]
	[bool]$WithServices=$False,
	[Parameter(Mandatory=$False)]
	[bool]$UseGrpc=$False
)

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
docker container stop $SqlContainerName
write-output "[$($date) INFO] Stop container: $IdentityContainerName"
docker container stop $IdentityContainerName
write-output "[$($date) INFO] Stop container: $SensenetContainerName"
docker container stop $SensenetContainerName
write-output "[$($date) INFO] Stop container: $SearchContainerName"
docker container stop $SearchContainerName
if ($WithServices) {
	write-output "[$($date) INFO] Stop container: $RabbitContainerName"
	docker container stop $RabbitContainerName
}
write-output " "
write-output "[$($date) INFO] Remove old containers:"
write-output "[$($date) INFO] Remove container: $SqlContainerName"
docker container rm $SqlContainerName
write-output "[$($date) INFO] Remove container: $IdentityContainerName"
docker container rm $IdentityContainerName
write-output "[$($date) INFO] Remove container: $SensenetContainerName"
docker container rm $SensenetContainerName
write-output "[$($date) INFO] Remove container: $SearchContainerName"
docker container rm $SearchContainerName
if ($WithServices) {
	write-output "[$($date) INFO] Remove container: $RabbitContainerName"
	docker container rm $RabbitContainerName
}

write-output "[$($date) INFO] Cleanup volume: $SensenetAppdataVolume"
docker run --rm -v "$($SensenetAppdataVolume):/app/App_Data" alpine rm -rf /app/App_Data
write-output "[$($date) INFO] Cleanup volume: $SqlVolume"
docker run --rm -v "$($SqlVolume):/var/opt/mssql" alpine rm -rf /var/opt/mssql
if ($UseGrpc) {
	write-output "[$($date) INFO] Cleanup volume: $SearchAppdataVolume"
	docker run --rm -v "$($SearchAppdataVolume):/app/App_Data" alpine rm -rf /app/App_Data
}

if ($DockerRegistry) {
	write-host "logout from docker registry..."
	docker logout $DockerRegistry 
}