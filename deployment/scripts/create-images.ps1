Param (
	[Parameter(Mandatory=$False)]
	[string]$ImageType="InSql",
	[Parameter(Mandatory=$False)]
	[bool]$LocalSn=$False,

	# Sensenet App
	[Parameter(Mandatory=$False)]
	[string]$SensenetGitRepo="https://github.com/SenseNet/sensenet",
	[Parameter(Mandatory=$False)]
	[string]$SensenetGitBranch="master",
	[Parameter(Mandatory=$False)]
	[string]$SensenetFolderPath,
	[Parameter(Mandatory=$False)]
	[string]$SensenetDockerImage,

	# Identity server
	[Parameter(Mandatory=$False)]
	[string]$IdentityGitRepo="https://github.com/SenseNet/sn-identityserver",
	[Parameter(Mandatory=$False)]
	[string]$IdentityGitBranch="main",
	[Parameter(Mandatory=$False)]
	[string]$IdentityDockerImage="sn-identityserver",

	# Search service parameters
	[Parameter(Mandatory=$False)]
	[string]$SearchGitRepo="https://github.com/SenseNet/sn-search-lucene29",
	[Parameter(Mandatory=$False)]
	[string]$SearchGitBranch="master",	
	[Parameter(Mandatory=$False)]
	[string]$SearchDockerImage="sn-searchservice",
	
	# Technical
	[Parameter(Mandatory=$False)]
	[bool]$DryRun=$False
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

# ==================================================================================

#############################
##    Variables section     #
#############################
# Prepare sn solution if run script independently
$creationList = @()
if (-not $SensenetFolderPath) {
	if (-not $LocalSn) {
		$SensenetFolderPath="$($PSScriptRoot)/../temp/Sensenet"
	} else {
		$SensenetFolderPath="$($PSScriptRoot)/../../"
	}
}

# Set image creator params according to app type
if ($ImageType -eq "InMem" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-inmem"
	$SensenetDockerfilePath="$($SensenetFolderPath)/src/WebApps/SnWebApplication.Api.InMem.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetFolderPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}
if ($ImageType -eq "InSql" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-sql"
	$SensenetDockerfilePath="$($SensenetFolderPath)/src/WebApps/SnWebApplication.Api.Sql.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetFolderPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}
if ($ImageType -eq "InSqlNlb" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-nlb"
	$SensenetDockerfilePath="$($SensenetFolderPath)/src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetFolderPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}

if ($ImageType -eq "Is" -or $ImageType -eq "All")
{
	$identityFolderPath="$($PSScriptRoot)/../temp/Identity"
	$IdentityDockerfilePath="$($identityFolderPath)/src/SenseNet.IdentityServer4.Web/Dockerfile"
	$imageFrom = @{
		SolutionPath=$identityFolderPath
		DockerImage=$IdentityDockerImage
		DockerFilePath=$IdentityDockerfilePath
	}
	$creationList += $imageFrom
}

if ($ImageType -eq "Search" -or $ImageType -eq "All")
{
	$SearchFolderPath="$($PSScriptRoot)/../temp/SearchService"
	$SearchDockerfilePath="$($SearchFolderPath)/src/SenseNet.Search.Lucene29.Centralized.GrpcService/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SearchFolderPath
		DockerImage=$SearchDockerImage
		DockerFilePath=$SearchDockerfilePath
	}
	$creationList += $imageFrom
}

#############################
##    Prepare Solutions     #
#############################

if (-not $LocalSn -and
	($ImageType -eq "InMem" -or 
	$ImageType -eq "InSql" -or
	$ImageType -eq "InSqlNlb" -or
	$ImageType -eq "All")) {
	Write-Output " "
	Write-Output "############################"
	Write-Output "#       get git repo       #"
	Write-Output "############################"
	
	Get-GitRepo -Url "$SensenetGitRepo" -TargetPath $SensenetFolderPath -BranchName "$SensenetGitBranch" -DryRun $DryRun -ErrorAction Stop
}

if ($ImageType -eq "Is" -or 	
	$ImageType -eq "All") {
	Write-Output " "
	Write-Output "############################"
	Write-Output "#       get git repo       #"
	Write-Output "############################"
	
	Get-GitRepo -Url "$IdentityGitRepo" -TargetPath $identityFolderPath -BranchName "$IdentityGitBranch" -DryRun $DryRun -ErrorAction Stop
}

if ($ImageType -eq "Search" -or 	
	$ImageType -eq "All") {
	Write-Output " "
	Write-Output "#######################################"
	Write-Output "#       get search service repo       #"
	Write-Output "#######################################"
	
	Get-GitRepo -Url "$SearchGitRepo" -TargetPath $SearchFolderPath -BranchName "$SearchGitBranch" -DryRun $DryRun -ErrorAction Stop
}

###############################
##    Build Docker Images     #
###############################

Foreach ($item in $creationList) {
	New-DockerImage @item -DryRun $DryRun -ErrorAction Stop
}