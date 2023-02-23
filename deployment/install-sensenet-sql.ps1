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
	./scripts/create-devcert.ps1
}

if ($CleanUp -or $Uninstall) {
    ./scripts/cleanup-sensenet.ps1 `
		-ProjectName sensenet-insql `
		-SnType "InSql"
	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
        -ImageType InSql `
		-LocalSn $LocalSn
    ./scripts/create-images.ps1 `
        -ImageType Is
}

if ($Install) {
	./scripts/install-sensenet-init.ps1

	./scripts/install-sql-server.ps1 `
		-ProjectName sensenet-insql

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-insql `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8091 `
		-IsHostPort 8092 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-insql `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InSql" `
		-SnHostPort 8091 `
		-SensenetPublicHost https://localhost:8091 `
		-IdentityPublicHost https://localhost:8092 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%
	
	Wait-For-It -Seconds 60	-Message "We are preparing your sensenet repository..." -DryRun $DryRun

	if (-not $DryRun -and $OpenInChrome) {
		Start-Process "chrome" "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A8091"
	}
}