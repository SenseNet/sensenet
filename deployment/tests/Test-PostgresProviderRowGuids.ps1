[CmdletBinding()]
param(
    [string]$PostgresImage = 'postgres:16',
    [string]$BaselineRef = 'origin/postgres-provider'
)

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$container = 'sn-pg-rowguids-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$assertions = 0
$created = $false

function Assert-Equal($Actual, $Expected, [string]$Message) {
    if ([string]$Actual -cne [string]$Expected) {
        throw "${Message}: expected '$Expected', got '$Actual'."
    }
    $script:assertions++
}

function Invoke-Sql([string]$Sql) {
    $rows = @($Sql | & docker exec -i $container psql -h 127.0.0.1 -X -qAt -F '|' -v ON_ERROR_STOP=1 -U postgres -d rowguids)
    if ($LASTEXITCODE -ne 0) { throw "PostgreSQL command failed ($LASTEXITCODE)." }
    return $rows
}

# Read the actual provider scripts rather than maintaining copies of their SQL.
$providerSource = Get-Content -Raw (Join-Path $repo 'src/ContentRepository.PostgreSql/Data/PgSqlDataProviderScripts.cs')
function Get-ProviderSql([string]$Name, [hashtable]$Parameters) {
    $pattern = 'protected override string ' + [regex]::Escape($Name) + ' => @"(?<sql>(?:[^"]|"")*)";'
    $match = [regex]::Match($providerSource, $pattern)
    if (-not $match.Success) { throw "Provider SQL not found: $Name" }
    $sql = $match.Groups['sql'].Value.Replace('""', '"')
    return [regex]::Replace($sql, '@[A-Za-z][A-Za-z0-9]*', [System.Text.RegularExpressions.MatchEvaluator]{
        param($parameter)
        $key = $parameter.Value.Substring(1)
        if (-not $Parameters.ContainsKey($key)) { throw "Missing test parameter: $key" }
        [string]$Parameters[$key]
    })
}

function Get-State {
    return (Invoke-Sql @'
SELECT md5(jsonb_build_array(
    (SELECT jsonb_agg(to_jsonb(n) ORDER BY "NodeId") FROM "Nodes" n),
    (SELECT jsonb_agg(to_jsonb(v) ORDER BY "VersionId") FROM "Versions" v),
    (SELECT jsonb_agg(to_jsonb(b) ORDER BY "BinaryPropertyId") FROM "BinaryProperties" b),
    (SELECT jsonb_agg(to_jsonb(r) ORDER BY "ReferencePropertyId") FROM "ReferenceProperties" r),
    (SELECT jsonb_agg(to_jsonb(l) ORDER BY "LongTextPropertyId") FROM "LongTextProperties" l)
)::text);
'@)
}

try {
    $baseline = @(& git -C $repo show "${BaselineRef}:src/ContentRepository.PostgreSql/Scripts/PgSqlInstall_Schema.sql")
    if ($LASTEXITCODE -ne 0) { throw "Cannot read baseline PostgreSQL schema from $BaselineRef." }
    & docker run -d --name $container --label sensenet.test=postgres-rowguids --network none `
        --tmpfs /var/lib/postgresql/data:rw -e POSTGRES_HOST_AUTH_METHOD=trust -e POSTGRES_DB=rowguids $PostgresImage | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start disposable PostgreSQL container.' }
    $created = $true
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        # The entrypoint's temporary initialization server only listens on its Unix
        # socket. TCP readiness waits for the final server after initialization.
        & docker exec $container pg_isready -h 127.0.0.1 -q -U postgres -d rowguids 2>$null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Disposable PostgreSQL did not become ready.' }
    Invoke-Sql ("SET client_min_messages=warning;`n" + ($baseline -join "`n")) | Out-Null
    Invoke-Sql @'
INSERT INTO "NodeTypes" ("NodeTypeId", "Name", "Properties") VALUES (1, 'TestType', '');
INSERT INTO "PropertyTypes" ("PropertyTypeId", "Name", "DataType", "Mapping", "IsContentListProperty")
VALUES (1, 'Binary', 'Binary', 0, 0), (2, 'Reference', 'Reference', 0, 0), (3, 'LongText', 'Text', 0, 0);
INSERT INTO "Nodes" ("NodeId", "NodeTypeId", "IsDeleted", "Name", "Path", "Index", "Locked", "ETag",
    "LockType", "LockTimeout", "LockDate", "LockToken", "LastLockUpdate", "CreationDate", "CreatedById",
    "ModificationDate", "ModifiedById", "OwnerId", "LastMinorVersionId", "LastMajorVersionId")
VALUES (1, 1, 0, 'Root', '/Root', 0, 0, '', 0, 0, '2026-01-01', '', '2026-01-01', '2026-01-01', 1,
    '2026-01-01', 1, 1, 1, 1);
INSERT INTO "Versions" ("VersionId", "NodeId", "MajorNumber", "MinorNumber", "CreationDate", "CreatedById", "ModificationDate", "ModifiedById")
VALUES (1, 1, 1, 0, '2026-01-01', 1, '2026-01-01', 1);
SELECT setval(pg_get_serial_sequence('"Nodes"', 'NodeId'), 1), setval(pg_get_serial_sequence('"Versions"', 'VersionId'), 1);
ALTER TABLE "Nodes" ENABLE TRIGGER ALL;
ALTER TABLE "Versions" ENABLE TRIGGER ALL;
ALTER TABLE "BinaryProperties" ENABLE TRIGGER ALL;
ALTER TABLE "ReferenceProperties" ENABLE TRIGGER ALL;
ALTER TABLE "LongTextProperties" ENABLE TRIGGER ALL;
'@ | Out-Null

    $legacyFunction = @(Invoke-Sql "SELECT pg_get_function_result(oid) FROM pg_proc WHERE proname='sn_insert_node_and_version';")[0]
    Assert-Equal ($legacyFunction -match 'NodeGuid|VersionGuid') $false 'Baseline retains the legacy insert function signature'

    $values = @{
        NodeTypeId='1'; ContentListTypeId='NULL'; ContentListId='NULL'; CreatingInProgress='0'; IsDeleted='0'; IsInherited='0';
        ParentNodeId='1'; Name="'RowGuid'"; DisplayName="'RowGuid'"; Path="'/Root/RowGuid'"; Index='0'; Locked='0'; LockedById='NULL';
        ETag="''"; LockType='0'; LockTimeout='0'; LockDate="'2026-01-01'"; LockToken="''"; LastLockUpdate="'2026-01-01'";
        NodeCreationDate="'2026-01-01'"; NodeCreatedById='1'; NodeModificationDate="'2026-01-01'"; NodeModifiedById='1';
        IsSystem='0'; OwnerId='1'; SavingState='0'; MajorNumber='1'; MinorNumber='0'; Status='1'; ChangedData="'[]'";
        VersionCreationDate="'2026-01-01'"; VersionCreatedById='1'; VersionModificationDate="'2026-01-01'"; VersionModifiedById='1';
        DynamicProperties="''"; ContentListProperties="''"; CreationDate="'2026-01-01'"; CreatedById='1';
        ModificationDate="'2026-01-01'"; ModifiedById='1'
    }
    $inserted = (@(Invoke-Sql (Get-ProviderSql 'InsertNodeAndVersionScript' $values))[0] -split '\|')
    Assert-Equal $inserted.Count 9 'Insert result shape'
    $nodeId = $inserted[0]; $nodeGuid = $inserted[2]; $versionId = $inserted[6]; $versionGuid = $inserted[8]
    Assert-Equal ([Guid]$nodeGuid -ne [Guid]::Empty) $true 'Insert assigns a node GUID'
    Assert-Equal ([Guid]$versionGuid -ne [Guid]::Empty) $true 'Insert assigns a version GUID'
    Assert-Equal $inserted[1] '1' 'Insert preserves legacy initial node timestamp'
    Assert-Equal $inserted[3] $versionId 'Insert publishes the major version pointer'
    Assert-Equal $inserted[4] $versionId 'Insert publishes the minor version pointer'
    Assert-Equal (@(Invoke-Sql "SELECT `"RowGuid`" FROM `"Nodes`" WHERE `"NodeId`"=$nodeId;")[0]) $nodeGuid 'Returned node GUID equals storage'
    Assert-Equal (@(Invoke-Sql "SELECT `"RowGuid`" FROM `"Versions`" WHERE `"VersionId`"=$versionId;")[0]) $versionGuid 'Returned version GUID equals storage'

    $values.NodeId=$nodeId; $values.VersionId=$versionId; $values.NodeTimestamp=$inserted[1]
    $versionUpdate = (@(Invoke-Sql (Get-ProviderSql 'UpdateVersionScript' $values))[0] -split '\|')
    Assert-Equal $versionUpdate[1] $versionGuid 'Version update preserves the stored GUID'
    $values.Index='1'
    $nodeUpdate = (@(Invoke-Sql (Get-ProviderSql 'UpdateNodeScript' $values))[0] -split '\|')
    Assert-Equal $nodeUpdate[1] $nodeGuid 'Node update preserves the stored GUID'
    Assert-Equal $nodeUpdate[0] '2' 'Node update advances timestamp'
    Assert-Equal (@(Invoke-Sql (Get-ProviderSql 'UpdateNodeScript' $values)).Count) 0 'Stale timestamp cannot update a node'

    # Exercise copies with real binary, reference and long text property rows.
    Invoke-Sql @"
INSERT INTO "Files" ("FileId", "ContentType", "Extension", "Size", "Stream") VALUES (1, 'text/plain', 'txt', 3, decode('616263','hex'));
INSERT INTO "BinaryProperties" ("VersionId", "PropertyTypeId", "FileId") VALUES ($versionId, 1, 1);
INSERT INTO "ReferenceProperties" ("VersionId", "PropertyTypeId", "ReferredNodeId") VALUES ($versionId, 2, 1);
INSERT INTO "LongTextProperties" ("VersionId", "PropertyTypeId", "Length", "Value") VALUES ($versionId, 3, 6, 'source');
"@ | Out-Null
    $values.PreviousVersionId=$versionId; $values.DestinationVersionId='NULL'; $values.MajorNumber='2'; $values.Status='2'
    $copied = (@(Invoke-Sql (Get-ProviderSql 'CopyVersionAndUpdateScript' $values))[0] -split '\|')
    $copyId=$copied[0]; $copyGuid=$copied[2]
    Assert-Equal ($copyId -ne $versionId) $true 'Copy creates a new version row'
    Assert-Equal ([Guid]$copyGuid -ne [Guid]::Empty -and $copyGuid -ne $versionGuid) $true 'New version gets a different GUID'
    Assert-Equal (@(Invoke-Sql "SELECT count(*) FROM `"BinaryProperties`" WHERE `"VersionId`"=$copyId;")[0]) '1' 'Copy retains binary properties'
    Assert-Equal (@(Invoke-Sql "SELECT count(*) FROM `"ReferenceProperties`" WHERE `"VersionId`"=$copyId;")[0]) '1' 'Copy retains reference properties'
    Assert-Equal (@(Invoke-Sql "SELECT `"Value`" FROM `"LongTextProperties`" WHERE `"VersionId`"=$copyId;")[0]) 'source' 'Copy retains long text'

    Invoke-Sql "UPDATE `"LongTextProperties`" SET `"Value`"='replacement' WHERE `"VersionId`"=$copyId;" | Out-Null
    $values.PreviousVersionId=$copyId; $values.DestinationVersionId=$versionId; $values.MajorNumber='1'; $values.Status='1'
    $overwritten = (@(Invoke-Sql (Get-ProviderSql 'CopyVersionAndUpdateScript' $values))[0] -split '\|')
    Assert-Equal $overwritten[0] $versionId 'Overwrite keeps destination version id'
    Assert-Equal $overwritten[2] $versionGuid 'Overwrite keeps destination GUID'
    Assert-Equal (@(Invoke-Sql "SELECT count(*) FROM `"BinaryProperties`" WHERE `"VersionId`"=$versionId;")[0]) '1' 'Overwrite replaces binary properties without duplicates'
    Assert-Equal (@(Invoke-Sql "SELECT count(*) FROM `"ReferenceProperties`" WHERE `"VersionId`"=$versionId;")[0]) '1' 'Overwrite replaces reference properties without duplicates'
    Assert-Equal (@(Invoke-Sql "SELECT `"Value`" FROM `"LongTextProperties`" WHERE `"VersionId`"=$versionId;")[0]) 'replacement' 'Overwrite replaces long text'

    $before = Get-State
    foreach ($case in @(
        @{ Source='2147483647'; Destination=$versionId },
        @{ Source=$versionId; Destination='2147483647' },
        @{ Source='2147483647'; Destination='NULL' }
    )) {
        $values.PreviousVersionId=$case.Source; $values.DestinationVersionId=$case.Destination
        Assert-Equal (@(Invoke-Sql (Get-ProviderSql 'CopyVersionAndUpdateScript' $values)).Count) 0 'Missing copy source/destination returns no row'
        Assert-Equal (Get-State) $before 'Missing copy source/destination changes no stored data'
    }
    $values.PreviousVersionId=$versionId; $values.DestinationVersionId=$versionId
    $selfCopy = (@(Invoke-Sql (Get-ProviderSql 'CopyVersionAndUpdateScript' $values))[0] -split '\|')
    Assert-Equal $selfCopy[2] $versionGuid 'Self-copy keeps destination GUID'
    Assert-Equal (@(Invoke-Sql "SELECT count(*) FROM `"BinaryProperties`" WHERE `"VersionId`"=$versionId;")[0]) '1' 'Self-copy preserves binary properties without duplication'
    Assert-Equal (@(Invoke-Sql "SELECT `"Value`" FROM `"LongTextProperties`" WHERE `"VersionId`"=$versionId;")[0]) 'replacement' 'Self-copy preserves long text'

    foreach ($lookup in @(
        @{ Script='GetNodeIdByGuidScript'; Table='Nodes'; Column='NodeId'; Guid=$nodeGuid; Id=$nodeId },
        @{ Script='GetVersionIdByGuidScript'; Table='Versions'; Column='VersionId'; Guid=$versionGuid; Id=$versionId }
    )) {
        # These simple lookup properties use ordinary C# strings.
        $pattern = 'protected override string ' + $lookup.Script + '\s*=>\s*"(?<sql>(?:\\.|[^"\\])*)";'
        $match = [regex]::Match($providerSource, $pattern)
        if (-not $match.Success) { throw "Lookup script not found: $($lookup.Script)" }
        $lookupSql = $match.Groups['sql'].Value.Replace('\"','"')
        Assert-Equal (@(Invoke-Sql ($lookupSql.Replace('@RowGuid', "'$($lookup.Guid)'::uuid")))[0]) $lookup.Id 'GUID lookup resolves stored row'
        Assert-Equal (@(Invoke-Sql ($lookupSql.Replace('@RowGuid', "'00000000-0000-0000-0000-000000000000'::uuid"))).Count) 0 'Empty GUID lookup returns no row'
        Assert-Equal (@(Invoke-Sql ($lookupSql.Replace('@RowGuid', "'$([Guid]::NewGuid())'::uuid"))).Count) 0 'Unknown GUID lookup returns no row'
    }
    $values.VersionIds="'$versionId,$copyId'"; $values.LongTextMaxSize='2147483647'
    $loaded = @(Invoke-Sql (Get-ProviderSql 'LoadNodesScript' $values))
    Assert-Equal (($loaded -join "`n").Contains($nodeGuid)) $true 'Load returns the stored node GUID'
    Assert-Equal (($loaded -join "`n").Contains($versionGuid)) $true 'Load returns the destination version GUID'
    Assert-Equal (($loaded -join "`n").Contains($copyGuid)) $true 'Load returns the copied version GUID'
    $before = Get-State
    $rollbackValues = $values.Clone()
    $rollbackValues.PreviousVersionId=$versionId; $rollbackValues.DestinationVersionId='NULL'
    Invoke-Sql ("BEGIN;`n" + (Get-ProviderSql 'CopyVersionAndUpdateScript' $rollbackValues) + ";`nROLLBACK;") | Out-Null
    Assert-Equal (Get-State) $before 'Rollback restores version rows and properties'
    Assert-Equal (@(Invoke-Sql "SELECT pg_get_function_result(oid) FROM pg_proc WHERE proname='sn_insert_node_and_version';")[0]) $legacyFunction 'Current provider leaves installed legacy insert function unchanged'
    Write-Host "$assertions PostgreSQL provider row GUID assertions passed against $PostgresImage and $BaselineRef schema."
}
finally {
    if ($created) {
        & docker rm -f -v $container | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove owned test container $container." }
    }
}
