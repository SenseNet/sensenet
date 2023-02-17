Param (
	[Parameter(Mandatory=$False)]
	[string]$ImageType="InMem",
	[Parameter(Mandatory=$False)]
	[boolean]$LocalSn=$False,

	[Parameter(Mandatory=$False)]
	[string]$SensenetGitRepo="https://github.com/SenseNet/sensenet",
	[Parameter(Mandatory=$False)]
	[string]$SensenetGitBranch="master",
	[Parameter(Mandatory=$False)]
	[string]$SensenetSlnPath,

	[Parameter(Mandatory=$False)]
	[string]$IdentityGitRepo="https://github.com/SenseNet/sn-identityserver",
	[Parameter(Mandatory=$False)]
	[string]$IdentityGitBranch="main",
	[Parameter(Mandatory=$False)]
	[string]$SnIsDockerImage="sensenet-identityserver"
)

##################
##    Helpers    #
##################
Function Invoke-Cli {
	Param (
		[Parameter(Mandatory=$True)]
		[string]$execFile,
		[Parameter(Mandatory=$False)]
		[string[]]$params	
	)

	write-host "$execFile $($params -replace "eol", "```n`t")"
	& $execFile $($params -replace "eol", "")
}

Function Get-GitRepo {
	Param (
		[Parameter(Mandatory=$False)]
		[string]$Url = "https://tfs.sensenet.com/SenseNetProductTeam/SenseNet/_git/sn-identityserver",
		[Parameter(Mandatory=$False)]
		[string]$TargetPath = "..\..\temp\IdentitySln",
		[Parameter(Mandatory=$False)]
		[string]$BranchName = "master"
	)
	
	if (Test-Path $TargetPath\.git) {
		write-host "Template folder already exists!"
		Write-Output "git --git-dir=`"$TargetPath\.git`" --work-tree=`"$TargetPath`" rev-parse --abbrev-ref HEAD"
		$currentBranch = (git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" rev-parse --abbrev-ref HEAD).Trim()
	
		Write-Output "Current branch: $currentBranch"
		if (-Not($BranchName -eq $currentBranch)) {
			Write-Output "git --git-dir=`"$TargetPath\.git`" --work-tree=`"$TargetPath`" fetch"
			git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" fetch 
			Write-Output "git --git-dir=`"$TargetPath\.git`" --work-tree=`"$TargetPath`" checkout $BranchName"
			git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" checkout $BranchName 
		}
		Write-Output "git --git-dir=`"$TargetPath\.git`" --work-tree=`"$TargetPath`" pull"
		git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" pull 
	} 
	else 
	{
		try {
			write-Output "Git repository downloading started..."
			Invoke-Cli -execFile "git" -params "clone", "--progress", "-b", "$BranchName", "$Url", "$TargetPath"
		} catch {
			Write-Output "Exception: $_.Exception"
		}
	}
}

Function New-DockerImage {
	Param (
		[Parameter(Mandatory=$False)]
		[string]$SolutionPath="",
		[Parameter(Mandatory=$False)]
		[string]$DockerImage="",
		[Parameter(Mandatory=$False)]
		[string]$DockerFilePath=""
	)

	write-output " "
	write-host "###################################"
	write-host "#       create docker image       #"
	write-host "###################################"

	Invoke-Cli -execFile "docker" -params "build", "--progress", "plain", "-t", "$DockerImage", "-f", "$DockerfilePath", "$($SolutionPath)/src"
	if ($LASTEXITCODE -gt 0) {
		Write-Output "Image creation failed!"
		exit;	
	}

	write-output " "
	write-host "#################################"
	write-host "#       docker image info       #"
	write-host "#################################"

	Invoke-Cli -execFile "docker" -params "image", "ls", "$DockerImage"
}


# ==================================================================================

# Check if docker is running
$ServerErrors = (ConvertFrom-Json -InputObject (docker info --format '{{json .}}')).ServerErrors
if ($ServerErrors){
	Write-Output "Docker server is not running!"
	exit;	
}

#############################
##    Variables section     #
#############################
# Prepare sn solution if run script independently
if (-not $SensenetSlnPath) {
	if (-not $LocalSn) {
		$SensenetSlnPath="../temp/SnSolution"
	} else {
		$SensenetSlnPath="../.."
	}
}
if (-not $IdentitySlnPath) {
	$IdentitySlnPath="../temp/IdentitySln"
}
$creationList = @()

# Set dockerfile path according to app type
if ($ImageType -eq "InMem" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-inmem"
	$SensenetDockerfilePath="$($SensenetSlnPath)/src/WebApps/SnWebApplication.Api.InMem.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetSlnPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}
if ($ImageType -eq "InSql" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-sql"
	$SensenetDockerfilePath="$($SensenetSlnPath)/src/WebApps/SnWebApplication.Api.Sql.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetSlnPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}
if ($ImageType -eq "InSqlNlb" -or $ImageType -eq "All")
{
	$SensenetDockerImage="sn-api-nlb"
	$SensenetDockerfilePath="$($SensenetSlnPath)/src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth/Dockerfile"
	$imageFrom = @{
		SolutionPath=$SensenetSlnPath
		DockerImage=$SensenetDockerImage
		DockerFilePath=$SensenetDockerfilePath
	}
	$creationList += $imageFrom
}

if ($ImageType -eq "Is" -or $ImageType -eq "All")
{
	$IdentityDockerfilePath="$($IdentitySlnPath)/src/SenseNet.IdentityServer4.Web/Dockerfile"
	$imageFrom = @{
		SolutionPath=$IdentitySlnPath
		DockerImage=$SnIsDockerImage
		DockerFilePath=$IdentityDockerfilePath
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
	write-output " "
	write-host "############################"
	write-host "#       get git repo       #"
	write-host "############################"
	
	Get-GitRepo -Url "$SensenetGitRepo" -TargetPath $SensenetSlnPath -BranchName "$SensenetGitBranch" 
}

if ($ImageType -eq "Is" -or 	
	$ImageType -eq "All") {
	write-output " "
	write-host "############################"
	write-host "#       get git repo       #"
	write-host "############################"
	
	Get-GitRepo -Url "$IdentityGitRepo" -TargetPath $IdentitySlnPath -BranchName "$IdentityGitBranch" 
}

###############################
##    Build Docker Images     #
###############################

Foreach ($item in $creationList) {
	# $param = @{
	# 	SolutionPath=$item.SolutionPath
	# 	DockerImage=$item.DockerImage
	# 	DockerFilePath=$item.DockerfilePath
	# }
	New-DockerImage @item
}