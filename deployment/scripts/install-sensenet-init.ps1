Param (
    [Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",
    
	# Docker
	[Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",
	[Parameter(Mandatory=$False)]
	[string]$DockerUser,
	[Parameter(Mandatory=$False)]
	[string]$DockerPsw
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

if ($SensenetDockerImage -Match "/") {
	write-host "pull $SensenetDockerImage image from the registry"
	Invoke-Cli -command "docker pull $SensenetDockerImage"
}

#############################
##    Variables section     #
#############################
$SN_NETWORKNAME=$NetworkName
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

write-output " "
##############################
#       docker network       #
##############################
write-output "[$($date) INFO] Create $($SN_NETWORKNAME)'"
$getNetwork=(docker network list -f name=$($SN_NETWORKNAME) --format "{{.Name}}" )
if ($getNetwork) {
    # docker network rm $SN_NETWORKNAME
    # docker network create -d bridge $SN_NETWORKNAME
    write-output "Docker network $getNetwork already exists..."
} else {
    docker network create -d bridge $SN_NETWORKNAME
}

write-output " "
#############################
#       auth registry       #
#############################
if ($DockerUser) {
	write-host "authenticating to docker registry..."
	docker login $DockerRegistry --username=$DockerUser --password=$DockerPsw
}



