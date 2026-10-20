#requires -Version 7.2
[CmdletBinding()]
Param ()
$ErrorActionPreference = 'Stop'
$deployment = Split-Path -Parent $PSScriptRoot
$supportsPostgres = Test-Path "$deployment/../src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile" -PathType Leaf
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

# Source builds must produce the exact dependency image the installer will run.
foreach ($dependency in @(@('Is','install-identity-server.ps1'),@('Auth','install-snauth.ps1'),@('Search','install-search-service.ps1'))) {
    $buildOutput = & "$deployment/scripts/create-images.ps1" -ImageType $dependency[0] -LocalSn $True -DryRun $True -Verbose 4>&1
    $buildCommand = @($buildOutput | Where-Object { $_ -is [Management.Automation.VerboseRecord] -and $_.Message -like 'docker build *' })
    $builtImage = [regex]::Match($buildCommand[0].Message,' -t (\S+)').Groups[1].Value
    $runOutput = & "$deployment/scripts/$($dependency[1])" -SkipPull $True -DryRun $True -Verbose 4>&1
    $runCommand = @($runOutput | Where-Object { $_ -is [Management.Automation.VerboseRecord] -and $_.Message -like 'docker run *' })
    $runImage = ($runCommand[0].Message.Trim() -split '\s+')[-1]
    Assert ($builtImage -and $builtImage -eq $runImage) "$($dependency[0]) build/run image tags do not agree."
}

# Compose networks must not satisfy the PowerShell installer's exact network name.
$global:deploymentTestNetwork = [pscustomobject]@{
    Calls = [Collections.Generic.List[string]]::new()
    Exists = $False
    FailCreate = $False
}
function docker {
    $arguments = @($args | ForEach-Object { $_ })
    $global:deploymentTestNetwork.Calls.Add($arguments -join ' ')
    $global:LASTEXITCODE = 0
    if ($arguments[0] -eq 'info') { return '{"ServerErrors":[]}' }
    if ($arguments[0] -ne 'network') { throw 'Unexpected Docker command in network test.' }
    switch ($arguments[1]) {
        'list' { 'sensenet-demo_sensenet' }
        'inspect' {
            if ($global:deploymentTestNetwork.Exists -and $arguments[-1] -eq 'sensenet') { 'sensenet' }
            else { $global:LASTEXITCODE = 1 }
        }
        'create' {
            if ($global:deploymentTestNetwork.FailCreate) { $global:LASTEXITCODE = 1 }
            else { $global:deploymentTestNetwork.Exists = $True; 'test-network-id' }
        }
        default { throw 'Unexpected network operation.' }
    }
}
& "$deployment/scripts/install-sensenet-init.ps1" -NetworkName sensenet | Out-Null
Assert ($global:deploymentTestNetwork.Exists) 'Compose networks prevented creation of the required sensenet network.'
Assert (@($global:deploymentTestNetwork.Calls -match '^network create ').Count -eq 1) 'Missing network must be created exactly once.'
& "$deployment/scripts/install-sensenet-init.ps1" -NetworkName sensenet | Out-Null
Assert (@($global:deploymentTestNetwork.Calls -match '^network create ').Count -eq 1) 'Repeated initialization must reuse the exact network.'
$global:deploymentTestNetwork.Exists = $False; $global:deploymentTestNetwork.FailCreate = $True; $rejected = $False
try { & "$deployment/scripts/install-sensenet-init.ps1" -NetworkName sensenet | Out-Null } catch { $rejected = $True }
Assert $rejected 'Failed network creation must terminate the installer.'
Remove-Variable -Name deploymentTestNetwork -Scope Global

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
    @{WebApp='LocalAuth';CreateImages=$True;LocalSn=$True}
)) {
    $dryRunOutput = & "$deployment/install-sensenet.ps1" @parameters -DryRun -Verbose *>&1 | Out-String
    Assert $True 'DryRun completed.'
    if (-not $parameters.InMemPlatform) {
        Assert ($dryRunOutput.Contains('sensenet__install__mssql__EnableFirstInstallDB=true')) 'SQL demo installer must enable initial schema installation.'
    }
}
foreach ($parameters in @(
    @{WebApp='LocalAuth';SearchService=$True}, @{WebApp='LocalAuth';UseAuth=$True},
    @{WebApp='PostgreSql';HostDb=$True}, @{WebApp='Preview';InMemPlatform=$True}
)) {
    $rejected = $False
    try { & "$deployment/install-sensenet.ps1" @parameters -DryRun *>&1 | Out-Null } catch { $rejected = $True }
    Assert $rejected 'An unsupported webapp combination was accepted.'
}
foreach ($parameters in @(@{}, @{CreateImages=$True}, @{CreateImages=$True;LocalSn=$True})) {
    if ($supportsPostgres) {
        & "$deployment/install-sensenet.ps1" -WebApp PostgreSql @parameters -DryRun *>&1 | Out-Null
        Assert $True 'PostgreSQL installer DryRun completed without external tools.'
        continue
    }
    $rejected = $False
    try { & "$deployment/install-sensenet.ps1" -WebApp PostgreSql @parameters -DryRun *>&1 | Out-Null }
    catch { $rejected = $_.Exception.Message -like '*PostgreSQL demo is inactive*' }
    Assert $rejected 'The inactive PostgreSQL installer must fail before using external tools.'
}
foreach ($parameters in @(@{ImageType='InPostgres'}, @{WebApp='PostgreSql'})) {
    if ($supportsPostgres) {
        & "$deployment/scripts/create-images.ps1" @parameters -DryRun $True *>&1 | Out-Null
        Assert $True 'PostgreSQL image build DryRun completed without external tools.'
        continue
    }
    $rejected = $False
    try { & "$deployment/scripts/create-images.ps1" @parameters -DryRun $True *>&1 | Out-Null }
    catch { $rejected = $_.Exception.Message -like '*PostgreSQL demo is inactive*' }
    Assert $rejected 'The inactive PostgreSQL build must not fetch the provider branch.'
}
if (-not $supportsPostgres) {
    $rejected = $False
    try { & "$deployment/tests/Test-DeploymentSmoke.ps1" -Scenario Postgres *>&1 | Out-Null }
    catch { $rejected = $_.Exception.Message -like '*PostgreSQL demo is inactive*' }
    Assert $rejected 'The inactive PostgreSQL smoke scenario must not start containers.'
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

# Historical ports remain available only when explicitly requested.
$legacyPorts = @{
    ''=51016; 'with-auth'=51016; 'with-volume'=51036; 'with-volume-auth'=51036
    'with-search'=51056; 'with-search-auth'=51056; 'complete'=51076; 'complete-auth'=51076
    'host-db'=51026; 'host-db-auth'=51026; 'host-db-volume'=51046; 'host-db-volume-auth'=51046
    'host-db-search'=51066; 'host-db-search-auth'=51066; 'host-db-volume-search'=51086; 'host-db-volume-search-auth'=51086
    'in-memory'=51011; 'in-memory-auth'=51011
    'local-auth'=51016; 'local-auth-volume'=51036; 'local-auth-host-db'=51026; 'local-auth-host-db-volume'=51046
    'preview'=51016; 'preview-search'=51056
}
$defaultPorts = @{
    ''=18016; 'with-auth'=18026; 'with-volume'=18036; 'with-volume-auth'=18046
    'with-search'=18056; 'with-search-auth'=18066; 'complete'=18076; 'complete-auth'=18086
    'host-db'=18096; 'host-db-auth'=18106; 'host-db-volume'=18116; 'host-db-volume-auth'=18126
    'host-db-search'=18136; 'host-db-search-auth'=18146; 'host-db-volume-search'=18156; 'host-db-volume-search-auth'=18166
    'in-memory'=18176; 'in-memory-auth'=18186; 'preview'=18196; 'preview-search'=18206
    'local-auth'=18216; 'local-auth-volume'=18226; 'local-auth-host-db'=18236; 'local-auth-host-db-volume'=18246
}
if ($supportsPostgres) {
    $legacyPorts['demo-postgres'] = 51016; $legacyPorts['demo-postgres-volume'] = 51036
    $defaultPorts['demo-postgres'] = 18256; $defaultPorts['demo-postgres-volume'] = 18266
}
function Assert-Endpoints($config, [int]$port, [string]$scenario) {
    Assert ($config.services.prerequisites.network_mode -eq 'none') "Required-file check should not use a network in $scenario"
    if ($config.services.snapp.environment.ConnectionStrings__SnCrMsSql) {
        Assert ($config.services.snapp.environment.sensenet__install__mssql__EnableFirstInstallDB -eq 'true') "SQL demo cannot initialize an empty database in $scenario"
    }
    $repositoryUrl = "https://localhost:$port"
    Assert ($config.services.snapp.environment.sensenet__authentication__repositoryUrl -eq $repositoryUrl) "Repository URL disagrees with port in $scenario"
    if ($config.services.snapp.environment.sensenet__authentication__Local__Issuer) {
        Assert ($config.services.snapp.environment.sensenet__authentication__Local__Issuer -eq $repositoryUrl) "LocalAuth issuer disagrees with port in $scenario"
    }
    if ($config.services.snis) {
        $authUrl = "https://localhost:$($config.services.snis.ports[0].published)"
        Assert ($config.services.snapp.environment.sensenet__authentication__authority -eq $authUrl) "Auth authority disagrees with port in $scenario"
        $auth = $config.services.snis.environment
        if ($auth.JwtSettings__Issuer) {
            Assert ($auth.JwtSettings__Issuer -eq $authUrl -and $auth.Application__Url -eq $authUrl) "SNAuth issuer/URL disagrees with port in $scenario"
            Assert ($auth.Sensenet__Repository__Url -eq $repositoryUrl -and $auth.Application__AllowedHosts__1 -eq $repositoryUrl) "SNAuth repository/CORS URL disagrees with port in $scenario"
        } else {
            Assert ($auth.IdentityServer__IssuerUri -eq $authUrl) "IdentityServer issuer disagrees with port in $scenario"
            foreach ($client in @('adminui','spa','client')) {
                Assert ($auth."sensenet__Clients__${client}__RepositoryHosts__0__PublicHost" -eq $repositoryUrl) "IdentityServer $client URL disagrees with port in $scenario"
                Assert ($auth."sensenet__Clients__${client}__RepositoryHosts__1__PublicHost" -eq "https://127.0.0.1:$port") "IdentityServer $client rejects the IPv4 loopback repository URL in $scenario"
                Assert ($auth."sensenet__Clients__${client}__RepositoryHosts__1__InternalHost" -eq 'http://snapp') "IdentityServer $client lacks internal routing for the IPv4 loopback alias in $scenario"
            }
        }
    }
    foreach ($entry in $config.services.PSObject.Properties) {
        $service = $entry.Value
        if ($entry.Name -ne 'prerequisites') {
            Assert ($service.depends_on.prerequisites.condition -eq 'service_completed_successfully') "Service $($entry.Name) can start before required-file validation in $scenario"
        }
        foreach ($binding in $service.ports) { Assert ($binding.host_ip -eq '127.0.0.1') "Non-local demo port in $scenario" }
        foreach ($mount in $service.volumes | Where-Object type -eq bind) {
            Assert ($mount.read_only -and -not $mount.bind.create_host_path) "Missing required read-only file bind in $scenario"
            Assert ($mount.source -match '\.(pfx|pem)$' -and $mount.target -match '\.(pfx|pem)$') "Bind must require the cert/key file, not an empty directory, in $scenario"
        }
    }
    $requiresKey = $config.services.prerequisites.environment.REQUIRE_LOCAL_AUTH_KEY -eq 'true'
    Assert ($requiresKey -eq [bool]$config.services.snapp.environment.sensenet__authentication__Local__SigningKeyPath) "Wrong LocalAuth key requirement in $scenario"
    if ($config.services.snsearch) {
        Assert ($config.services.snapp.image -like '*sn-api-nlb*') "Search scenario uses an in-process API in $scenario"
        Assert ($config.services.snapp.environment.sensenet__search__service__ServiceAddress -eq 'https://snsearch') 'Missing search address.'
        Assert ($null -ne $config.services.snsearch.environment.ConnectionStrings__SecurityStorage) 'Missing search security database.'
    }
}
function Get-ComposeConfig([string]$file, [switch]$Legacy) {
    $arguments = @('compose','--env-file',"$deployment/.env.template")
    if ($Legacy) { $arguments += @('--env-file',"$deployment/legacy-ports.env") }
    $json = & docker @arguments -f $file config --format json
    Assert ($LASTEXITCODE -eq 0) "Compose failed: $file"
    ($json -join "`n") | ConvertFrom-Json
}
# Isolate configuration tests from the caller's port overrides and private .env.
$portVariables = @('COMPOSE_PROJECT_NAME','REPOSITORY_PORT','AUTH_PORT','SEARCH_PORT','SQL_PORT','POSTGRES_PORT','RABBIT_PORT')
$portVariables += @(Get-ChildItem Env: | Where-Object Name -like 'LEGACY_*_PORT' | ForEach-Object Name)
$previous = @{}
foreach ($variable in $portVariables) {
    $previous[$variable] = [Environment]::GetEnvironmentVariable($variable)
    Remove-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue
}
try {
    $projects = [Collections.Generic.HashSet[string]]::new()
    $containers = [Collections.Generic.HashSet[string]]::new()
    $bindings = [Collections.Generic.HashSet[string]]::new()
    # Test this demo suite explicitly; other development Compose files may coexist.
    $files = @($defaultPorts.Keys | Sort-Object | ForEach-Object {
        $name = $(if ($_ -eq '') { 'docker-compose.yml' } else { "docker-compose.$_.yml" })
        Get-Item -LiteralPath "$deployment/$name" -ErrorAction Stop
    })
    Assert ($files.Count -eq $defaultPorts.Count) 'An active demo scenario is missing.'
    foreach ($pending in @('postgres','postgres-volume')) {
        if ($supportsPostgres) {
            Assert (Test-Path "$deployment/docker-compose.demo-$pending.yml" -PathType Leaf) 'PostgreSQL provider checkout must offer its independent demo.'
        } else {
            Assert (Test-Path "$deployment/docker-compose.$pending.yml.disabled" -PathType Leaf) 'Keep the pending PostgreSQL scaffold for later integration.'
        }
    }
    foreach ($file in $files) {
        $key = $file.BaseName -replace '^docker-compose\.?',''
        Assert ($defaultPorts.ContainsKey($key)) "Unaccounted scenario: $($file.Name)"
        $config = Get-ComposeConfig $file.FullName
        Assert ($projects.Add($config.name)) "Default project name collision in $($file.Name)"
        $port = [int]$config.services.snapp.ports[0].published
        Assert ($port -eq $defaultPorts[$key]) "Wrong default repository port in $($file.Name)"
        Assert-Endpoints $config $port $file.Name
        foreach ($service in $config.services.PSObject.Properties.Value) {
            if ($service.container_name) { Assert ($containers.Add($service.container_name)) "Container name collision in $($file.Name)" }
            foreach ($binding in $service.ports) {
                Assert ($bindings.Add("$($binding.host_ip):$($binding.published)/$($binding.protocol)")) "Default port collision in $($file.Name)"
            }
        }
        if ($config.services.snis) { Assert ([int]$config.services.snis.ports[0].published -eq $port+1) "Wrong default auth port in $($file.Name)" }
        if ($config.services.snsearch) { Assert ([int]$config.services.snsearch.ports[0].published -eq $port+2) "Wrong default search port in $($file.Name)" }
        if ($config.services.snsql) { Assert ([int]$config.services.snsql.ports[0].published -eq $port+3) "Wrong default DB port in $($file.Name)" }
        if ($config.services.rabbit) { Assert ([int]$config.services.rabbit.ports[0].published -eq $port-1) "Wrong default Rabbit port in $($file.Name)" }
        $legacy = Get-ComposeConfig $file.FullName -Legacy
        $port = [int]$legacy.services.snapp.ports[0].published
        Assert ($port -eq $legacyPorts[$key]) "Legacy repository port changed in $($file.Name)"
        Assert ($legacy.name -eq $config.name) 'Legacy ports must not change project/resource ownership.'
        Assert-Endpoints $legacy $port $file.Name
        if ($legacy.services.snis) { Assert ([int]$legacy.services.snis.ports[0].published -eq $port+1) "Wrong legacy auth port in $($file.Name)" }
        if ($legacy.services.snsearch) { Assert ([int]$legacy.services.snsearch.ports[0].published -eq $port+2) "Wrong legacy search port in $($file.Name)" }
        if ($legacy.services.snsql) {
            $dbPort = $(if ($key -like '*postgres*') { 5532 } else { 9999 })
            Assert ([int]$legacy.services.snsql.ports[0].published -eq $dbPort) "Wrong legacy DB port in $($file.Name): expected $dbPort, got $($legacy.services.snsql.ports[0].published)"
        }
        if ($legacy.services.rabbit) { Assert ([int]$legacy.services.rabbit.ports[0].published -eq 51005) "Wrong legacy Rabbit port in $($file.Name)" }
    }
    # Arbitrary overrides must change both bindings and advertised endpoints.
    $env:REPOSITORY_PORT='52916'; $env:AUTH_PORT='52917'; $env:SEARCH_PORT='52918'
    $env:SQL_PORT='52919'; $env:RABBIT_PORT='52915'; $env:POSTGRES_PORT='52929'
    $overrideScenarios = @('with-search-auth','with-search','local-auth')
    if ($supportsPostgres) { $overrideScenarios += 'demo-postgres' }
    foreach ($scenario in $overrideScenarios) {
        $config = Get-ComposeConfig "$deployment/docker-compose.$scenario.yml" -Legacy
        Assert ([int]$config.services.snapp.ports[0].published -eq 52916) 'Explicit API override must take precedence over legacy defaults.'
        Assert-Endpoints $config 52916 $scenario
        if ($config.services.snis) { Assert ([int]$config.services.snis.ports[0].published -eq 52917) 'Explicit auth port override was ignored.' }
        if ($config.services.snsearch) { Assert ([int]$config.services.snsearch.ports[0].published -eq 52918) 'Explicit search port override was ignored.' }
        if ($config.services.rabbit) { Assert ([int]$config.services.rabbit.ports[0].published -eq 52915) 'Explicit RabbitMQ port override was ignored.' }
        $dbPort = $(if ($scenario -eq 'demo-postgres') { 52929 } else { 52919 })
        Assert ([int]$config.services.snsql.ports[0].published -eq $dbPort) 'Explicit DB port override was ignored.'
    }
} finally {
    foreach ($variable in $portVariables) {
        if ($null -eq $previous[$variable]) { Remove-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($variable,$previous[$variable]) }
    }
}
Write-Host "PASS: $checks deployment assertions (PowerShell, network/container lifecycle, $($files.Count) active demos, provider availability, legacy ports and URL overrides)."
