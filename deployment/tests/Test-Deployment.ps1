#requires -Version 7.2
[CmdletBinding()]
Param ()
$ErrorActionPreference = 'Stop'
$deployment = Split-Path -Parent $PSScriptRoot
$checks = 0
function Assert($condition, [string]$message) {
    if (-not $condition) { throw $message }
    $script:checks++
}

# Detect parameter-binding failures before executing any external tools.
Get-ChildItem $deployment -Recurse -Filter '*.ps1' | ForEach-Object {
    $tokens = $null; $parseErrors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$parseErrors)
    Assert ($parseErrors.Count -eq 0) "$($_.Name): $($parseErrors.Message -join '; ')"
}
. "$deployment/scripts/helper-functions.ps1"

# These names must agree with the Dockerfile/image mapping in the CI workflow.
$images = @(
    @('InMem','Standard',$False,'sensenetcsp/sn-api-inmem:preview'),
    @('InSql','Standard',$False,'sensenetcsp/sn-api-sql:preview'),
    @('InSql','Standard',$True,'sensenetcsp/sn-api-nlb:preview'),
    @('InSql','Preview',$False,'sensenetcsp/sn-api-sql-prv:preview'),
    @('InSql','Preview',$True,'sensenetcsp/sn-api-nlb-prv:preview'),
    @('InSql','LocalAuth',$False,'local/sn-api-sql-localauth:demo'),
    @('InPostgres','PostgreSql',$False,'sensenetcsp/sn-api-postgre:postgres-provider')
)
foreach ($image in $images) {
    $selection = Get-SensenetImage -SnType $image[0] -WebApp $image[1] -SearchService $image[2]
    Assert ($selection.Image -eq $image[3]) "Wrong image for $($image[0..2] -join ',')"
}

# SNAuth COPY src/sn-auth requires the repository root, and existing PowerShell
# SQL demos must keep their original data-directory mount.
$buildOutput = & "$deployment/scripts/create-images.ps1" -ImageType Auth -LocalSn $True -DryRun $True -Verbose 4>&1
$buildCommand = @($buildOutput | Where-Object { $_ -is [Management.Automation.VerboseRecord] -and $_.Message -like 'docker build *' })
Assert ($buildCommand.Count -eq 1 -and $buildCommand[0].Message.TrimEnd().EndsWith('/sn-auth')) 'SNAuth build context must be its repository root.'
$sqlOutput = & "$deployment/scripts/install-sql-server.ps1" -UseVolume $True -SqlUser demo -SqlPsw demo -DryRun $True -Verbose 4>&1
$sqlCommand = @($sqlOutput | Where-Object { $_ -is [Management.Automation.VerboseRecord] -and $_.Message -like 'docker run -d *' })
Assert ($sqlCommand.Count -eq 1 -and $sqlCommand[0].Message.Contains(':/var/opt/mssql/data')) 'Existing SQL data mount layout changed.'

# Exercise the lifecycle contract without touching the Docker daemon.
$script:dockerCalls = [Collections.Generic.List[string]]::new()
function docker {
    $script:dockerCalls.Add($args -join ' ')
    if ($args[0] -eq 'container') {
        $global:LASTEXITCODE = $(if ($script:containerState -eq 'missing') { 1 } else { 0 })
        if ($global:LASTEXITCODE -eq 0) { $script:containerState }
    } else {
        $global:LASTEXITCODE = $(if ($script:failOperation) { 1 } else { 0 })
    }
}
foreach ($case in @(@('missing','created'),@('created','started'),@('exited','started'),@('running','running'))) {
    $script:containerState = $case[0]; $script:failOperation = $False
    $script:dockerCalls.Clear()
    $result = Manage-Container -ContainerName demo -DockerRunParams @('run','--name','demo','example')
    Assert ($result -is [string] -and $result -eq $case[1]) "Lifecycle returned a polluted result for $($case[0])."
    Assert (-not ($script:dockerCalls -match '^rm ')) 'Lifecycle must not delete an existing container.'
}
$script:containerState = 'paused'; $rejected = $False
try { $null = Manage-Container demo @('run','example') } catch { $rejected = $True }
Assert $rejected 'Paused containers must require explicit operator action.'
$script:containerState = 'exited'; $script:failOperation = $True; $rejected = $False
try { $null = Manage-Container demo @('run','example') } catch { $rejected = $True }
Assert $rejected 'A failed container start must terminate the installer.'

# A complete DryRun must not invoke docker/dotnet/git, even in the search restart path.
function docker { throw 'DryRun invoked docker.' }
function dotnet { throw 'DryRun invoked dotnet.' }
function git { throw 'DryRun invoked git.' }
foreach ($parameters in @(
    @{}, @{UseAuth=$True}, @{InMemPlatform=$True}, @{CreateImages=$True;UseAuth=$True},
    @{UseVolume=$True;SearchService=$True;UseAuth=$True},
    @{HostDb=$True;SqlUser='demo';SqlPsw='demo';SearchService=$True},
    @{WebApp='Preview';CreateImages=$True;LocalSn=$True;SearchService=$True;UseAuth=$True},
    @{WebApp='LocalAuth';CreateImages=$True;LocalSn=$True},
    @{WebApp='PostgreSql';UseVolume=$True}
)) {
    & "$deployment/install-sensenet.ps1" @parameters -DryRun -Verbose *>&1 | Out-Null
    Assert $True 'DryRun completed.'
}
foreach ($parameters in @(
    @{WebApp='LocalAuth';SearchService=$True}, @{WebApp='LocalAuth';UseAuth=$True},
    @{WebApp='PostgreSql';HostDb=$True}, @{WebApp='Preview';InMemPlatform=$True}
)) {
    $rejected = $False
    try { & "$deployment/install-sensenet.ps1" @parameters -DryRun *>&1 | Out-Null } catch { $rejected = $True }
    Assert $rejected 'An unsupported webapp combination was accepted.'
}
Remove-Item Function:docker,Function:dotnet,Function:git

# HTTP 200 for an empty collection is not proof that a repository is installed.
$script:probeCalls = 0; $script:probeReady = $False
function Invoke-WebRequest {
    $script:probeCalls++
    $content = $(if ($script:probeReady) { '{"d":{"Id":2,"Path":"/Root"}}' } else { '{"d":{"results":[]}}' })
    [pscustomobject]@{StatusCode=200;Content=$content}
}
function Wait-For-It { Param($Seconds,$Silent,$DryRun) }
$rejected = $False
try { Wait-SnApp -SnHostPort 51916 -MaxTryNumber 3 } catch { $rejected = $True }
Assert $rejected 'An empty OData collection was accepted as an initialized repository.'
Assert ($script:probeCalls -eq 3) 'Repository readiness did not respect its retry limit.'
$script:probeReady = $True; $script:probeCalls = 0
Wait-SnApp -SnHostPort 51916 -MaxTryNumber 3
Assert ($script:probeCalls -eq 1) 'An initialized repository was not accepted.'
Remove-Item Function:Invoke-WebRequest
. "$deployment/scripts/helper-functions.ps1"

# Compose must resolve every dependency/resource and retain the legacy port matrix.
$expectedPorts = @{
    ''=51016; 'with-auth'=51016; 'with-volume'=51036; 'with-volume-auth'=51036
    'with-search'=51056; 'with-search-auth'=51056; 'complete'=51076; 'complete-auth'=51076
    'host-db'=51026; 'host-db-auth'=51026; 'host-db-volume'=51046; 'host-db-volume-auth'=51046
    'host-db-search'=51066; 'host-db-search-auth'=51066; 'host-db-volume-search'=51086; 'host-db-volume-search-auth'=51086
    'in-memory'=51011; 'in-memory-auth'=51011
    'local-auth'=51016; 'local-auth-volume'=51036; 'local-auth-host-db'=51026; 'local-auth-host-db-volume'=51046
    'preview'=51016; 'preview-search'=51056; 'postgres'=51016; 'postgres-volume'=51036
}
foreach ($file in Get-ChildItem $deployment -Filter 'docker-compose*.yml') {
    $json = & docker compose -f $file.FullName config --format json
    Assert ($LASTEXITCODE -eq 0) "Compose failed: $($file.Name)"
    $config = ($json -join "`n") | ConvertFrom-Json
    $key = $file.BaseName -replace '^docker-compose\.?',''
    Assert ($expectedPorts.ContainsKey($key)) "Unaccounted scenario: $($file.Name)"
    $port = [int]$config.services.snapp.ports[0].published
    Assert ($port -eq $expectedPorts[$key]) "Legacy repository port changed in $($file.Name)"
    if ($config.services.snis) {
        Assert ([int]$config.services.snis.ports[0].published -eq $port+1) "Wrong auth port in $($file.Name)"
    }
    foreach ($service in $config.services.PSObject.Properties.Value) {
        foreach ($binding in $service.ports) { Assert ($binding.host_ip -eq '127.0.0.1') "Non-local demo port in $($file.Name)" }
    }
    if ($config.services.snsearch) {
        Assert ($config.services.snapp.image -like '*sn-api-nlb*') "Search scenario uses an in-process API in $($file.Name)"
        Assert ($config.services.snapp.environment.sensenet__search__service__ServiceAddress -eq 'https://snsearch') 'Missing search address.'
        Assert ($null -ne $config.services.snsearch.environment.ConnectionStrings__SecurityStorage) 'Missing search security database.'
    }
}
Write-Host "PASS: $checks deployment assertions (PowerShell syntax, lifecycle, DryRun, image selection and 26 Compose scenarios)."
