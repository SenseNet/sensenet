
./scripts/cleanup-sensenet.ps1 `
    -ProjectName local-devcert-sql-new

./scripts/install-sensenet-init.ps1
./scripts/install-rabbit.ps1 

./scripts/install-sql-server.ps1 `
    -ProjectName local-devcert-sql-new

./scripts/install-identity-server.ps1 `
    -ProjectName local-devcert-sql-new `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SensenetPublicHost https://localhost:8091 `
    -IsHostPort 8092 `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%

./scripts/install-sensenet-app.ps1 `
    -ProjectName local-devcert-sql-new `
    -Routing cnt `
    -AppEnvironment Development `
    -OpenPort $True `
    -SnHostPort 8091 `
    -SensenetPublicHost https://localhost:8091 `
    -IdentityPublicHost https://localhost:8092 `
    -RabbitServiceHost amqp://admin:QWEasd123%@sn-rabbit/ `
    -CertFolder $env:USERPROFILE\.aspnet\https\ `
    -CertPath /root/.aspnet/https/aspnetapp.pfx `
    -CertPass QWEasd123%