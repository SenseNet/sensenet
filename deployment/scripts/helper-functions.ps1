Function Test-Docker {
	# Check if docker is running
	if ($DryRun) { return }

	$ServerErrors = (ConvertFrom-Json -InputObject (docker info --format '{{json .}}')).ServerErrors
	if ($ServerErrors){
		Write-Error "Docker server is not running!"
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
		[bool]$DryRun=$False
	)

	if ($command) {
		$cmdParts = $command.Trim().Split(" ")
		$execFile = $cmdParts[0]
		$params = $cmdParts | Select-Object -Skip 1
	}

	if ($message) { Write-Output $message}
	Write-Verbose "$execFile $($params -replace "eol", "```r`n`t")"
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
		[string]$BranchName = "main",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)

	if (-not $Url -or -not $TargetPath)
	{
		Write-Error "Repository url or target path missing!"
		exit 1
	}
	
	if (Test-Path $TargetPath\.git) {
		$TargetPath = Resolve-Path $TargetPath
		Write-Output "Template folder already exists!"
		
		$currentBranch = (git --git-dir="$TargetPath\.git" --work-tree="$TargetPath" rev-parse --abbrev-ref HEAD).Trim()
		Write-Output "Current branch: $currentBranch"

		if (-Not($BranchName -eq $currentBranch)) {
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "fetch" -DryRun $DryRun -ErrorAction Stop
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "checkout", $BranchName -DryRun $DryRun -ErrorAction Stop
		}		
		Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath\.git", "--work-tree=$TargetPath", "pull" -DryRun $DryRun -ErrorAction Stop
	} 
	else 
	{
		Write-Output "Git repository downloading started..."
		Invoke-Cli -execFile "git" -params "clone", "--progress", "-b", "$BranchName", "$Url", "$TargetPath" -DryRun $DryRun -ErrorAction Stop
	}
}

Function New-DockerImage {
	Param (
		[Parameter(Mandatory=$False)]
		[string]$SolutionPath="",
		[Parameter(Mandatory=$False)]
		[string]$DockerImage="",
		[Parameter(Mandatory=$False)]
		[string]$DockerFilePath="",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)

	Write-Output " "
	Write-Output "###################################"
	Write-Output "#       create docker image       #"
	Write-Output "###################################"

	Invoke-Cli -command "docker build --progress plain -t $DockerImage -f $($DockerfilePath) $($SolutionPath)/src" -DryRun $DryRun -ErrorAction Stop

	Write-Output " "
	Write-Output "#################################"
	Write-Output "#       docker image info       #"
	Write-Output "#################################"

	Invoke-Cli -command "docker image ls $DockerImage" -DryRun $DryRun
}

Function Remove-Database {
	Param (
		[Parameter(Mandatory = $True)]
		[string]$ServerName,
		[Parameter(Mandatory = $True)]
		[string]$CatalogName,
		[Parameter(Mandatory = $False)]
		[string]$UserName,
		[Parameter(Mandatory = $False)]
		[string]$UserPsw
	)

	try {
		# https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.management.smo.database.drop.aspx
		# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SMO') | out-null 
		# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SmoExtended') | out-null 

		if ($ServerName -eq "" -or $CatalogName -eq "") {
			Write-Error "ServerName or CatalogName is missing!"
			return
		}

		Write-Verbose "Initialize Drop $CatalogName on $ServerName with $UserName..."		

		Import-Module SQLServer -DisableNameChecking

		#Set variables 
		$dbServer = new-object ('Microsoft.SqlServer.Management.Smo.Server') $ServerName 

		if ($UserName) {
			#This sets the connection to mixed-mode authentication
			$dbServer.ConnectionContext.LoginSecure = $false;

			#This sets the login name
			$dbServer.ConnectionContext.set_Login($UserName);
		
			#This sets the password
			$dbServer.ConnectionContext.set_Password($UserPsw)
		}

		$databases = $dbServer.Databases 
		$dbname = $CatalogName 
		$db = $databases[$dbname]

		if ($db) {
			$dbServer.KillAllProcesses("$dbname")
			#$dbServer.KillDatabase("$dbname")
			#$dbServer.KillProcess(52)
			$db.drop()
			Write-Output "$dbname has been removed."
		}
		else {
				Write-Verbose "$dbname doesn't exists!"
		}
	}
	catch {
		Write-Error "Something went wrong!"
	}
}

Function New-Database {
	Param (
		[Parameter(Mandatory = $True)]
		[string]$ServerName,
		[Parameter(Mandatory = $True)]
		[string]$CatalogName,
		[Parameter(Mandatory = $False)]
		[string]$UserName,
		[Parameter(Mandatory = $False)]
		[string]$UserPsw
	)

	# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SMO') | out-null 
	# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SmoExtended') | out-null 

	Import-Module SQLServer -DisableNameChecking

	#Set variables 
	$dbServer = new-object ('Microsoft.SqlServer.Management.Smo.Server') $ServerName 
	Write-Verbose "Create $CatalogName on $ServerName with $USerName"

	if ($UserName) {
		#This sets the connection to mixed-mode authentication
		$dbServer.ConnectionContext.LoginSecure = $false;

		#This sets the login name
		$dbServer.ConnectionContext.set_Login($UserName);
		
		#This sets the password
		$dbServer.ConnectionContext.set_Password($UserPsw)
	}

	DO {
		Write-Verbose "check server availability..."
		$isServerAvailable = $False
		try {	 
			$dbServer.ConnectionContext.Connect()
			Write-Verbose "server available!"
			$isServerAvailable = $True 
		}
		catch {
			Write-Verbose "server not yet available!"
			$isServerAvailable = $False
			Start-Sleep -s 10
		}

	} Until ($isServerAvailable)

	$databases = $dbServer.Databases 
	$dbname = $CatalogName
	$db = $databases[$dbname]

	if ($db) {
		Write-Verbose "$dbname already exists!"
	}
	else {
		Write-Verbose "$dbname doesn't exists!"
		try {
			$db = New-Object Microsoft.SqlServer.Management.Smo.Database($dbServer, $dbname)
			$db.RecoveryModel = "Simple"
			$db.Create()
			Write-Output "$dbname has been created."
		} 
		catch {
			Write-Error "$_.Exception"
		}	
	}
}

Function Wait-RemoteDbServer {
	Param (
		[Parameter(Mandatory = $True)]
		[string]$ServerName,
		[Parameter(Mandatory = $False)]
		[string]$UserName,
		[Parameter(Mandatory = $False)]
		[string]$UserPsw,
		[Parameter(Mandatory = $False)]
		[int]$WaintInSeconds = 10,
		[Parameter(Mandatory = $False)]
		[int]$MaxTryNumber = 5
	)
	
	# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SMO') | out-null 
	# [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SmoExtended') | out-null 
	
	Import-Module SQLServer -DisableNameChecking
	
	#Set variables 
	$dbServer = new-object ('Microsoft.SqlServer.Management.Smo.Server') $ServerName 
	
	Write-Verbose "username: $UserName"
	if ($UserName) {
		Write-Output "username: $UserName"
	
		#This sets the connection to mixed-mode authentication
		$dbServer.ConnectionContext.LoginSecure = $false;
	
		#This sets the login name
		$dbServer.ConnectionContext.set_Login($UserName);
		
		#This sets the password
		$dbServer.ConnectionContext.set_Password($UserPsw)
	}
	
	DO {
		Write-Verbose "check server availability..."
		$isServerAvailable = $False
		try {	 
			$dbServer.ConnectionContext.Connect()
			Write-Verbose "server available!"
			$isServerAvailable = $True 
		}
		catch {
			if ($MaxTryNumber-- -gt 0) {
				Write-Verbose "server not yet available!"
				$isServerAvailable = $False
				Start-Sleep -s $WaintInSeconds
			} else {
				Write-Error "Wait for sql server timed out! ($($MaxTryNumber * $WaintInSeconds)s)"
			}
		}
	
	} Until ($isServerAvailable)
}

Function Wait-CntDbServer {
	Param (		
		[Parameter(Mandatory=$False)]
		[string]$ContainerName,
		[Parameter(Mandatory=$False)]
		[int]$RecheckInSeconds=10,
		[Parameter(Mandatory=$False)]
		[int]$MaxTryNumber=6,
		[Parameter(Mandatory = $False)]
		[string]$UserName="sa",
		[Parameter(Mandatory = $False)]
		[string]$UserPsw,
		[Parameter(Mandatory=$False)]
		[string]$Message,
		[Parameter(Mandatory=$False)]
		[bool]$Silent=$False,
		[Parameter(Mandatory=$False)]
		[string]$Progress="Output",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)
	
	Write-Verbose "Waiting for $ContainerName MsSql server to be ready..."
    DO {		
		$isServerAvailable = $False        
        $dummyQuery = $((docker exec $ContainerName /opt/mssql-tools/bin/sqlcmd -U $UserName -P $UserPsw -Q "SET NOCOUNT ON; select count(name) from sys.databases" -h -1).Trim())
		Write-Verbose "[$dummyQuery]"
        if ([int]$dummyQuery -gt 0) {
            Write-Verbose "server available!"
            $isServerAvailable = $True 
        } else {
            Write-Verbose "server not yet available!"
            $isServerAvailable = $False
            Wait-For-It -Seconds $RecheckInSeconds -Message "waiting..." -ShowSeconds $False -Silent $Silent -DryRun $DryRun

            if ($MaxTryNumber-- -lt 0) {
                Write-Error "Wait for sql server timed out! ($($MaxTryNumber * $RecheckInSeconds)s)"
                return
            }
        }
	} Until ($isServerAvailable)
}

Function Wait-For-It {
	Param (		
		[Parameter(Mandatory=$True)]
		[int]$Seconds,
		[Parameter(Mandatory=$False)]
		[string]$Message,
		[Parameter(Mandatory=$False)]
		[string]$StatusPostFix=" seconds left",
		[Parameter(Mandatory=$False)]
		[bool]$ShowSeconds=$True,
		[Parameter(Mandatory=$False)]
		[bool]$Silent=$False,
		[Parameter(Mandatory=$False)]
		[string]$Progress="Output",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)
	
	if ($Progress -eq "Bar" -and $Message) { Write-Output $Message }
	if (-not $DryRun) {
		$lenght = $Seconds / 100
		For ($Seconds; $Seconds -gt 0; $Seconds--) {
			if (-not $Silent) {
				if ($ShowSeconds) {	$status = $Seconds + $StatusPostFix} 
				if ($Progress -eq "Bar") {
					Write-Progress -Activity $Message -Status $status -PercentComplete ($Seconds / $lenght)
				} elseif ($Progress -eq "Output") {
					if ($seconds % 10 -eq 0) {
						Write-Output "$("$Message $status".Trim())"
					}
				}
			}
			Start-Sleep 1
		}
	}
}

Function Wait-Container {
	Param (		
		[Parameter(Mandatory=$False)]
		[string]$ContainerName,
		[Parameter(Mandatory=$False)]
		[int]$RecheckInSeconds=10,
		[Parameter(Mandatory=$False)]
		[int]$MaxTryNumber=6,
		[Parameter(Mandatory=$False)]
		[string]$Message,
		[Parameter(Mandatory=$False)]
		[bool]$Silent=$False,
		[Parameter(Mandatory=$False)]
		[string]$Progress="Output",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)

	if ($Message) {	Write-Output $Message }
	if (-not $DryRun) {
		Write-Output "Waiting for $($ContainerName) container to be ready..."
		DO {		
			$isContainerAvailable = $False        
			$cntStatus = $( docker container inspect -f "{{.State.Status}}" $ContainerName )
			if ($cntStatus -eq "running") {
				Write-Output "$($ContainerName) container available!"
				$isContainerAvailable = $True 
			} else {
				Write-Verbose "$($ContainerName) container not yet available!"
				$isContainerAvailable = $False
				Wait-For-It -Seconds $RecheckInSeconds -Message "waiting..." -ShowSeconds $False -Silent $Silent -DryRun $DryRun 
	
				if ($MaxTryNumber-- -lt 0) {
					Write-Error "Wait for $($ContainerName) container timed out! ($($MaxTryNumber * $RecheckInSeconds)s)"
					return
				}
			}
		} Until ($isContainerAvailable)
	}
}

Function Wait-SnApp {
	Param (		
		[Parameter(Mandatory=$True)]
		[int]$SnHostPort,
		[Parameter(Mandatory=$False)]
		[int]$RecheckInSeconds=10,
		[Parameter(Mandatory=$False)]
		[int]$MaxTryNumber=6,
		[Parameter(Mandatory=$False)]
		[string]$Message,
		[Parameter(Mandatory=$False)]
		[bool]$Silent=$False,
		[Parameter(Mandatory=$False)]
		[string]$Progress="Output",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)

	Write-Output "Waiting for https://localhost:$($SnHostPort) snapp to be ready..."
	DO {		
		$isSnAppAvailable = $False
		try {	 
			$snApiStatusCode = (Invoke-WebRequest -Uri https://localhost:$($SnHostPort)/odata.svc/Root -Verbose:$false).StatusCode
			if ($snApiStatusCode = 200) {
				Write-Output "sn api available!"
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
				Wait-For-It -Seconds $RecheckInSeconds -Message "waiting..." -ShowSeconds $False -Silent $Silent -DryRun $DryRun 
			} else {
				Write-Error "Wait for snapp timed out! ($($MaxTryNumber * $RecheckInSeconds)s)"
			}
		}
	} Until ($isSnAppAvailable)
}