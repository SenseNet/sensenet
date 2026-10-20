Param (
    [Parameter(Mandatory=$False)]
	[string]$NetworkName="sensenet",
    
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
	Invoke-Cli -command "docker network rm $($NetworkName)" -DryRun $DryRun -ErrorAction stop
    if ($Uninstall) {
        return
    }
}

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"

Write-Output " "
Write-Output "###############################"
Write-Output "#       docker network        #"
Write-Output "###############################"
Write-Output "[$($date) INFO] Create $($NetworkName)"
$getNetwork = $null
if (-not $DryRun) {
	$networkResult = docker network inspect --format '{{.Name}}' $NetworkName 2>$null
	if ($LASTEXITCODE -eq 0 -and $networkResult -contains $NetworkName) { $getNetwork = $NetworkName }
}
if ($getNetwork) {
    Write-Output "Docker network $getNetwork already exists..."
} else {
	Invoke-Cli -command "docker network create -d bridge $($NetworkName)" -DryRun $DryRun -ErrorAction stop
}

Write-Output " "
#############################
#       auth registry       #
#############################
if ($DockerUser) {
	Write-Output "authenticating to docker registry..."
	Invoke-Cli -command "docker login $DockerRegistry --username=$DockerUser --password=$DockerPsw" -DryRun $DryRun -ErrorAction stop
}



