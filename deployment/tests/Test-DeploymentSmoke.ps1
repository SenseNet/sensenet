#requires -Version 7.2
[CmdletBinding()]
Param (
    [ValidateSet('WithAuth','LocalAuth','Postgres','Search','InMemory','Parallel')][string]$Scenario='WithAuth',
    [switch]$LegacyPorts,
    [int]$TimeoutSeconds=300
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $False
if ($Scenario -eq 'Postgres') { throw 'The PostgreSQL demo is inactive until its provider is integrated into develop.' }
if ($LegacyPorts -and $Scenario -eq 'Parallel') { throw 'Legacy LocalAuth/SNAuth demos share ports. Test parallel demos with default ports.' }
$deployment = Split-Path -Parent $PSScriptRoot
$files = @{
    WithAuth='docker-compose.with-auth.yml'; LocalAuth='docker-compose.local-auth.yml'
    Search='docker-compose.with-search-auth.yml'
    InMemory='docker-compose.in-memory-auth.yml'
}
$selected = $(if ($Scenario -eq 'Parallel') { @('LocalAuth','WithAuth') } else { @($Scenario) })
$run = 'codex-deployment-smoke-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$temporary = Join-Path ([IO.Path]::GetTempPath()) $run
$variables = @('CERT_FOLDER','LOCAL_AUTH_KEY_FOLDER','CERT_PASSWORD','COMPOSE_PROJECT_NAME','REPOSITORY_PORT','AUTH_PORT','SEARCH_PORT','SQL_PORT','POSTGRES_PORT','RABBIT_PORT')
$variables += @(Get-ChildItem Env: | Where-Object Name -like 'LEGACY_*_PORT' | ForEach-Object Name)
$previous = @{}
$demos = @()
$ownsTemporary = $False
function Wait-Repository($demo, [int]$timeout) {
    $deadline = [DateTime]::UtcNow.AddSeconds($timeout)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $root = Invoke-RestMethod "$($demo.Url)/odata.svc/('Root')?`$select=Id,Path" -SkipCertificateCheck -TimeoutSec 5
            if ($root.d.Id -gt 0 -and $root.d.Path -eq '/Root') { return }
        } catch { Start-Sleep -Seconds 2 }
    }
    throw "$($demo.Scenario) did not initialize OData within $timeout seconds."
}
function Test-AdminRead($demo) {
    $admin = Invoke-RestMethod "$($demo.Url)/odata.svc/Root/IMS/BuiltIn/Portal('Admin')?`$select=Id,Name" -Headers $demo.Headers -SkipCertificateCheck -TimeoutSec 15
    if ($admin.d.Id -ne 1) { throw "Authenticated Admin read failed for $($demo.Scenario)." }
}
function Test-Login($demo) {
    if ($demo.Scenario -eq 'LocalAuth') {
        $body = @{username='admin';password='admin'} | ConvertTo-Json
        $loginUrl = "$($demo.Url)/authentication/local/login"
    } else {
        $body = @{loginName='admin';password='admin';siteUrl=$demo.Url} | ConvertTo-Json
        $loginUrl = "https://localhost:$($demo.Config.services.snis.ports[0].published)/api/auth/login"
    }
    $login = Invoke-RestMethod $loginUrl -Method Post -ContentType application/json -Body $body -SkipCertificateCheck -TimeoutSec 30
    if (-not $login.accessToken) { throw "$($demo.Scenario) login returned no access token." }
    $demo.Headers = @{Authorization="Bearer $($login.accessToken)"}
    Test-AdminRead $demo
    Write-Host "PASS: $($demo.Scenario) OData, admin login and authenticated Admin read at $($demo.Url)."
}
try {
    foreach ($variable in $variables) {
        $previous[$variable] = [Environment]::GetEnvironmentVariable($variable)
        Remove-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue
    }
    $null = New-Item -ItemType Directory -Path $temporary
    $ownsTemporary = $True
    $env:CERT_FOLDER = $temporary
    $env:LOCAL_AUTH_KEY_FOLDER = $temporary
    $env:CERT_PASSWORD = 'SuP3rS3CuR3P4sSw0Rd'
    # The test certificate never changes the user's trust store.
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
    foreach ($selection in $selected) {
        $project = "$run-$($selection.ToLowerInvariant())"
        $arguments = @('compose','--progress','quiet','--env-file',"$deployment/.env.template")
        if ($LegacyPorts) { $arguments += @('--env-file',"$deployment/legacy-ports.env") }
        $arguments += @('--project-name',$project,'-f',"$deployment/$($files[$selection])")
        $existing = & docker ps -aq --filter "label=com.docker.compose.project=$project"
        if ($LASTEXITCODE -ne 0 -or $existing) { throw 'Could not reserve an isolated smoke-test project.' }
        $json = & docker @arguments config --format json
        if ($LASTEXITCODE -ne 0) { throw "Compose config failed for $selection." }
        $config = ($json -join "`n") | ConvertFrom-Json
        $demos += [pscustomobject]@{Scenario=$selection;Arguments=$arguments;Config=$config;Url="https://localhost:$($config.services.snapp.ports[0].published)";Headers=@{};Owned=$False}
    }
    $local = $demos | Where-Object Scenario -eq LocalAuth
    if ($local) {
        & docker @($local.Arguments) build snapp
        if ($LASTEXITCODE -ne 0) { throw 'LocalAuth build failed.' }
        # An existing empty directory must not hide a missing required file.
        foreach ($missing in @(@('LOCAL_AUTH_KEY_FOLDER','missing-key','signing-key.pem'),@('CERT_FOLDER','missing-cert','snapp.pfx'))) {
            $folder = Join-Path $temporary $missing[1]
            $null = New-Item -ItemType Directory -Path $folder
            [Environment]::SetEnvironmentVariable($missing[0],$folder)
            $local.Owned = $True
            $output = (& docker @($local.Arguments) up -d --no-build 2>&1 | Out-String)
            if ($LASTEXITCODE -eq 0) { throw "Compose accepted the missing $($missing[2]) file." }
            $diagnostic = (& docker @($local.Arguments) logs --no-color prerequisites 2>&1 | Out-String)
            if ($diagnostic -notmatch [regex]::Escape($missing[2])) { throw "Missing-file check failed without a useful diagnostic: $output $diagnostic" }
            $status = & docker inspect $local.Config.services.snapp.container_name --format '{{.State.Status}}'
            if ($LASTEXITCODE -ne 0 -or $status -ne 'created') { throw 'Application must remain unstarted when a required demo file is missing.' }
            & docker @($local.Arguments) down --volumes --remove-orphans -t 5
            if ($LASTEXITCODE -ne 0) { throw 'Missing-file test cleanup failed.' }
            [Environment]::SetEnvironmentVariable($missing[0],$temporary)
            if ($missing[0] -eq 'LOCAL_AUTH_KEY_FOLDER') { & "$deployment/scripts/create-localauth-key.ps1" -KeyPath "$temporary/signing-key.pem" }
            Write-Host "PASS: Compose rejects missing $($missing[2]) even when its directory exists."
        }
    }
    # Start both projects before waiting: Parallel uses the default ports.
    foreach ($demo in $demos) {
        $demo.Owned = $True
        Write-Host "Starting $($demo.Scenario) at $($demo.Url)."
        & docker @($demo.Arguments) up -d --build
        if ($LASTEXITCODE -ne 0) { throw "Compose startup failed for $($demo.Scenario)." }
    }
    foreach ($demo in $demos) { Wait-Repository $demo $TimeoutSeconds; Test-Login $demo }
    foreach ($demo in $demos) {
        $others = @($demos | Where-Object { $_ -ne $demo })
        $started = @{}
        foreach ($other in $others) {
            $container = $other.Config.services.snapp.container_name
            $started[$container] = & docker inspect $container --format '{{.State.StartedAt}}'
            if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the other demo.' }
        }
        & docker @($demo.Arguments) stop -t 5 snapp
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop the isolated application.' }
        foreach ($other in $others) { Test-AdminRead $other }
        & docker @($demo.Arguments) up -d --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Repeated Compose up failed.' }
        Wait-Repository $demo 90
        Test-AdminRead $demo
        foreach ($other in $others) {
            Test-AdminRead $other
            $container = $other.Config.services.snapp.container_name
            $now = & docker inspect $container --format '{{.State.StartedAt}}'
            if ($LASTEXITCODE -ne 0 -or $now -ne $started[$container]) { throw 'Restarting one demo restarted the other.' }
        }
        Write-Host "PASS: $($demo.Scenario) repeated up/restart preserves its authenticated access and leaves other demos running."
    }
} catch {
    # Do not print application logs: login/token paths may log sensitive details.
    Write-Warning "Smoke test failed: $($_.Exception.Message)"
    foreach ($demo in $demos | Where-Object Owned) { & docker @($demo.Arguments) ps -a }
    throw
} finally {
    foreach ($demo in $demos | Where-Object Owned) {
        & docker @($demo.Arguments) down --volumes --remove-orphans -t 5
        if ($LASTEXITCODE -ne 0) { Write-Warning "Smoke-test resources remain for $($demo.Scenario)." }
    }
    foreach ($variable in $variables) {
        if ($null -eq $previous[$variable]) { Remove-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($variable,$previous[$variable]) }
    }
    # Docker Desktop may create an empty directory at a missing bind-file path.
    # Delete only this run's verified temporary subtree, including such artifacts.
    $temporaryPath = [IO.Path]::GetFullPath($temporary)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $temporaryPath.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $temporaryPath) -ne $run -or $run -notlike 'codex-deployment-smoke-*') {
        throw 'Refusing cleanup outside this smoke test temporary directory.'
    }
    if ($ownsTemporary -and (Test-Path -LiteralPath $temporaryPath)) { Remove-Item -LiteralPath $temporaryPath -Recurse -Force }
}
