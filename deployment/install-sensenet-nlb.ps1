Param (
	[Parameter(Mandatory=$False)]
	[string]$ProjectName="sensenet-nlb",	
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

if ($CreateDevCert) {
	./scripts/create-devcert.ps1
}

if ($CleanUp -or $Uninstall) {
    ./scripts/cleanup-sensenet.ps1 `
        -ProjectName sensenet-nlb `
		-SnType "InSqlNlb" `
        -WithServices $True `
		-UseGrpc $True
	if ($Uninstall) {
		exit 0;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
    	-ImageType All `
		-LocalSn $LocalSn		
}

if ($Install) {
	./scripts/install-sensenet-init.ps1
	if ($LASTEXITCODE -gt 0) {exit 1}
	./scripts/install-rabbit.ps1 
	./scripts/install-sql-server.ps1 -ProjectName sensenet-nlb

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8095 `
		-IsHostPort 8096 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%

	./scripts/install-search-service.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SearchHostPort 8097 `
		-RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InSqlNlb" `
		-SnHostPort 8095 `
		-SensenetPublicHost https://localhost:8095 `
		-IdentityPublicHost https://localhost:8096 `
		-RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123%

	Wait-For-It -Seconds 60	-Message "We are preparing your sensenet repository..."

	./scripts/install-search-service.ps1 `
		-ProjectName sensenet-nlb `
		-Restart $True
}