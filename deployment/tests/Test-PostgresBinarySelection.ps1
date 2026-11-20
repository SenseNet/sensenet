[CmdletBinding()]
param([string]$PostgresImage = 'postgres:16')

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$container = 'sn-pg-binary-selection-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)

function Invoke-Sql([string]$Sql) {
    $rows = @($Sql | & docker exec -i $container psql -h 127.0.0.1 -X -qAt -F '|' -v ON_ERROR_STOP=1 -U postgres)
    if ($LASTEXITCODE -ne 0) { throw "PostgreSQL command failed ($LASTEXITCODE)." }
    return $rows
}

function Get-Sql([string]$Source, [string]$Boundary) {
    $match = [regex]::Match($Source, $Boundary + '(?<sql>(?:[^"]|"")*)";')
    if (-not $match.Success) { throw "Provider SQL not found: $Boundary" }
    return $match.Groups['sql'].Value.Replace('""', '"').Trim().TrimEnd(';')
}

function Assert-Rows([string]$Name, [string]$Sql, [string[]]$Expected) {
    $actual = @(Invoke-Sql $Sql)
    if (($actual -join ', ') -cne ($Expected -join ', ')) {
        throw "$Name expected [$($Expected -join ', ')] but got [$($actual -join ', ')]."
    }
}

$nodeSource = Get-Content -Raw (Join-Path $repo 'src/ContentRepository.PostgreSql/Data/PgSqlDataProviderScripts.cs')
$blobSource = Get-Content -Raw (Join-Path $repo 'src/ContentRepository.PostgreSql/Data/PgSqlBlobMetaDataProviderScripts.cs')
$nodeBatch = Get-Sql $nodeSource 'protected override string LoadNodesScript => @"'
$nodeSql = [regex]::Match($nodeBatch, '(?s)-- BinaryProperties\s+(.*?);').Groups[1].Value
if (-not $nodeSql) { throw 'Binary properties result not found in LoadNodesScript.' }
$nodeSql = $nodeSql.Replace('@VersionIds', "'100,101'")
$propertySql = Get-Sql $blobSource 'private const string LoadBinaryPropertyScript = @"'
$cacheSql = Get-Sql $blobSource 'private const string LoadBinaryCacheEntityScript = @"'

& docker run -d --rm --name $container --network none -e POSTGRES_HOST_AUTH_METHOD=trust $PostgresImage | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Cannot start disposable PostgreSQL container.' }

try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $container pg_isready -h 127.0.0.1 -q -U postgres 2>$null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Disposable PostgreSQL did not become ready.' }

    $schema = Get-Content -Raw (Join-Path $repo 'src/ContentRepository.PostgreSql/Scripts/PgSqlInstall_Schema.sql')
    foreach ($table in 'Files', 'BinaryProperties') {
        $definition = [regex]::Match($schema, '(?s)CREATE TABLE IF NOT EXISTS "' + $table + '" \(.*?\);')
        if (-not $definition.Success) { throw "Table definition not found: $table" }
        Invoke-Sql $definition.Value | Out-Null
    }

    Invoke-Sql @'
INSERT INTO "Files" ("FileId", "ContentType", "Extension", "Size", "Stream", "Staging") VALUES
    (1, 'application/pdf', 'pdf', 0, NULL, NULL),
    (2, 'application/pdf', 'pdf', 3, decode('616263','hex'), NULL),
    (3, 'application/pdf', 'pdf', 4, decode('25504446','hex'), NULL),
    (4, 'application/pdf', 'pdf', 5, decode('7374616765','hex'), TRUE),
    (5, 'image/png', 'png', 4, decode('89504e47','hex'), NULL);
INSERT INTO "BinaryProperties" ("BinaryPropertyId", "VersionId", "PropertyTypeId", "FileId") VALUES
    (10, 100, 1, 1), (12, 100, 1, 3), (11, 100, 1, 2), (13, 100, 1, 1), (14, 100, 1, 4),
    (15, 100, 2, 5), (16, 101, 1, 1), (17, 100, 3, 4);
'@ | Out-Null

    $property100 = $propertySql.Replace('@VersionId', '100').Replace('@PropertyTypeId', '1')
    $cache100 = $cacheSql.Replace('@VersionId', '100').Replace('@PropertyTypeId', '1').Replace('@MaxSize', '5')
    $cacheLarge = $cacheSql.Replace('@VersionId', '100').Replace('@PropertyTypeId', '1').Replace('@MaxSize', '4')
    $cacheEmpty = $cacheSql.Replace('@VersionId', '101').Replace('@PropertyTypeId', '1').Replace('@MaxSize', '5')
    $cacheStaging = $cacheSql.Replace('@VersionId', '100').Replace('@PropertyTypeId', '3').Replace('@MaxSize', '5')

    Assert-Rows 'Download cache and inline PDF bytes' ('SELECT "BinaryPropertyId", "FileId", "Size", encode("Stream", ''hex'') FROM (' + $cache100 + ') s') @('12|3|4|25504446')
    Assert-Rows 'Standalone binary metadata' ('SELECT "BinaryPropertyId", "FileId", "Size" FROM (' + $property100 + ') s') @('12|3|4')
    Assert-Rows 'Node binary metadata' ('SELECT "VersionId", "PropertyTypeId", "BinaryPropertyId", "FileId" FROM (' + $nodeSql + ') s ORDER BY 1, 2, 3') @('100|1|12|3', '100|2|15|5', '101|1|16|1')
    Assert-Rows 'File ID for streaming above the cache threshold' ('SELECT "BinaryPropertyId", "FileId", "Size", "Stream" IS NULL FROM (' + $cacheLarge + ') s') @('12|3|4|t')
    Assert-Rows 'An empty file remains addressable' ('SELECT "BinaryPropertyId", "Size" FROM (' + $cacheEmpty + ') s') @('16|0')
    Assert-Rows 'A staging-only file remains invisible' $cacheStaging @()
    Write-Host '6 PostgreSQL binary selection checks passed.'
}
finally {
    & docker stop $container | Out-Null
}
