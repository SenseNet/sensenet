
./scripts/cleanup-sensenet.ps1 `
    -ProjectName sensenet-insql

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
    -RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
    -CertFolder $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./certificates") `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%
