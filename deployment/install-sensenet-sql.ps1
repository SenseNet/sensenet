Param (
    [Parameter(Mandatory=$False)]
	[boolean]$CleanUp=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$CreateImages=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$CreateDevCert=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$Uninstall=$False
)

if ($CreateDevCert) {
	./scripts/create-devcert.ps1
}

if ($CleanUp -or $Uninstall) {
    ./scripts/cleanup-sensenet.ps1 `
		-ProjectName sensenet-insql
	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
        -ImageType InSql
    ./scripts/create-images.ps1 `
        -ImageType Is
    ./scripts/create-images.ps1 `
        -ImageType Search
}

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
