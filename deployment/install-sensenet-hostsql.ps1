Param (
	[Parameter(Mandatory=$False)]
	[string]$HostName="",
	[Parameter(Mandatory=$False)]
    [string]$DataSource="$($HostName)",

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

# Sql Server Configuration Manager / SQL Server Network Configuration / Protocols -> TCP/IP=Enabled
# try {
if (-not (Get-Command "Wait-For-It" -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/scripts/helper-functions.ps1"
}

if ($CreateDevCert) {
	./scripts/create-devcert.ps1 -ErrorAction stop
}

if ($HostName -or $DataSource) {
	$SqlUSer = Read-Host -Prompt 'Input your mssql user name'
	$SqlPsw = Read-Host -Prompt 'Input the mssql user password'
	$UseDbContainer = $False
} else {
	Write-Error "HostName or DataSource required!" -ErrorAction stop
	# $UseDbContainer = $True
}

if ($CleanUp -or $Uninstall) {
	./scripts/cleanup-sensenet.ps1 `
		-ProjectName sensenet-extsql `
		-SnType "InSql" `
		-UseDbContainer $UseDbContainer `
		-DryRun $DryRun `
		-ErrorAction stop
	
	if (-not $UseDbContainer) {
		./scripts/install-sql-server.ps1 `
			-ProjectName sensenet-extsql `
			-HostName $Hostname `
			-UseDbContainer $UseDbContainer `
			-DataSource $DataSource `
			-SqlUser $SqlUSer `
			-SqlPsw $SqlPsw `
			-Uninstall $True `
			-DryRun $DryRun `
			-ErrorAction stop
	}

	if ($Uninstall) {
		exit;
	}
}

if ($CreateImages) {
	./scripts/create-images.ps1 `
		-ImageType InSql `
		-LocalSn $LocalSn `
		-DryRun $DryRun `
		-ErrorAction stop
	./scripts/create-images.ps1 `
		-ImageType Is `
		-DryRun $DryRun `
		-ErrorAction stop
}

if ($Install) {
	./scripts/install-sensenet-init.ps1 -ErrorAction stop

	./scripts/install-sql-server.ps1 `
		-ProjectName sensenet-extsql `
		-HostName $Hostname `
		-UseDbContainer $UseDbContainer `
		-DataSource $DataSource `
		-SqlUser $SqlUSer `
		-SqlPsw $SqlPsw `
		-DryRun $DryRun `
		-ErrorAction stop

	./scripts/install-identity-server.ps1 `
		-ProjectName sensenet-extsql `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SensenetPublicHost https://localhost:8098 `
		-IsHostPort 8099 `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun `
		-ErrorAction stop

	./scripts/install-sensenet-app.ps1 `
		-ProjectName sensenet-extsql `
		-HostName $Hostname `
		-DataSource $DataSource `
		-Routing cnt `
		-AppEnvironment Development `
		-OpenPort $True `
		-SnType "InSql" `
		-SnHostPort 8098 `
		-SensenetPublicHost https://localhost:8098 `
		-IdentityPublicHost https://localhost:8099 `
		-UseDbContainer $UseDbContainer `
		-SqlUser $SqlUSer `
		-SqlPsw $SqlPsw `
		-CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
		-CertPath /root/.aspnet/https/aspnetapp.pfx `
		-CertPass QWEasd123% `
		-DryRun $DryRun `
		-ErrorAction stop
	
	Wait-For-It -Seconds 60	-Message "We are preparing your sensenet repository..." -DryRun $DryRun

	if (-not $DryRun -and $OpenInBrowser) {
		Start-Process "https://admin.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A8098"
	}

	Write-Output "Done."
}
# } catch {
# 	Write-Error "$_"
# }
