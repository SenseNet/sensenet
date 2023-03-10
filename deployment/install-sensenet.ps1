Param (
	[Parameter(Mandatory=$False, DontShow=$True)]
	[string]$ProjectName,
	[Parameter(Mandatory=$False)]
	[switch]$InMemPlatform,

	# Install/Uninstall processes
	[Parameter(Mandatory=$False)]
	[switch]$CreateDevCert,
	[Parameter(Mandatory=$False)]
	[switch]$CreateImages,
	[Parameter(Mandatory=$False)]
	[switch]$CleanUp,
	[Parameter(Mandatory=$False, DontShow=$True)]
	[switch]$NoInstall,
	[Parameter(Mandatory=$False)]
	[switch]$OpenInBrowser,
	[Parameter(Mandatory=$False)]
	[switch]$Uninstall,
	[Parameter(Mandatory=$False)]
	[switch]$KeepRemoteDatabase,
	[Parameter(Mandatory=$False)]
	[switch]$KeepRabbitMq,
	
	# Modifiers
	[Parameter(Mandatory=$False)]
	[switch]$LocalSn,
	[Parameter(Mandatory=$False,
	ParameterSetName="volume")]
	[switch]$UseVolume,	

	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$HostName="$Env:COMPUTERNAME",
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[switch]$HostDb,
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$HostName",
	[Parameter(Mandatory=$False)]
    [string]$SqlUser,
	[Parameter(Mandatory=$False)]
    [string]$SqlPsw,

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[switch]$SearchService,

	# Rabbit-mq
	[Parameter(Mandatory=$False)]
	[string]$RabbitServiceHost,

	# Technical	
	[Parameter(Mandatory=$False)]
	[switch]$DryRun
)

# example 
# .\install-sensenet-ultimate.ps1 -CleanUp $true -UseVolume $true -SearchService $true  -VolumeBasePath /var/lib/docker/volumes

# actual settings: Get-ExecutionPolicy -List
# disable: Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass 
# remove: Set-ExecutionPolicy -Scope Process -ExecutionPolicy Undefined 

# Docker images we use are created for linux and Docker for Windows with Linux containers have issues 
# with lock files so if you want to use volumes you have to mount them from linux system
# for example volume path on Docker Desktop for Windows with WSL2 can be: /var/lib/docker/volumes
# You can manage mounted volume from File Explorer, but the actual path may be varied by Docker Desktop version, e.g.:
# \\wsl$\docker-desktop-data\data\docker\volumes\
# \\wsl$\docker-desktop-data\version-pack-data\community\docker\volumes

# Sql Server Configuration Manager / SQL Server Network Configuration / Protocols -> TCP/IP=Enabled
# try {
	if (-not (Get-Command "Wait-For-It" -ErrorAction SilentlyContinue)) {
		Write-Output "load helper functions"
		. "$($PSScriptRoot)/scripts/helper-functions.ps1"
	}

#############################
##    Variables section     #
#############################
$AppEnvironment="Development"
if ($InMemPlatform) {
	$SnType = "InMem"
} else {
	$SnType = "InSql"
}

# different sensenet types with different names for combined demo
if (-not $ProjectName) {
	$ProjectName = "sensenet-$($SnType.ToLower())"
	if ($SnType -eq "InSql") {
		if ($HostDb) { $ProjectName += "-hdb" } else { $ProjectName += "-cdb" }
		if ($UseVolume) { $ProjectName += "-wv" }
		if ($SearchService) { $ProjectName += "-ws" }
	}
}

# different sensenet types on different ports for combined demo
$basePort = 51000
$portModifier = 0
if ($SnType -eq "InSql") {
	$portModifier = 5 + 10 * [bool]$HostDb + 20 * [bool]$UseVolume + 40 * [bool]$SearchService
}
$SnHostPort=$basePort + 11 + $portModifier
$IsHostPort=$basePort + 12 + $portModifier
$SearchHostPort=$basePort + 13 + $portModifier
#dbport?

# Docker images needed for the actual setup
[string[]]$imageTypes = "$($SnType)", "Is" 
if ($SearchService) { $imageTypes+="Search" }

# InMem repo neither use database nor search service 
if ($SnType -eq "InMem") {  
	$SearchService = $False
	$UseDbContainer = $False	
} else {
	$UseDbContainer = -not $HostDb
}

# Wait time in seconds before try to connect to sensenet repository
if ($SnType -eq "InMem") {
	$WaitForSnInSeconds = 30
} else {
	$WaitForSnInSeconds = 60
}

# Workaround for relative path on host machine
if ($VolumeBasePath.StartsWith("./") -or 
	$VolumeBasePath.StartsWith("../")) {
	$VolumeBasePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($VolumeBasePath)
}

# Developer certificate demo passwords
$CertPsw="SuP3rS3CuR3P4sSw0Rd"

# RebbitMq container demo settings
$rabbitContainerName="sensenet-rabbit"
$rabbitUser="admin"
$rabbitPsw="SuP3rS3CuR3P4sSw0Rd"
$rabbitPort=$basePort + 5

if (-not $SearchService) {
	$createRabbitContainer = $False
} elseif ($RabbitServiceHost) {
	$createRabbitContainer = $False
} else {
	$createRabbitContainer = $True
	$RabbitServiceHost="amqp://$($rabbitUser):$($rabbitPsw)@$($rabbitContainerName)/"
}

if ($SnType -eq "InSql") {
	if ($HostDb) {
		if (-not $SqlUser -or -not $SqlPsw) {
			$SqlUser = Read-Host -Prompt 'Input your mssql user name'
			$SqlPsw = Read-Host -Prompt 'Input the mssql user password'		
		}		
	} else {
		# db in container, use sa for demo purposes
		if (-not $SqlPsw) {
			$SqlUser = "sa"
			$SqlPsw = "SuP3rS3CuR3P4sSw0Rd"
		}
	} 
}

#====================== clean up ======================

if ($CleanUp -or $Uninstall) {
	./scripts/cleanup-sensenet.ps1 `
		-ProjectName $ProjectName `
		-SnType $SnType `
		-UseDbContainer $UseDbContainer `
		-SearchService $SearchService `
		-UseVolume $UseVolume `
		-VolumeBasePath $VolumeBasePath `
		-DryRun $DryRun `
		-ErrorAction stop
	
	if ($SnType -eq "InSql" -and -not $UseDbContainer -and -not $KeepRemoteDatabase) {
		./scripts/install-sql-server.ps1 `
			-ProjectName $ProjectName `
			-HostName $Hostname `
			-VolumeBasePath $VolumeBasePath `
			-UseDbContainer $UseDbContainer `
			-DataSource $DataSource `
			-SqlUser $SqlUSer `
			-SqlPsw $SqlPsw `
			-Uninstall $True `
			-UseVolume $UseVolume `
			-DryRun $DryRun `
			-ErrorAction stop
	}

	if ($Uninstall) {
		# rabbitmq shared across installments so remove only at uninstall 
		if ($createRabbitContainer -and -not $KeepRabbitMq) {
			./scripts/install-rabbit.ps1 `
				-RabbitContainername $rabbitContainerName `
				-Uninstall $True `
				-DryRun $DryRun `
				-ErrorAction stop
		}

		exit;
	}
}

#====================== prerequisites ======================

if ($CreateDevCert) {
	./scripts/create-devcert.ps1 `
		-VolumeBasePath $VolumeBasePath `
		-CertPsw $CertPsw `
		-UseVolume $UseVolume `
		-DryRun $DryRun `
		-ErrorAction stop
}

if ($CreateImages) {
	foreach ($imageType in $imageTypes) {
		./scripts/create-images.ps1 `
			-ImageType $imageType `
			-SearchService $SearchService `
			-LocalSn $LocalSn `
			-DryRun $DryRun `
			-ErrorAction stop
	}
}

#====================== installer ======================

if ($NoInstall) {
	return
}

./scripts/install-sensenet-init.ps1 `
	-DryRun $DryRun `
	-ErrorAction stop

if ($SearchService -and $createRabbitContainer) {
	./scripts/install-rabbit.ps1 `
		-RabbitContainername $rabbitContainerName `
		-RabbitPort $rabbitPort `
		-RabbitUser $rabbitUSer `
		-RabbitPsw $rabbitPsw `
		-DryRun $DryRun `
		-ErrorAction stop
}

if ($SnType -eq "InSql") {
	./scripts/install-sql-server.ps1 `
		-ProjectName $ProjectName `
		-HostName $Hostname `
		-VolumeBasePath $VolumeBasePath `
		-UseDbContainer $UseDbContainer `
		-DataSource $DataSource `
		-SqlUser $SqlUSer `
		-SqlPsw $SqlPsw `
		-UseVolume $UseVolume `
		-DryRun $DryRun `
		-ErrorAction stop
}

./scripts/install-identity-server.ps1 `
	-ProjectName $ProjectName `
	-VolumeBasePath $VolumeBasePath `
	-Routing cnt `
	-AppEnvironment $AppEnvironment `
	-OpenPort $True `
	-SensenetPublicHost https://localhost:$SnHostPort `
	-IdentityPublicHost https://localhost:$IsHostPort `
	-IsHostPort $IsHostPort `
	-CertPass $CertPsw `
	-UseVolume $UseVolume `
	-DryRun $DryRun `
	-ErrorAction stop

if ($SearchService) {
	./scripts/install-search-service.ps1 `
		-ProjectName $ProjectName `
		-HostName $HostName `
		-VolumeBasePath $VolumeBasePath `
		-Routing cnt `
		-AppEnvironment $AppEnvironment `
		-OpenPort $True `
		-UseDbContainer $UseDbContainer `
		-DataSource $DataSource `
		-SqlUser $SqlUSer `
		-SqlPsw $SqlPsw `
		-SearchHostPort $SearchHostPort `
		-RabbitServiceHost $RabbitServiceHost `
		-CertPass $CertPsw `
		-UseVolume $UseVolume `
		-DryRun $DryRun `
		-ErrorAction stop
}

./scripts/install-sensenet-app.ps1 `
	-ProjectName $ProjectName `
	-HostName $Hostname `
	-VolumeBasePath $VolumeBasePath `
	-Routing cnt `
	-AppEnvironment $AppEnvironment `
	-OpenPort $True `
	-SnType $SnType `
	-SnHostPort $SnHostPort `
	-SensenetPublicHost https://localhost:$SnHostPort `
	-IdentityPublicHost https://localhost:$IsHostPort `
	-UseDbContainer $UseDbContainer `
	-DataSource $DataSource `
	-SqlUser $SqlUSer `
	-SqlPsw $SqlPsw `
	-SearchService $SearchService `
	-RabbitServiceHost $RabbitServiceHost `
	-CertPass $CertPsw `
	-UseVolume $UseVolume `
	-DryRun $DryRun `
	-ErrorAction stop

if (-not $DryRun -and ($OpenInBrowser -or $SearchService)) {
	# Wait-For-It -Seconds $WaitForSnInSeconds `
	# 	-Message "We are preparing your sensenet repository..." `
	# 	-DryRun $DryRun

	$RecheckDbInSeconds = 10
	$MaxTryNumber = 12
	Write-Verbose "check snapp availability..."
	DO {		
		$isSnAppAvailable = $False
		try {	 
			$snApiStatusCode = (Invoke-WebRequest -Uri https://localhost:$($SnHostPort)/odata.svc/Root -Verbose:$false).StatusCode
			if ($snApiStatusCode = 200) {
				Write-Verbose "sn api available!"
				$isSnAppAvailable = $True
			} else {
				Write-Verbose "sn api not yet available!"
				$isSnAppAvailable = $False
			}
		}
		catch {
			if ($MaxTryNumber-- -gt 0) {
				Write-Verbose "sn api not yet available!"
				$isSnAppAvailable = $False
				Start-Sleep -s $RecheckDbInSeconds
			} else {
				Write-Error "Wait for sql server timed out! ($($MaxTryNumber * $RecheckDbInSeconds)s)"
			}
		}
	} Until ($isSnAppAvailable)
}

if ($SearchService) {
	# Search service workaround to refresh lucene index after sensenet repository is initialized
	./scripts/install-search-service.ps1 `
		-ProjectName $ProjectName `
		-Restart $True `
		-DryRun $DryRun `
		-ErrorAction stop

	# Sensenet application workaround if preparation was too slow and app terminated
	./scripts/install-sensenet-app.ps1 `
		-ProjectName $ProjectName `
		-Restart $True `
		-DryRun $DryRun `
		-ErrorAction stop

	# if (-not $DryRun -and $OpenInBrowser) {
	# 	Wait-For-It -Seconds 5 `
	# 		-Message "Restart your sensenet repository..." `
	# 		-Silent $True `
	# 		-DryRun $DryRun
	# }
}

if (-not $DryRun -and $OpenInBrowser) {
	Start-Process "https://admin.test.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A$SnHostPort"
}

Write-Output "You're welcome!"

# } catch {
# 	Write-Error "$_"
# }
