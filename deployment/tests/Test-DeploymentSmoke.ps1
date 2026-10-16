#requires -Version 7.2
[CmdletBinding()]
Param (
    [ValidateSet('WithAuth','LocalAuth','Postgres','Search')][string]$Scenario='WithAuth',
    [int]$TimeoutSeconds=300
)
$ErrorActionPreference = 'Stop'
$deployment = Split-Path -Parent $PSScriptRoot
$file = @{
    WithAuth='docker-compose.with-auth.yml'; LocalAuth='docker-compose.local-auth.yml'
    Postgres='docker-compose.postgres.yml'; Search='docker-compose.with-search-auth.yml'
}[$Scenario]
$port = $(if ($Scenario -eq 'Search') { 51056 } else { 51016 })
$baseUrl = "https://localhost:$port"
$project = 'codex-deployment-smoke-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$temporary = Join-Path ([IO.Path]::GetTempPath()) $project
$variables = @('CERT_FOLDER','LOCAL_AUTH_KEY_FOLDER','CERT_PASSWORD','SQL_PORT','POSTGRES_PORT','RABBIT_PORT')
$previous = @{}
foreach ($variable in $variables) { $previous[$variable] = [Environment]::GetEnvironmentVariable($variable) }
$compose = @('compose','--progress','quiet','--project-name',$project,'-f',"$deployment/$file")
$ownsProject = $False
try {
    $existing = & docker ps -aq --filter "label=com.docker.compose.project=$project"
    if ($LASTEXITCODE -ne 0 -or $existing) { throw 'Could not reserve an isolated smoke-test project.' }
    $null = New-Item -ItemType Directory -Path $temporary
    $env:CERT_FOLDER = $temporary
    $env:LOCAL_AUTH_KEY_FOLDER = $temporary
    $env:CERT_PASSWORD = 'SuP3rS3CuR3P4sSw0Rd'
    $env:SQL_PORT = $(if ($Scenario -eq 'Search') { '51998' } else { '51999' })
    $env:POSTGRES_PORT = '51932'; $env:RABBIT_PORT = '51905'
    # Generate an isolated self-signed certificate without changing the user's trust store.
    $rsa = [Security.Cryptography.RSA]::Create(3072)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=localhost',$rsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        $san.AddDnsName('localhost'); $san.AddIpAddress([Net.IPAddress]::Loopback)
        $request.CertificateExtensions.Add($san.Build())
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-1),[DateTimeOffset]::UtcNow.AddDays(1))
        try { [IO.File]::WriteAllBytes("$temporary/snapp.pfx",$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$env:CERT_PASSWORD)) }
        finally { $certificate.Dispose() }
    } finally { $rsa.Dispose() }
    if ($Scenario -eq 'LocalAuth') { & "$deployment/scripts/create-localauth-key.ps1" -KeyPath "$temporary/signing-key.pem" }
    $ownsProject = $True
    & docker @compose up -d --build
    if ($LASTEXITCODE -ne 0) { throw "Compose startup failed for $Scenario." }
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $ready = $False
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $root = Invoke-RestMethod "$baseUrl/odata.svc/('Root')?`$select=Id,Path,Name" -SkipCertificateCheck -TimeoutSec 5
            if ($root.d.Id -gt 0 -and $root.d.Path -eq '/Root') { $ready = $True; break }
        } catch { Start-Sleep -Seconds 2 }
    }
    if (-not $ready) { throw "Repository OData did not initialize within $TimeoutSeconds seconds for $Scenario." }
    if ($Scenario -eq 'LocalAuth') {
        $body = @{username='admin';password='admin'} | ConvertTo-Json
        $login = Invoke-RestMethod "$baseUrl/authentication/local/login" -Method Post -ContentType application/json -Body $body -SkipCertificateCheck
    } else {
        $body = @{loginName='admin';password='admin';siteUrl=$baseUrl} | ConvertTo-Json
        $login = Invoke-RestMethod "https://localhost:$($port+1)/api/auth/login" -Method Post -ContentType application/json -Body $body -SkipCertificateCheck
    }
    if (-not $login.accessToken) { throw 'Login did not return an access token.' }
    $headers = @{Authorization="Bearer $($login.accessToken)"}
    $admin = Invoke-RestMethod "$baseUrl/odata.svc/Root/IMS/BuiltIn/Portal('Admin')?`$select=Id,Name" -Headers $headers -SkipCertificateCheck
    if ($admin.d.Id -ne 1) { throw 'Authenticated administrator read failed.' }
    $children = Invoke-RestMethod "$baseUrl/odata.svc/Root?`$select=Id,Name&`$top=5" -Headers $headers -SkipCertificateCheck
    Write-Host "PASS: $Scenario initialized OData /Root, admin login and authenticated Admin read (Id=1)."
    # Verify idempotent up and a stopped application restart without changing its database.
    & docker @compose stop -t 5 snapp
    if ($LASTEXITCODE -ne 0) { throw 'Could not stop the isolated application.' }
    & docker @compose up -d --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Repeated Compose up failed.' }
    $ready = $False; $deadline = [DateTime]::UtcNow.AddSeconds(90)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $null = Invoke-RestMethod "$baseUrl/odata.svc/('Root')?`$select=Id" -Headers $headers -SkipCertificateCheck -TimeoutSec 5
            $ready = $True; break
        } catch { Start-Sleep -Seconds 2 }
    }
    if (-not $ready) { throw 'The existing repository did not return after restart.' }
    Write-Host "PASS: $Scenario repeated up and application restart."
} catch {
    # Do not print app logs: login/token paths may log sensitive response details.
    & docker @compose ps -a
    throw
} finally {
    if ($ownsProject) {
        & docker @compose down --volumes --remove-orphans -t 5
        if ($LASTEXITCODE -ne 0) { Write-Warning "Smoke-test resources remain in project $project." }
    }
    foreach ($variable in $variables) { [Environment]::SetEnvironmentVariable($variable,$previous[$variable]) }
    foreach ($name in @('snapp.pfx','signing-key.pem')) {
        $path = Join-Path $temporary $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
}
