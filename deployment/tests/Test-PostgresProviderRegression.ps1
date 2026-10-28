#requires -Version 7.2
<#
.SYNOPSIS
Verifies a built PostgreSQL application image against a disposable persistent demo.
.EXAMPLE
./Test-PostgresProviderRegression.ps1 -Image local/sn-api-postgre:current
.EXAMPLE
./Test-PostgresProviderRegression.ps1 -Image local/sn-api-postgre:current -BaselineImage local/sn-api-postgre:baseline
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Image,
    [string]$BaselineImage,
    [ValidateRange(30,3600)][int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$deployment = Split-Path -Parent $PSScriptRoot
$run = 'codex-pg-regression-' + [guid]::NewGuid().ToString('N').Substring(0,12)
$temporary = Join-Path ([IO.Path]::GetTempPath()) $run
$owned = $false
$ownsTemporary = $false
$variables = @('CERT_FOLDER','LOCAL_AUTH_KEY_FOLDER','CERT_PASSWORD','COMPOSE_PROJECT_NAME','REPOSITORY_PORT','AUTH_PORT','POSTGRES_PORT','POSTGRES_IMAGE','SQL_PASSWORD','ADMIN_UI_URL','REPOSITORY_API_KEY','JWT_SECRET','AUTH_IMAGE')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
$compose = @('compose','--progress','quiet','--env-file',"$deployment/.env.template",'--project-name',$run,'-f',"$deployment/docker-compose.demo-postgres-volume.yml")
$headers = @{}

function Get-FreePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
    try { $listener.Start(); return $listener.LocalEndpoint.Port }
    finally { $listener.Stop() }
}
function Invoke-Compose([string[]]$Command) {
    & docker @compose @Command
    if ($LASTEXITCODE -ne 0) { throw "Compose command failed: $($Command -join ' ')" }
}
function Read-Content([int]$Id, [switch]$Guids) {
    $select = 'Id,Name,Path,DisplayName,Description'
    if ($Guids) { $select += ',NodeGuidText,VersionGuidText' }
    return (Invoke-RestMethod "$base/odata.svc/content($Id)?`$select=$select" -Headers $headers -SkipCertificateCheck -TimeoutSec 30).d
}
function Wait-Repository {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $root = Invoke-RestMethod "$base/odata.svc/('Root')?`$select=Id,Path" -SkipCertificateCheck -TimeoutSec 5
            if ($root.d.Id -gt 0 -and $root.d.Path -eq '/Root') { return }
        } catch { Start-Sleep -Seconds 2 }
    }
    throw "Repository did not initialize within $TimeoutSeconds seconds."
}
function Connect-Admin {
    $loginUrl = "$auth/api/auth/login"
    $pageUrl = "$auth/login?redirectUrl=$([uri]::EscapeDataString($origin))&callbackUri=%2Fauthentication%2Fcallback"
    $page = Invoke-WebRequest $pageUrl -SkipCertificateCheck -TimeoutSec 30
    if ($page.Content -notmatch 'name="LoginName"') { throw 'SNAuth refused the configured Admin UI redirect Origin.' }
    $preflight = Invoke-WebRequest $loginUrl -Method Options -Headers @{Origin=$origin;'Access-Control-Request-Method'='POST';'Access-Control-Request-Headers'='content-type'} -SkipCertificateCheck -TimeoutSec 30
    if ($origin -notin $preflight.Headers['Access-Control-Allow-Origin']) { throw 'SNAuth preflight did not allow the Admin UI Origin.' }
    $body = @{loginName='admin';password='admin';siteUrl=$base} | ConvertTo-Json -Compress
    $login = Invoke-RestMethod $loginUrl -Method Post -ContentType application/json -Body $body -Headers @{Origin=$origin} -SkipCertificateCheck -TimeoutSec 30 -ResponseHeadersVariable responseHeaders
    if (-not $login.accessToken -or $origin -notin $responseHeaders['Access-Control-Allow-Origin']) { throw 'SNAuth login/token/CORS failed.' }
    $script:headers = @{Authorization="Bearer $($login.accessToken)";Origin=$origin}
    $admin = Invoke-RestMethod "$base/odata.svc/Root/IMS/BuiltIn/Portal('Admin')?`$select=Id,Name" -Headers $headers -SkipCertificateCheck -TimeoutSec 30 -ResponseHeadersVariable responseHeaders
    if ($admin.d.Id -ne 1 -or ($origin -notin $responseHeaders['Access-Control-Allow-Origin'] -and '*' -notin $responseHeaders['Access-Control-Allow-Origin'])) { throw 'Authenticated Admin read/CORS failed.' }
    Write-Host 'PASS: SNAuth form, Admin UI Origin preflight/login and authenticated Admin read.'
}
function Assert-Guid($Value, [string]$Label) {
    $parsed = [guid]::Empty
    if (-not [guid]::TryParse([string]$Value,[ref]$parsed) -or $parsed -eq [guid]::Empty) { throw "$Label is not a nonempty GUID." }
}
function Get-DatabaseGuids([int]$Id) {
    $query = 'SELECT n."RowGuid"::text || ''|'' || v."RowGuid"::text FROM "Nodes" n JOIN "Versions" v ON v."VersionId"=n."LastMinorVersionId" WHERE n."NodeId"=' + $Id
    $lines = & docker @compose exec -T snsql psql -U postgres -d "$run-sndb" -At -v ON_ERROR_STOP=1 -c $query
    if ($LASTEXITCODE -ne 0 -or @($lines).Count -ne 1) { throw "Failed reading GUID rows for content $Id." }
    $parts = ([string]$lines).Split('|')
    if ($parts.Length -ne 2) { throw 'Unexpected database GUID result.' }
    Assert-Guid $parts[0] 'Database node GUID'; Assert-Guid $parts[1] 'Database version GUID'
    return [pscustomobject]@{Node=$parts[0];Version=$parts[1]}
}
function Assert-Identity([int]$Id, $Expected) {
    $read = Read-Content $Id -Guids
    Assert-Guid $read.NodeGuidText 'OData node GUID'; Assert-Guid $read.VersionGuidText 'OData version GUID'
    $database = Get-DatabaseGuids $Id
    if ($read.NodeGuidText -ne $database.Node -or $read.VersionGuidText -ne $database.Version) { throw "OData identifiers differ from persisted row identities for $Id." }
    if ($Expected -and ($read.NodeGuidText -ne $Expected.Node -or $read.VersionGuidText -ne $Expected.Version)) { throw "Row identity changed unexpectedly for $Id." }
    return $database
}
function Test-Index {
    $query = [uri]::EscapeDataString("+InTree:/Root +Name:`"$folderName`" .AUTOFILTERS:OFF")
    for ($attempt=0; $attempt -lt 20; $attempt++) {
        $found = (Invoke-RestMethod "$base/odata.svc/Root?query=$query&`$select=Id" -Headers $headers -SkipCertificateCheck -TimeoutSec 30).d.results
        if (@($found).Count -eq 1 -and $found[0].Id -eq $folder.Id) { return }
        Start-Sleep -Milliseconds 500
    }
    throw 'Indexed query did not find the created folder with its original Id.'
}
function Test-Binary {
    $path = "$base$($folder.Path)/payload.bin"
    $destination = Join-Path $temporary 'download.bin'
    $response = Invoke-WebRequest $path -OutFile $destination -PassThru -Headers $headers -SkipCertificateCheck -TimeoutSec 90
    if ((Get-Item -LiteralPath $destination).Length -ne $bytes.Length -or (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Binary download length/SHA256 differs from uploaded bytes.' }
    if ($origin -notin $response.Headers['Access-Control-Allow-Origin'] -and '*' -notin $response.Headers['Access-Control-Allow-Origin']) { throw 'Binary response did not allow the Admin UI Origin.' }
    Write-Host "PASS: $($bytes.Length)-byte upload/download SHA256 and binary CORS."
}
function Assert-Persisted {
    $read = Read-Content $folder.Id
    if ($read.DisplayName -ne $displayName -or $read.Description -ne 'Updated through OData PATCH') { throw 'Unicode metadata did not persist.' }
    Test-Index; Test-Binary
}
try {
    foreach ($name in $variables) { Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue }
    $targetId = & docker image inspect $Image --format '{{.Id}}'
    if ($LASTEXITCODE -ne 0) { throw 'Build or pull the target image before running this test.' }
    if ($BaselineImage) {
        $baselineId = & docker image inspect $BaselineImage --format '{{.Id}}'
        if ($LASTEXITCODE -ne 0 -or $baselineId -eq $targetId) { throw 'A distinct, locally available baseline image is required for upgrade testing.' }
    }
    $existing = & docker ps -aq --filter "label=com.docker.compose.project=$run"
    if ($LASTEXITCODE -ne 0 -or $existing) { throw 'Could not reserve a new isolated Compose project.' }
    New-Item -ItemType Directory -Path $temporary | Out-Null
    $ownsTemporary = $true
    $env:CERT_FOLDER = $temporary; $env:CERT_PASSWORD = 'DisposableRegressionCertificate'
    $env:COMPOSE_PROJECT_NAME = $run
    $env:REPOSITORY_PORT = [string](Get-FreePort); $env:AUTH_PORT = [string](Get-FreePort); $env:POSTGRES_PORT = [string](Get-FreePort)
    if (@($env:REPOSITORY_PORT,$env:AUTH_PORT,$env:POSTGRES_PORT | Select-Object -Unique).Count -ne 3) { throw 'Could not reserve distinct service ports; rerun.' }
    $base = "https://localhost:$env:REPOSITORY_PORT"; $auth = "https://localhost:$env:AUTH_PORT"; $origin = 'https://admin.example.invalid'
    $env:ADMIN_UI_URL = $origin
    $env:POSTGRES_IMAGE = $(if ($BaselineImage) { $BaselineImage } else { $Image })
    $rsa = [Security.Cryptography.RSA]::Create(3072)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=localhost',$rsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new(); $san.AddDnsName('localhost'); $san.AddIpAddress([Net.IPAddress]::Loopback)
        $request.CertificateExtensions.Add($san.Build())
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-1),[DateTimeOffset]::UtcNow.AddDays(1))
        try { [IO.File]::WriteAllBytes("$temporary/snapp.pfx",$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$env:CERT_PASSWORD)) }
        finally { $certificate.Dispose() }
    } finally { $rsa.Dispose() }
    Write-Host "Testing target $Image ($targetId); baseline=$BaselineImage; isolated project=$run."
    $owned = $true
    Invoke-Compose @('up','-d','--no-build')
    Wait-Repository; Connect-Admin
    $folderName = 'pg-regression-' + [guid]::NewGuid().ToString('N')
    $body = @{__ContentType='SystemFolder';Name=$folderName;DisplayName='Regression folder'} | ConvertTo-Json -Compress
    $folder = (Invoke-RestMethod "$base/odata.svc/Root?`$select=Id,Path" -Method Post -ContentType application/json -Body $body -Headers $headers -SkipCertificateCheck -TimeoutSec 30).d
    if ($folder.Id -le 0 -or $folder.Path -ne "/Root/$folderName") { throw 'Folder creation failed.' }
    $folderUrl = "$base/odata.svc/content($($folder.Id))"
    $folderIdentity = Get-DatabaseGuids $folder.Id
    $displayName = 'PostgreSQL árvíztűrő tükörfúrógép'
    $body = @{DisplayName=$displayName;Description='Updated through OData PATCH'} | ConvertTo-Json -Compress
    $null = Invoke-RestMethod $folderUrl -Method Patch -ContentType application/json -Body $body -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $folderAfterPatch = Get-DatabaseGuids $folder.Id
    if ($folderIdentity.Node -ne $folderAfterPatch.Node -or $folderIdentity.Version -ne $folderAfterPatch.Version) { throw 'Unversioned PATCH changed immutable row GUIDs.' }
    $source = Join-Path $temporary 'payload.bin'
    $bytes = [byte[]]::new(2MB + 137); [Security.Cryptography.RandomNumberGenerator]::Fill($bytes); [IO.File]::WriteAllBytes($source,$bytes)
    $expectedHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $uploaded = Invoke-RestMethod "$folderUrl/Upload" -Method Post -Form @{file=(Get-Item -LiteralPath $source);FileName='payload.bin';FileLength=$bytes.Length.ToString();ContentType='File';PropertyName='Binary';Overwrite='true';UseChunk='false';ChunkToken='0*0*False*False'} -Headers $headers -SkipCertificateCheck -TimeoutSec 90
    if ($uploaded -is [string]) { $uploaded = $uploaded | ConvertFrom-Json }; if ($uploaded.d) { $uploaded = $uploaded.d }
    if ($uploaded.Id -le 0 -or $uploaded.Name -ne 'payload.bin') { throw 'Multipart upload failed.' }
    $fileIdentity = Get-DatabaseGuids $uploaded.Id
    Assert-Persisted
    Write-Host 'PASS: create, Unicode PATCH, indexed search and immutable database identities.'
    if ($BaselineImage) {
        Invoke-Compose @('stop','-t','5','snapp')
        $env:POSTGRES_IMAGE = $Image
        Invoke-Compose @('up','-d','--no-build','--force-recreate')
        Wait-Repository; Connect-Admin; Assert-Persisted
        Write-Host 'PASS: existing baseline database upgraded without losing metadata, index or binary content.'
    }
    $null = Assert-Identity $folder.Id $folderIdentity
    $null = Assert-Identity $uploaded.Id $fileIdentity
    # Read-only computed fields must reject spoofed values while retaining row identity.
    $body = @{NodeGuidText=[guid]::NewGuid().ToString();VersionGuidText=[guid]::NewGuid().ToString();DisplayName=$displayName} | ConvertTo-Json -Compress
    $null = Invoke-RestMethod $folderUrl -Method Patch -ContentType application/json -Body $body -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $null = Assert-Identity $folder.Id $folderIdentity
    # CheckOut copies a version row; CheckIn must retain the copied row's GUID.
    $fileUrl = "$base/odata.svc/content($($uploaded.Id))"
    $null = Invoke-RestMethod $fileUrl -Method Patch -ContentType application/json -Body '{"VersioningMode":"3"}' -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $null = Invoke-RestMethod "$fileUrl/Publish" -Method Post -ContentType application/json -Body '{}' -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $beforeCheckout = Assert-Identity $uploaded.Id
    $null = Invoke-RestMethod "$fileUrl/CheckOut" -Method Post -ContentType application/json -Body '{}' -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $afterCheckout = Assert-Identity $uploaded.Id
    if ($beforeCheckout.Node -ne $afterCheckout.Node -or $beforeCheckout.Version -eq $afterCheckout.Version) { throw 'CheckOut did not preserve node identity and create a fresh version GUID.' }
    $null = Invoke-RestMethod $fileUrl -Method Patch -ContentType application/json -Body '{"Description":"Checked-out version save"}' -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $null = Assert-Identity $uploaded.Id $afterCheckout
    $null = Invoke-RestMethod "$fileUrl/CheckIn" -Method Post -ContentType application/json -Body '{}' -Headers $headers -SkipCertificateCheck -TimeoutSec 30
    $fileIdentity = Assert-Identity $uploaded.Id $afterCheckout
    Write-Host 'PASS: persisted/OData GUID equality, spoofed-field protection and version-copy/save/check-in identity.'
    Invoke-Compose @('up','-d','--no-build','--force-recreate')
    Wait-Repository; Connect-Admin; Assert-Persisted
    $null = Assert-Identity $folder.Id $folderIdentity; $null = Assert-Identity $uploaded.Id $fileIdentity
    Write-Host 'PASS: full stack recreation preserves metadata, index, binary and node/version GUIDs.'
    $null = Invoke-WebRequest "$folderUrl`?permanent=true" -Method Delete -Headers $headers -SkipCertificateCheck -TimeoutSec 60
    foreach ($id in @($folder.Id,$uploaded.Id)) {
        $response = Invoke-WebRequest "$base/odata.svc/content($id)?`$select=Id" -Headers $headers -SkipCertificateCheck -SkipHttpErrorCheck -TimeoutSec 30
        if ($response.StatusCode -ne 404) { throw "Deleted content $id is still accessible: HTTP $($response.StatusCode)." }
    }
    $query = [uri]::EscapeDataString("Name:`"$folderName`" .AUTOFILTERS:OFF")
    $found = (Invoke-RestMethod "$base/odata.svc/Root?query=$query&`$select=Id" -Headers $headers -SkipCertificateCheck -TimeoutSec 30).d.results
    if (@($found).Count -ne 0) { throw 'Deleted content remains in indexed search.' }
    Write-Host 'PASS: recursive permanent deletion removes metadata, children and index results.'
} catch {
    Write-Warning "PostgreSQL regression failed: $($_.Exception.Message)"
    if ($owned) {
        & docker @compose ps -a
        # Keep the failure visible after the disposable stack is removed. Do not
        # emit JWTs, bearer values, or the demo API key if a dependency logs them.
        $diagnostic = & docker @compose logs --no-color --tail 100 snapp snis snsql 2>&1 | Out-String
        $diagnostic = [regex]::Replace($diagnostic,'(?i)Bearer\s+[^\s"'']+','Bearer [redacted]')
        $diagnostic = [regex]::Replace($diagnostic,'eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+','[redacted JWT]')
        $diagnostic = $diagnostic.Replace('pr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Ted','[redacted demo API key]')
        Write-Host $diagnostic
    }
    throw
} finally {
    if ($owned) {
        & docker @compose down --volumes --remove-orphans -t 5
        if ($LASTEXITCODE -ne 0) { Write-Warning "Test resources remain for isolated project $run." }
    }
    foreach ($name in $variables) {
        if ($null -eq $previous[$name]) { Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($name,$previous[$name]) }
    }
    $temporaryPath = [IO.Path]::GetFullPath($temporary)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $temporaryPath.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $temporaryPath) -ne $run -or $run -notlike 'codex-pg-regression-*') { throw 'Refusing cleanup outside this test temporary directory.' }
    if ($ownsTemporary -and (Test-Path -LiteralPath $temporaryPath)) { Remove-Item -LiteralPath $temporaryPath -Recurse -Force }
}
