Param (
	[Parameter(Mandatory=$False)]
	[string]$ProjectName,
	[Parameter(Mandatory=$False)]
	[string]$SnType="InSql",

	# Install/Uninstall processes
	[Parameter(Mandatory=$False)]
	[bool]$CreateDevCert=$False,	
	[Parameter(Mandatory=$False)]
	[bool]$CreateImages=$False,
	[Parameter(Mandatory=$False)]
	[bool]$CleanUp=$False,
	[Parameter(Mandatory=$False)]
	[bool]$Install=$True,
	[Parameter(Mandatory=$False)]
	[bool]$OpenInBrowser=$True,
	[Parameter(Mandatory=$False)]
	[bool]$Uninstall=$False,
	
	# Modifiers
	[Parameter(Mandatory=$False)]
	[bool]$LocalSn=$False,
	[Parameter(Mandatory=$False)]
	[bool]$UseVolume=$False,	
	[Parameter(Mandatory=$False)]
	[bool]$KeepRemoteDatabase=$True,

	# Hosting environment
	[Parameter(Mandatory=$False)]
	[string]$HostName="$Env:COMPUTERNAME",
	[Parameter(Mandatory=$False)]
	[string]$VolumeBasePath="./volumes",

	# Sensenet Repository Database
	[Parameter(Mandatory=$False)]
	[bool]$UseDbContainer=$True,
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$HostName",
	[Parameter(Mandatory=$False)]
    [string]$SqlUser,
	[Parameter(Mandatory=$False)]
    [string]$SqlPsw,

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[bool]$SearchService=$False,

	# Technical	
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
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

	if ($SnType -ne "InMem" -and $SnType -ne "InSql") {
		Write-Error "-SnType is either 'InMem' or 'InSql'."
		return
	}

#############################
##    Variables section     #
#############################
$AppEnvironment="Development"

# different sensenet types with different names for combined demo
if (-not $ProjectName) {
	$ProjectName = "sensenet-$($SnType.ToLower())"
	if ($SnType -eq "InSql") {
		if ($UseDbContainer) { $ProjectName += "-cdb" } else { $ProjectName += "-hdb" }
		if ($UseVolume) { $ProjectName += "-wv" }
		if ($SearchService) { $ProjectName += "-ws" }
	}
}

# different sensenet types on different ports for combined demo
$basePort = 51000
$portModifier = 0
if ($SnType -eq "InSql") {
	$portModifier = 5 + 10 * $UseDbContainer + 20 * $UseVolume + 40 * $SearchService
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

if ($SnType -eq "InSql") {
	if (-not $UseDbContainer) {
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

#====================== prerequisites ======================

if ($CreateDevCert) {
	./scripts/create-devcert.ps1 `
		-VolumeBasePath $VolumeBasePath `
		-CertPsw $CertPsw `
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
			-DryRun $DryRun `
			-ErrorAction stop
	}

	if ($Uninstall) {
		exit;
	}
}

#====================== installer ======================

if ($Install) {
	./scripts/install-sensenet-init.ps1 `
		-DryRun $DryRun `
		-ErrorAction stop

	if ($SearchService) {
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
		-IsHostPort $IsHostPort `
		-CertPass $CertPsw `
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
			-RabbitServiceHost amqp://$($rabbitUser):$($rabbitPsw)@$($rabbitContainerName)/ `
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
		-RabbitServiceHost amqp://$($rabbitUser):$($rabbitPsw)@$($rabbitContainerName)/ `
		-CertPass $CertPsw `
		-UseVolume $UseVolume `
		-DryRun $DryRun `
		-ErrorAction stop
	
	if (-not $DryRun -and ($OpenInBrowser -or $SearchService)) {
		Wait-For-It -Seconds $WaitForSnInSeconds `
			-Message "We are preparing your sensenet repository..." `
			-DryRun $DryRun
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

		if (-not $DryRun -and $OpenInBrowser) {
			Wait-For-It -Seconds 5 `
				-Message "Restart your sensenet repository..." `
				-Silent $True
				-DryRun $DryRun
		}
	}

	if (-not $DryRun -and $OpenInBrowser) {
		Start-Process "https://admin.test.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A$SnHostPort"
	}

	Write-Output "You're welcome!"
}
# } catch {
# 	Write-Error "$_"
# }
