Param (
	[Parameter(Mandatory=$False)]
	[string]$ImageType="InSql",
	[Parameter(Mandatory=$False)]
	[boolean]$LocalSn=$False,

	[Parameter(Mandatory=$False)]
	[string]$SensenetGitRepo="https://github.com/SenseNet/sensenet",
	[Parameter(Mandatory=$False)]
	[string]$SensenetGitBranch="master",
	[Parameter(Mandatory=$False)]
	[string]$SensenetFolderPath,
	[Parameter(Mandatory=$False)]
	[string]$SensenetDockerImage,

	[Parameter(Mandatory=$False)]
	[string]$IdentityGitRepo="https://github.com/SenseNet/sn-identityserver",
	[Parameter(Mandatory=$False)]
	[string]$IdentityGitBranch="main",
	[Parameter(Mandatory=$False)]
	[string]$IdentityDockerImage="sn-identityserver",

	[Parameter(Mandatory=$False)]
	[string]$SearchGitRepo="https://github.com/SenseNet/sn-search-lucene29",
	[Parameter(Mandatory=$False)]
	[string]$SearchGitBranch="master",	
	[Parameter(Mandatory=$False)]
	[string]$SearchDockerImage="sn-searchservice"
)

if (-not (Get-Command "Invoke-Cli" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Function Get-GitRepo {
	Param (
		[Parameter(Mandatory=$False)]
		[string]$Url,
		[Parameter(Mandatory=$False)]
		[string]$TargetPath,
		[Parameter(Mandatory=$False)]
		[string]$BranchName = "main"
	)

	if (-not $Url -or -not $TargetPath)
	{
		Write-Error "Repository url or target path missing!"
		# exit 1;
	}
	
	if (Test-Path $TargetPath\.git) {
		$TargetPath = Resolve-Path $TargetPath
		write-host "Template folder already exists!"
		
		$currentBranch = (git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" rev-parse --abbrev-ref HEAD).Trim()
		Write-Output "Current branch: $currentBranch"

		if (-Not($BranchName -eq $currentBranch)) {
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "fetch" -ErrorAction Stop
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "checkout", $BranchName -ErrorAction Stop
		}		
		Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "pull" -ErrorAction Stop
	} 
	else 
	{
		try {
			write-Output "Git repository downloading started..."
			Invoke-Cli -execFile "git" -params "clone", "--progress", "-b", "$BranchName", "$Url", "$TargetPath" -ErrorAction Stop
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

	Invoke-Cli -command "docker build --progress plain -t $DockerImage -f $DockerfilePath $($SolutionPath)/src" -ErrorAction Stop
	# if ($LASTEXITCODE -gt 0) {
	# 	Write-Error "Image creation failed!"
	# 	# exit;
	# }

	write-output " "
	write-host "#################################"
	write-host "#       docker image info       #"
	write-host "#################################"

	Invoke-Cli -command "docker image ls $DockerImage" 
}


# ==================================================================================

# Check if docker is running
$ServerErrors = (ConvertFrom-Json -InputObject (docker info --format '{{json .}}')).ServerErrors
if ($ServerErrors){
	Write-Error "Docker server is not running!"
	# exit 1;
}

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
	write-output " "
	write-host "############################"
	write-host "#       get git repo       #"
	write-host "############################"
	
	Get-GitRepo -Url "$SensenetGitRepo" -TargetPath $SensenetFolderPath -BranchName "$SensenetGitBranch" -ErrorAction Stop
}

if ($ImageType -eq "Is" -or 	
	$ImageType -eq "All") {
	write-output " "
	write-host "############################"
	write-host "#       get git repo       #"
	write-host "############################"
	
	Get-GitRepo -Url "$IdentityGitRepo" -TargetPath $identityFolderPath -BranchName "$IdentityGitBranch" -ErrorAction Stop
}

if ($ImageType -eq "Search" -or 	
	$ImageType -eq "All") {
	write-output " "
	write-host "#######################################"
	write-host "#       get search service repo       #"
	write-host "#######################################"
	
	Get-GitRepo -Url "$SearchGitRepo" -TargetPath $SearchFolderPath -BranchName "$SearchGitBranch" -ErrorAction Stop
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
	New-DockerImage @item -ErrorAction Stop
}