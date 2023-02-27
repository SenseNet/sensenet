Param (
	[Parameter(Mandatory=$False)]
	[boolean]$Install=$True,
    [Parameter(Mandatory=$False)]
	[boolean]$CleanUp=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$CreateImages=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$LocalSn=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$CreateDevCert=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$Uninstall=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$OpenInChrome=$True,
	[Parameter(Mandatory=$False)]
	[boolean]$DryRun=$False
)

if (-not (Get-Command "Wait-For-It" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/scripts/helper-functions.ps1"
}

if ($CreateDevCert) {
	./scripts/create-devcert.ps1 -ErrorAction stop
}

if ($CleanUp -or $Uninstall) {
    ./scripts/cleanup-sensenet.ps1 `
		-ProjectName sensenet-extsql `
		-SnType "InSql" `
		-ErrorAction stop
	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
        -ImageType InSql `
		-LocalSn $LocalSn
    ./scripts/create-images.ps1 `
        -ImageType Is `
		-ErrorAction stop
}

if ($Install) {
	./scripts/install-sensenet-init.ps1 -ErrorAction stop

	./scripts/install-sql-server.ps1 `
		-ProjectName sensenet-extsql `
		-UseDbContainer $False `
		-DataSource "QuietJoe" `
		-ErrorAction stop
 
	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-extsql `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8091 `
		-IsHostPort 8092 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-ErrorAction stop

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-extsql `
		-HostName "QuietJoe" `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InSql" `
		-SnHostPort 8091 `
		-SensenetPublicHost https://localhost:8091 `
		-IdentityPublicHost https://localhost:8092 `
		-UseDbContainer $False `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-ErrorAction stop
	
	Wait-For-It -Seconds 60	-Message "We are preparing your sensenet repository..." -DryRun $DryRun

	if (-not $DryRun -and $OpenInChrome) {
		Start-Process "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A8091"
	}

	Write-Output "Done."
}