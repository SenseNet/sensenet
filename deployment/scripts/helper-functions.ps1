Function Invoke-Cli {
	Param (
		[CmdletBinding(DefaultParameterSetName = "auto")]
		[Parameter(ParameterSetName="auto", Mandatory=$True)]
		[string]$command,
		[Parameter(ParameterSetName="manual", Mandatory=$True)]
		[string]$execFile,
		[Parameter(ParameterSetName="manual", Mandatory=$False)]
		[string[]]$params
	)

	if ($command) {
		$cmdParts = $command.Trim().Split(" ")
		$execFile = $cmdParts[0]
		$params = $cmdParts | Select-Object -Skip 1
		# Write-Output "exec: $execFile"
		# Write-Output "params: $params"
	}

	write-host "$execFile $($params -replace "eol", "```n`t")"
	& $execFile $($params -replace "eol", "")
}

Function Wait-For-It {
	Param (
		[Parameter(Mandatory=$True)]
		[int]$ReadyBy
	)
	Write-Output "Your sensenet repository is about to make. It can take about a minute."
	Start-Sleep -Seconds $ReadyBy
}