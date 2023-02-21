
./scripts/cleanup-sensenet.ps1 `
    -ProjectName sensenet-inmem

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