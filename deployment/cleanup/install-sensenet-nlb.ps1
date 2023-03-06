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
	[boolean]$Uninstall=$False,
	[Parameter(Mandatory=$False)]
	[boolean]$OpenInBrowser=$True,
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
        -ProjectName sensenet-nlb `
		-SnType "InSqlNlb" `
        -WithServices $True `
		-UseGrpc $True `
		-DryRun $DryRun `
		-ErrorAction stop
	if ($Uninstall) {
		exit 0;
	}
}

if ($CreateImages) {
    ./scripts/create-images.ps1 `
    	-ImageType All `
		-LocalSn $LocalSn `
		-DryRun $DryRun `
		-ErrorAction stop
}

if ($Install) {
	./scripts/install-sensenet-init.ps1 -DryRun $DryRun -ErrorAction stop
	./scripts/install-rabbit.ps1 -DryRun $DryRun -ErrorAction stop
	./scripts/install-sql-server.ps1 -ProjectName sensenet-nlb -SqlPsw QWEasd123% -DryRun $DryRun -ErrorAction stop

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8095 `
		-IsHostPort 8096 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun `
		-ErrorAction stop

	./scripts/install-search-service.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SearchHostPort 8097 `
		-RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun `
		-ErrorAction stop

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-nlb `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InSqlNlb" `
		-SnHostPort 8095 `
		-SensenetPublicHost https://localhost:8095 `
		-IdentityPublicHost https://localhost:8096 `
		-SqlPsw QWEasd123% `
		-RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun `
		-ErrorAction stop

	Wait-For-It -Seconds 60	-Message "We are preparing your sensenet repository..." -DryRun $DryRun

	./scripts/install-search-service.ps1 `
		-ProjectName sensenet-nlb `
		-Restart $True `
		-DryRun $DryRun `
		-ErrorAction stop

	if (-not $DryRun -and $OpenInBrowser) {
		Start-Process "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A8095"
	}

	Write-Output "Done."
}