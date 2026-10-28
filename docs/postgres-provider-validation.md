# PostgreSQL provider validation

Run these commands from the repository root with PowerShell 7.2+, Git, and Docker Desktop using Linux containers. The tests create disposable containers, databases and certificates. They do not change the certificate trust store or use an existing database.

## Provider SQL contract

```powershell
pwsh -NoProfile -File deployment/tests/Test-PostgresProviderRowGuids.ps1 -BaselineRef 12d7fa7e
```

This test executes the SQL extracted from the checked-out provider against the PostgreSQL schema from the specified Git baseline. It checks GUID lookup, insert/update result columns, version-copy behavior, retained node/version identities and rollback. Its PostgreSQL container has no published ports and is removed afterwards. This verifies the SQL contract independently of HTTP and the application cache.

## Build the current application

```powershell
docker build -f src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile -t local/sn-api-postgre:validation-current src
if ($LASTEXITCODE -ne 0) { throw 'Current PostgreSQL image build failed.' }
```

Run a fresh database regression against that local image:

```powershell
pwsh -NoProfile -File deployment/tests/Test-PostgresProviderRegression.ps1 -Image local/sn-api-postgre:validation-current
```

The HTTP test verifies SNAuth login and the configured Admin UI Origin, protected reads, content creation, Unicode updates, indexed queries, a 2,097,289-byte upload/download with SHA256 equality, GUID fields matching PostgreSQL rows, same-row save identity, version-copy identity, full stack recreation and recursive permanent deletion. It chooses temporary ports and a unique Compose project, then removes only its own containers and volumes. It consumes the supplied application image without rebuilding it. Required dependency images are acquired by Compose when unavailable locally.

## Upgrade an existing database

Build the previous application from an exact commit without changing the current checkout. `12d7fa7e` is the provider baseline used for the develop refresh; replace it with the intended old commit for a later upgrade.

```powershell
$baselineRef = '12d7fa7e'
$baselineRoot = Join-Path $PWD ('deployment/temp/pg-validation-baseline-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $baselineRoot | Out-Null
git archive --format=tar --output="$baselineRoot/source.tar" $baselineRef src
if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
tar -xf "$baselineRoot/source.tar" -C $baselineRoot
if ($LASTEXITCODE -ne 0) { throw 'Baseline extraction failed.' }
docker build -f "$baselineRoot/src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile" -t local/sn-api-postgre:validation-baseline "$baselineRoot/src"
if ($LASTEXITCODE -ne 0) { throw 'Baseline PostgreSQL image build failed.' }

pwsh -NoProfile -File deployment/tests/Test-PostgresProviderRegression.ps1 -Image local/sn-api-postgre:validation-current -BaselineImage local/sn-api-postgre:validation-baseline -TimeoutSeconds 600
```

The upgrade test creates content using the baseline image, records its persisted database GUIDs, replaces the application with the current image on the same database/app-data volumes, signs in again and checks the original metadata, index, binary bytes and identifiers. It then exercises the new GUID fields and version operations, recreates the stack again, and verifies persistence and deletion. The baseline is read through SQL until the upgrade, so it does not need to expose the new OData fields.

Builds leave local images and the archived baseline source for inspection. The tests print PASS messages only after each corresponding assertion succeeds; adding these scripts is not evidence that a particular image has passed them. Record the tested commits/image IDs and actual command results with the change.

These checks cover a local single-repository PostgreSQL deployment and the operations above. They do not certify every custom content handler, concurrent production workload, external blob provider, search-service topology, browser session or external authentication provider. Run the relevant .NET suites and deployment validation in addition to these provider checks.
