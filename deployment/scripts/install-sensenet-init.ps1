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
    [bool]$Cleanup=$False,
    [Parameter(Mandatory=$False)]
	[bool]$Uninstall=$False,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

if ($Cleanup -or $Uninstall) {
	Write-Output "Remove $($NetworkName) network'"
	Invoke-Cli -command "docker network rm $NetworkName" -DryRun $DryRun -ErrorAction stop
    if ($Uninstall) {
        return
    }
}

# if ($SensenetDockerImage -Match "/") {
# 	Write-Output "pull $SensenetDockerImage image from the registry"
# 	Invoke-Cli -command "docker pull $SensenetDockerImage" -DryRun $DryRun
# }

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

Write-Output " "
##############################
#       docker network       #
##############################
Write-Output "[$($date) INFO] Create $($NetworkName)'"
$getNetwork=(docker network list -f name=$($NetworkName) --format "{{.Name}}" )
if ($getNetwork) {
    Write-Output "Docker network $getNetwork already exists..."
} else {
	Invoke-Cli -command "docker network create -d bridge $NetworkName" -DryRun $DryRun -ErrorAction stop
}

Write-Output " "
#############################
#       auth registry       #
#############################
if ($DockerUser) {
	Write-Output "authenticating to docker registry..."
	Invoke-Cli -command "docker login $DockerRegistry --username=$DockerUser --password=$DockerPsw" -DryRun $DryRun -ErrorAction stop
}



