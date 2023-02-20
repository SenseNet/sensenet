Param (
    [Parameter(Mandatory=$False)]
	[string]$ProjectName="docker",
    
	[Parameter(Mandatory=$False)]
	[string]$SqlContainerName="$($ProjectName)-sql",
    [Parameter(Mandatory=$False)]
	[string]$IdentityContainerName="$($ProjectName)-is",
    [Parameter(Mandatory=$False)]
	[string]$SensenetContainerName="$($ProjectName)-snapp",
	[Parameter(Mandatory=$False)]
	[string]$SearchContainerName="$($ProjectName)-search",
    [Parameter(Mandatory=$False)]
	[string]$SnAuiContainerName="$($ProjectName)-snaui",
	
	[Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
    [Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",
	[Parameter(Mandatory=$False)]
    # [string]$SensenetAppdataVolume="/var/lib/docker/volumes/$($SensenetContainerName)/appdata",
	[string]$SensenetAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/appdata"),
	[Parameter(Mandatory=$False)]
    # [string]$SearchAppdataVolume="/var/lib/docker/volumes/$($SearchContainerName)/appdata",
	[string]$SearchAppdataVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SearchContainerName)/appdata"),
	
	[Parameter(Mandatory=$False)]
	# [string]$SqlVolume="/var/lib/docker/volumes/$($SensenetContainerName)/mssql",
	[string]$SqlVolume=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./volumes/$($SensenetContainerName)/mssql"),
	
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
write-output "[$($date) INFO] Stop container: $SnAuiContainerName"
docker container stop $SnAuiContainerName
write-output "[$($date) INFO] Stop container: $SensenetContainerName"
docker container stop $SensenetContainerName
write-output "[$($date) INFO] Stop container: $SearchContainerName"
docker container stop $SearchContainerName
write-output "[$($date) INFO] Stop container: $RABBIT_CONTAINERNAME"
docker container stop $RABBIT_CONTAINERNAME
write-output " "
write-output "[$($date) INFO] Remove old containers:"
write-output "[$($date) INFO] Remove container: $SqlContainerName"
docker container rm $SqlContainerName
write-output "[$($date) INFO] Remove container: $IdentityContainerName"
docker container rm $IdentityContainerName
write-output "[$($date) INFO] Remove container: $SnAuiContainerName"
docker container rm $SnAuiContainerName
write-output "[$($date) INFO] Remove container: $SensenetContainerName"
docker container rm $SensenetContainerName
write-output "[$($date) INFO] Remove container: $SearchContainerName"
docker container rm $SearchContainerName
write-output "[$($date) INFO] Remove container: $RABBIT_CONTAINERNAME"
docker container rm $RABBIT_CONTAINERNAME

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