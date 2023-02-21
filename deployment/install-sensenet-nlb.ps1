
./scripts/cleanup-sensenet.ps1 `
    -ProjectName sensenet-nlb `
    -WithServices $True

./scripts/install-sensenet-init.ps1
./scripts/install-rabbit.ps1 
./scripts/install-sql-server.ps1 -ProjectName sensenet-nlb

./scripts/install-identity-server.ps1 `
    -ProjectName sensenet-nlb `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SensenetPublicHost https://localhost:8095 `
    -IsHostPort 8096 `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%

./scripts/install-search-service.ps1 `
    -ProjectName sensenet-nlb `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SearchHostPort 8097 `
    -RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
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
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%    