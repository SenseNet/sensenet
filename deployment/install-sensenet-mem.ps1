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
        -ProjectName sensenet-inmem `
		-SnType "InMem" `
		-DryRun $DryRun
	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
        -ImageType InMem `
		-LocalSn $LocalSn `
		-DryRun $DryRun `
		-ErrorAction Stop 
    ./scripts/create-images.ps1 `
        -ImageType Is `
		-DryRun $DryRun `
		-ErrorAction Stop
}

if ($Install) {
	./scripts/install-sensenet-init.ps1 -DryRun $DryRun

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-inmem `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8093 `
		-IsHostPort 8094 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-inmem `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InMem" `
		-SnHostPort 8093 `
		-SensenetPublicHost https://localhost:8093 `
		-IdentityPublicHost https://localhost:8094 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun

	Wait-For-It -Seconds 30	-Message "We are preparing your sensenet repository..." -DryRun $DryRun

	if (-not $DryRun -and $OpenInChrome) {
		Start-Process "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A8093"
	}

	Write-Output "Done."
}