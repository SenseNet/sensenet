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
	[boolean]$Uninstall=$False
)

if (-not (Get-Command "Wait-For-It" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/scripts/helper-functions.ps1"
}

Test-Docker -ErrorAction stop

if ($CreateDevCert) {
	./scripts/create-devcert.ps1
}

if ($CleanUp -or $Uninstall) {
    ./scripts/cleanup-sensenet.ps1 `
        -ProjectName sensenet-inmem
	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
        -ImageType InMem `
		-LocalSn $LocalSn `
		-ErrorAction Stop
    ./scripts/create-images.ps1 `
        -ImageType Is `
		-ErrorAction Stop
}

if ($Install) {
	./scripts/install-sensenet-init.ps1

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-inmem `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8093 `
		-IsHostPort 8094 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%

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
		-CertPass QWEasd123%
}