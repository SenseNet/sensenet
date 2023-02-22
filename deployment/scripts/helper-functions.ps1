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
		[boolean]$DryRun=$False
	)

	if ($command) {
		$cmdParts = $command.Trim().Split(" ")
		$execFile = $cmdParts[0]
		$params = $cmdParts | Select-Object -Skip 1
		# Write-Output "exec: $execFile"
		# Write-Output "params: $params"
	}

	write-host "$execFile $($params -replace "eol", "```n`t")"
	if (-not $DryRun) {	
		& $execFile $($params -replace "eol", "")
		if ($LASTEXITCODE -gt 0) {
			write-error "Error in executing $execFile"
		}
	}
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