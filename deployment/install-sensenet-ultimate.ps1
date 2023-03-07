Param (
	[Parameter(Mandatory=$False)]
	[string]$ProjectName,
	[Parameter(Mandatory=$False)]
	[string]$PreSet="InSql",

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

#############################
##    Variables section     #
#############################
$AppEnvironment="Development"

# different sensenet types with different names for combined demo
if (-not $ProjectName) {
	$ProjectName = "sensenet-$($PreSet.ToLower())"
	if ($PreSet -eq "InSql") {
		if ($UseDbContainer) { $ProjectName += "-cdb" } else { $ProjectName += "-hdb" }
		if ($UseVolume) { $ProjectName += "-wv" }
		if ($SearchService) { $ProjectName += "-ws" }
	}
}

# different sensenet types on different ports for combined demo
$basePort = 51000
$portModifier = 0
if ($PreSet -eq "InSql") {
	$portModifier = 5 * ($PreSet -eq "InSql") + 10 * $UseDbContainer + 20 * $UseVolume + 30 * $SearchService
}
$SnHostPort=$basePort + 11 + $portModifier
$IsHostPort=$basePort + 12 + $portModifier
$SearchHostPort=$basePort + 13 + $portModifier
#dbport?

# Workaround for relative path on host machine
if ($VolumeBasePath.StartsWith("./") -or 
	$VolumeBasePath.StartsWith("../")) {
	$VolumeBasePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($VolumeBasePath)
}

# Developer certificate demo password
$CertPass="QWEasd123%"

# Wait time in seconds before try to connect to sensenet repository
$WaitForSnInSeconds = 60

switch ($PreSet) {
	"InMem" {  
		$SnType="InMem"
		$SearchService = $False
		$UseDbContainer = $False
		$WaitForSnInSeconds = 30
		$imageTypes="InMem","Is"
		$cleanupSetup=@{
			ProjectName=$ProjectName
			SnType=$SnType
			UseVolume=$UseVolume
		}
	}
	"InSql" {		
		if (-not $SearchService) {
			$SnType="InSql"
			$imageTypes="InSql","Is"
			$cleanupSetup=@{
				ProjectName=$ProjectName
				SnType=$SnType
				UseDbContainer=$UseDbContainer
				UseVolume=$UseVolume
			}
		} else {
			$SnType="InSqlNlb"
			$imageTypes="InSql", "Is", "Search"
			$cleanupSetup=@{
				ProjectName=$ProjectName
				SnType=$SnType
				UseDbContainer=$UseDbContainer
				WithServices=$False
				UseGrpc=$True
				UseVolume=$UseVolume
			}
		}
	}
	Default {
		Write-Error "-PreSet is either 'InMem' or 'InSql'."
	}
}

if ($SnType -eq "InSql" -or $SnType -eq "InSqlNlb") {
	if ($HostName) {
		if (-not $SqlUser -or -not $SqlPsw) {
			$SqlUser = Read-Host -Prompt 'Input your mssql user name'
			$SqlPsw = Read-Host -Prompt 'Input the mssql user password'		
		}		
	} else {
		# db in container
		if (-not $SqlPsw) {
			$SqlUser = "sa"
			$SqlPsw = "QWEasd123%"
		}
	} 
}

#====================== prerequisites ======================

if ($CreateDevCert) {
	./scripts/create-devcert.ps1 `
		-VolumeBasePath $VolumeBasePath `
		-CertPsw $CertPass `
		-DryRun $DryRun `
		-ErrorAction stop
}

if ($CreateImages) {
	foreach ($imageType in $imageTypes) {
		./scripts/create-images.ps1 `
			-ImageType $imageType `
			-LocalSn $LocalSn `
			-DryRun $DryRun `
			-ErrorAction stop
	}
}

#====================== clean up ======================

if ($CleanUp -or $Uninstall) {
	./scripts/cleanup-sensenet.ps1 `
		@cleanupSetup `
		-VolumeBasePath $VolumeBasePath `
		-DryRun $DryRun `
		-ErrorAction stop
	
	if ($PreSet -eq "InSql" -and -not $UseDbContainer -and -not $KeepRemoteDatabase) {
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
			-DryRun $DryRun `
			-ErrorAction stop
	}

	if ($PreSet -eq "InSql") {
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
		-CertPass $CertPass `
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
			-SearchHostPort $SearchHostPort `
			-RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
			-CertPass $CertPass `
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
		-CertPass $CertPass `
		-UseVolume $UseVolume `
		-DryRun $DryRun `
		-ErrorAction stop
	
	Wait-For-It -Seconds $WaitForSnInSeconds `
		-Message "We are preparing your sensenet repository..." `
		-DryRun $DryRun

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
	}

	if (-not $DryRun -and $OpenInBrowser) {
		Start-Process "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A$SnHostPort"
	}

	Write-Output "You're welcome!"
}
# } catch {
# 	Write-Error "$_"
# }
