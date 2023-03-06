Param (
    [Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",
    
	# Docker
	[Parameter(Mandatory=$False)]
	[string]$DockerRegistry="",
	[Parameter(Mandatory=$False)]
	[string]$DockerUser,
	[Parameter(Mandatory=$False)]
	[string]$DockerPsw,

	# Technical
	[Parameter(Mandatory=$False)]
	[boolean]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

if ($SensenetDockerImage -Match "/") {
	Write-Output "pull $SensenetDockerImage image from the registry"
	Invoke-Cli -command "docker pull $SensenetDockerImage" -DryRun $DryRun
}

#############################
##    Variables section     #
#############################
$SN_NETWORKNAME=$NetworkName
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

Write-Output " "
##############################
#       docker network       #
##############################
Write-Output "[$($date) INFO] Create $($SN_NETWORKNAME)'"
$getNetwork=(docker network list -f name=$($SN_NETWORKNAME) --format "{{.Name}}" )
if ($getNetwork) {
    Write-Output "Docker network $getNetwork already exists..."
} else {
	Invoke-Cli -command "docker network create -d bridge $SN_NETWORKNAME" -DryRun $DryRun -ErrorAction stop
}

Write-Output " "
#############################
#       auth registry       #
#############################
if ($DockerUser) {
	Write-Output "authenticating to docker registry..."
	Invoke-Cli -command "docker login $DockerRegistry --username=$DockerUser --password=$DockerPsw" -DryRun $DryRun -ErrorAction stop
}



