Function Test-Docker {
	# Check if docker is running
	$ServerErrors = (ConvertFrom-Json -InputObject (docker info --format '{{json .}}')).ServerErrors
	if ($ServerErrors){
		Write-Error "Docker server is not running!"
		exit 1
	}
}

Function Invoke-Cli {
	Param (
		[CmdletBinding(DefaultParameterSetName = "auto")]
		[Parameter(ParameterSetName="auto", Mandatory=$True)]
		[string]$command,
		[Parameter(ParameterSetName="manual", Mandatory=$True)]
		[string]$execFile,
		[Parameter(ParameterSetName="manual", Mandatory=$False)]
		[string[]]$params,
		[Parameter(Mandatory=$False)]
		[string]$message,
		[Parameter(Mandatory=$False)]
		[boolean]$DryRun=$False
	)

	if ($command) {
		$cmdParts = $command.Trim().Split(" ")
		$execFile = $cmdParts[0]
		$params = $cmdParts | Select-Object -Skip 1
	}

	if ($message) { Write-Output $message}
	Write-Verbose "$execFile $($params -replace "eol", "```n`t")"
	if (-not $DryRun) {	
		& $execFile $($params -replace "eol", "")
		if ($LASTEXITCODE -ne 0) {
			Write-Error "Error in executing $execFile"
		}
	}
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
		exit 1
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
		write-Output "Git repository downloading started..."
		Invoke-Cli -execFile "git" -params "clone", "--progress", "-b", "$BranchName", "$Url", "$TargetPath" -ErrorAction Stop
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

	Invoke-Cli -command "docker build --progress plain -t $DockerImage -f $($DockerfilePath) $($SolutionPath)/src" -ErrorAction Stop

	write-output " "
	write-host "#################################"
	write-host "#       docker image info       #"
	write-host "#################################"

	Invoke-Cli -command "docker image ls $DockerImage" 
}


Function Wait-For-It {
	Param (		
		[Parameter(Mandatory=$True)]
		[int]$Seconds,
		[Parameter(Mandatory=$False)]
		[string]$Message
	)
	Write-Output $Message
	Start-Sleep -Seconds $Seconds
}
