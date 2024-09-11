Param (
	[Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",

	# RabbitMq
    [Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
	[Parameter(Mandatory=$False)]
	[int]$RabbitPort=8079,
	[Parameter(Mandatory=$False)]
	[string]$RabbitUser,
	[Parameter(Mandatory=$False)]
	[string]$RabbitPsw,
	
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
    Invoke-Cli -command "docker container stop $RabbitContainerName" -message "[$($date) INFO] Stop container: $RabbitContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
	Invoke-Cli -command "docker container rm $RabbitContainerName" -message "[$($date) INFO] Remove container: $RabbitContainerName" -DryRun $DryRun -ErrorAction SilentlyContinue
    if ($Uninstall) {
        return
    }
}

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"
$RABBIT_DOCKERIMAGE="rabbitmq:3-management"

$rbtStatus = $( docker container inspect -f "{{.State.Status}}" $RabbitContainerName )
switch ($rbtStatus) {
	"running" {
		Write-Output "RabbitMq already running! Remove it first if you want to start a new container..."
		return
	}
	"exited" {
		Write-Error "RabbitMq already started but exited! Fix the issue with the container and try again."
		return
	}
	"" {
		# not an issue, new container will be started
	}
	Default {
		Write-Output "Rabbit status: $rbtStatus" 
	}
}

if (-not $RabbitUser -or -not $RabbitPsw) {
	Write-Error "Username or password is missing!"
	return
}

Write-Output " "
Write-Output "############################"
Write-Output "#         rabbitmq         #"
Write-Output "############################"

$execFile = "docker"
$params = "run", "-d", "eol",
	"--net", "$NetworkName", "eol",
	"--hostname", "$($RabbitContainerName)", "eol",
	"--name", "$($RabbitContainerName)", "eol",
	"-e", "RABBITMQ_DEFAULT_USER=$RabbitUser", "eol",
	"-e", "RABBITMQ_DEFAULT_PASS=$RabbitPsw", "eol",
	"-p", "$($RabbitPort):15672", "eol",
	"$RABBIT_DOCKERIMAGE"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
if (-not $DryRun) {	
	$RABBITIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $RabbitContainerName)
	if ($RABBITIP -is [array] -and $RABBITIP[1] -is [string]) {
		# workaround for "failed to get console mode for stdout: The handle is invalid."
		$RABBITIP = $RABBITIP[1]
	}
	Write-Output "`n[$($date) INFO] RABBITIP: $RABBITIP"
	Write-Output "[$($date) INFO] RabbitMq url: http://localhost:$RabbitPort"
}