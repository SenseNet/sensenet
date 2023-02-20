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

#############################
##    Variables section     #
#############################
$date = Get-Date -Format "yyyy-MM-dd HH:mm K"
$RABBIT_USER="admin"
$RABBIT_PSW="QWEasd123%"
$RABBIT_DOCKERIMAGE="rabbitmq:3-management"

write-output " "
write-host "############################"
write-host "#         rabbitmq         #"
write-host "############################"

$execFile = "docker"
$params = "run", "-d", "eol",
	"--net", "`"$NetworkName`"", "eol",
	"--hostname", "`"$($RabbitContainerName)`"", "eol",
	"--name", "`"$($RabbitContainerName)`"", "eol",
	"-e", "`"RABBITMQ_DEFAULT_USER=$RABBIT_USER`"", "eol",
	"-e", "`"RABBITMQ_DEFAULT_PASS=$RABBIT_PSW`"", "eol",
	"-p", "`"$($RabbitPort):15672`"", "eol",
	"$RABBIT_DOCKERIMAGE"

write-host "$execFile $($params -replace "eol", "```n`t")"
if (-not $DryRun) {
	& $execFile $($params -replace "eol", "")

	write-output " "
	$RABBITIP=$(docker inspect -f "{{ .NetworkSettings.Networks.$($NetworkName).IPAddress }}" $RabbitContainerName)
	write-output "[$($date) INFO] RABBITIP: $RABBITIP"
} else {
	write-host "`nDryRun"
}
