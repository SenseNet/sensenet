Param (
    [Parameter(Mandatory=$False)]
	[string]$NetworkName="snnetwork",
    [Parameter(Mandatory=$False)]
	[string]$RabbitContainerName="sn-rabbit",
	[Parameter(Mandatory=$False)]
	[int]$RabbitPort=8079,
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
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
$RABBIT_USER="admin"
$RABBIT_PSW="QWEasd123%"
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



Write-Output " "
Write-Output "############################"
Write-Output "#         rabbitmq         #"
Write-Output "############################"

$execFile = "docker"
$params = "run", "-d", "eol",
	"--net", "`"$NetworkName`"", "eol",
	"--hostname", "`"$($RabbitContainerName)`"", "eol",
	"--name", "`"$($RabbitContainerName)`"", "eol",
	"-e", "`"RABBITMQ_DEFAULT_USER=$RABBIT_USER`"", "eol",
	"-e", "`"RABBITMQ_DEFAULT_PASS=$RABBIT_PSW`"", "eol",
	"-p", "`"$($RabbitPort):15672`"", "eol",
	"$RABBIT_DOCKERIMAGE"

Invoke-Cli -execFile $execFile -params $params -DryRun $DryRun -ErrorAction stop
if (-not $DryRun) {	
	$RABBITIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $RabbitContainerName)
	Write-Output "`n[$($date) INFO] RABBITIP: $RABBITIP"
	Write-Output "[$($date) INFO] RabbitMq url: http://localhost:$RabbitPort"
}