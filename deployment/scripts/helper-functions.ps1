Function Test-Docker {
	# Check if docker is running
	if ($DryRun) { return }

	$dockerResult = (docker info --format '{{json .}}')
	if ($LASTEXITCODE -ne 0 -or -not $dockerResult) { throw "Docker server is not available." }
	if ($dockerResult -is [array] -and $dockerResult[1] -is [string]) {
		# workaround for "failed to get console mode for stdout: The handle is invalid."
    	$dockerResult = $dockerResult[1]
	}
	$ServerErrors = (ConvertFrom-Json -InputObject $dockerResult).ServerErrors
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

	$eolChar = "``"
	if ($IsLinux) { $eolChar = "\" }

	if ($command) {
		$cmdParts = $command.Trim().Split(" ")
		$execFile = $cmdParts[0]
		$params = $cmdParts | Select-Object -Skip 1
	}

	if ($message) { Write-Output $message}
	Write-Verbose "$execFile $($params -replace "eol", "$($eolChar)`r`n`t")"
	if (-not $DryRun) {	
		& $execFile $($params | where-object {$_ -ne "eol"})
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
	if ($DryRun) {
		Invoke-Cli -execFile git -params "clone", "-b", $BranchName, $Url, $TargetPath -DryRun $True
		return
	}
	
	if (Test-Path $TargetPath/.git) {
		$TargetPath = Resolve-Path $TargetPath
		Write-Output "Template folder already exists!"
		
		$currentBranch = (git --git-dir="$TargetPath/.git" --work-tree="$TargetPath" rev-parse --abbrev-ref HEAD).Trim()
		Write-Output "Current branch: $currentBranch"

		if (-Not($BranchName -eq $currentBranch)) {
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath/.git", "--work-tree=$TargetPath", "fetch" -DryRun $DryRun -ErrorAction Stop
			Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath/.git", "--work-tree=$TargetPath", "checkout", $BranchName -DryRun $DryRun -ErrorAction Stop
		}		
		Invoke-Cli -execFile "git" -params "--git-dir=$TargetPath/.git", "--work-tree=$TargetPath", "pull" -DryRun $DryRun -ErrorAction Stop
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
		[string]$BuildContext="",
		[Parameter(Mandatory=$False)]
		[bool]$DryRun=$False
	)

	Write-Output " "
	Write-Output "###################################"
	Write-Output "#       create docker image       #"
	Write-Output "###################################"

	if (-not $BuildContext) { $BuildContext = "$SolutionPath/src" }
	Invoke-Cli -execFile docker -params "build", "--progress", "plain", "-t", $DockerImage, "-f", $DockerfilePath, $BuildContext -DryRun $DryRun -ErrorAction Stop

	Write-Output " "
	Write-Output "#################################"
	Write-Output "#       docker image info       #"
	Write-Output "#################################"

	Invoke-Cli -execFile docker -params "image", "ls", $DockerImage -DryRun $DryRun
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
		[string]$SqlCmdPath="/opt/mssql-tools18/bin/sqlcmd",
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
	if (-not $DryRun) {
		DO {
			$isServerAvailable = $False
			try {
				$dummyQuery = $((docker exec $ContainerName $SqlCmdPath -C -b -U "$($UserName)" -P "$($UserPsw)" -Q "SET NOCOUNT ON; select count(name) from sys.databases" -h -1).Trim())
				if ($dummyQuery -is [array] -and $dummyQuery[1] -is [string]) {
					# workaround for "failed to get console mode for stdout: The handle is invalid."
					$dummyQuery = $dummyQuery[1]
				}
			} catch {
				$dummyQuery = -1
			}
			Write-Verbose "[$dummyQuery]"
			if ([int]$dummyQuery -gt 0) {
				Write-Output "server available!"
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

	if ($Progress -eq "Bar" -and $Message) { Write-Output $Message }
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
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$True)][int]$SnHostPort,
        [int]$RecheckInSeconds=10,
        [int]$MaxTryNumber=6,
        [string]$Message,
        [bool]$Silent=$False,
        [string]$Progress="Output",
        [bool]$DryRun=$False
    )
    if ($DryRun) { return }
    for ($attempt = 0; $attempt -lt $MaxTryNumber; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:$SnHostPort/odata.svc/('Root')?`$select=Id,Path" -UseBasicParsing -TimeoutSec 10 -Verbose:$False
            $root = $response.Content | ConvertFrom-Json
            if ($response.StatusCode -eq 200 -and $root.d.Id -gt 0 -and $root.d.Path -eq '/Root') { return }
        } catch { Write-Verbose "Repository on port $SnHostPort is not ready: $($_.Exception.Message)" }
        if ($attempt + 1 -lt $MaxTryNumber) { Wait-For-It -Seconds $RecheckInSeconds -Silent $Silent -DryRun $DryRun }
    }
    throw "Wait for snapp timed out on port $SnHostPort after $MaxTryNumber attempts."
}

# Return one status on the success stream. Logs must not become part of the result.
Function Manage-Container {
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$True)][string]$ContainerName,
		[Parameter(Mandatory=$True)][string[]]$DockerRunParams,
		[bool]$DryRun=$False
	)

	if ($DryRun) {
		Invoke-Cli -execFile docker -params $DockerRunParams -DryRun $True
		return "created"
	}

	$status = docker container inspect -f '{{.State.Status}}' $ContainerName 2>$null
	if ($LASTEXITCODE -eq 0) {
		switch ($status) {
			"running" { Write-Verbose "$ContainerName is already running."; return "running" }
			{ $_ -in "exited", "created" } {
				Invoke-Cli -execFile docker -params "start", $ContainerName -ErrorAction Stop | Out-Host
				return "started"
			}
			default { throw "Container $ContainerName is in state '$status'. Resolve its state before retrying; it has not been removed." }
		}
	}

	Invoke-Cli -execFile docker -params $DockerRunParams -ErrorAction Stop | Out-Host
	return "created"
}

Function Get-SensenetImage {
	Param (
		[string]$SnType="InSql",
		[ValidateSet("Standard", "Preview", "LocalAuth", "PostgreSql")][string]$WebApp="Standard",
		[bool]$SearchService=$False,
		[string]$ImageTag=""
	)
	if (-not $ImageTag) { if ($WebApp -eq "PostgreSql") { $ImageTag="postgres-provider" } else { $ImageTag="preview" } }
	if ($SnType -eq "InMem") {
		return @{ Image="sensenetcsp/sn-api-inmem:$ImageTag"; Project="SnWebApplication.Api.InMem.TokenAuth" }
	}
	switch ($WebApp) {
		"LocalAuth" { return @{ Image="local/sn-api-sql-localauth:demo"; Project="SnWebApplication.Api.Sql.LocalAuth" } }
		"PostgreSql" { return @{ Image="sensenetcsp/sn-api-postgre:$ImageTag"; Project="SnWebApplication.Api.PostgreSql.TokenAuth" } }
	}
	$name = "sn-api-sql"
	$project = "SnWebApplication.Api.Sql.TokenAuth"
	if ($SearchService) { $name="sn-api-nlb"; $project="SnWebApplication.Api.Sql.SearchService.TokenAuth" }
	if ($WebApp -eq "Preview") { $name+="-prv"; $project+=".Preview" }
	return @{ Image="sensenetcsp/${name}:$ImageTag"; Project=$project }
}
