
./scripts/cleanup-sensenet.ps1 `
    -ProjectName local-devcert-sql-nlb 

./scripts/install-sensenet-init.ps1
./scripts/install-rabbit.ps1 
./scripts/install-sql-server.ps1 -ProjectName local-devcert-sql-nlb

./scripts/install-identity-server.ps1 `
    -ProjectName local-devcert-sql-nlb `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SensenetPublicHost https://localhost:8066 `
    -IsHostPort 8067 `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%

./scripts/install-search-service.ps1 `
    -ProjectName local-devcert-sql-nlb `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SearchHostPort 8068 `
    -RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%

./scripts/install-sensenet-app.ps1 `
    -ProjectName local-devcert-sql-nlb `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SnHostPort 8066 `
    -SensenetPublicHost https://localhost:8066 `
    -IdentityPublicHost https://localhost:8067 `
    -RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123% `
    -UseGrpc $True