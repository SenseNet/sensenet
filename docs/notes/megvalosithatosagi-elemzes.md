# 🐘 Megvalósíthatósági elemzés: PostgreSQL provider létrehozása a sensenet MSSQL providerből

## Tartalomjegyzék

- [1. Architektúra áttekintése](#1-architektúra-áttekintése)
- [2. Érintett fájlok és kódmennyiség](#2-érintett-fájlok-és-kódmennyiség)
- [3. MSSQL → PostgreSQL SQL konverziók](#3-mssql--postgresql-sql-konverziók)
- [4. C# kód változások](#4-c-kód-változások)
- [5. Adatbázis-séma konverzió](#5-adatbázis-séma-konverzió)
- [6. Az absztrakt réteg szükséges javításai](#6-az-absztrakt-réteg-szükséges-javításai)
- [7. Munkaterv és becslés](#7-munkaterv-és-becslés)
- [8. Kockázatok és kihívások](#8-kockázatok-és-kihívások)
- [9. Javasolt projektstruktúra](#9-javasolt-projektstruktúra)
- [10. Összefoglalás](#10-összefoglalás)

---

## 1. Architektúra áttekintése

A sensenet adatelérési rétege **háromszintű öröklési hierarchiát** követ:

```
DataProvider (abstract, ~988 sor)              ← adatbázis-független
  ├── RelationalDataProviderBase (abstract, ~2759 sor)  ← SQL-alapú, de ⚠️ MSSQL-szivárgás
  │     └── MsSqlDataProvider (konkrét, ~630 + 1344 sor SQL)
  └── InMemoryDataProvider (konkrét, tesztekhez)

SnDataContext (abstract, ~226 sor)             ← ⚠️ MSSQL-szivárgás
  └── MsSqlDataContext (konkrét, ~225 sor)
```

> Az `InMemoryDataProvider` közvetlenül a `DataProvider`-ből származik, **átugorva** a `RelationalDataProviderBase`-t. Ez azt jelenti, hogy egy PostgreSQL provider számára a `RelationalDataProviderBase` a helyes szülőosztály.

### Rétegek részletezése

| Réteg | Projekt | Szerep |
|-------|---------|--------|
| **Common** | `SenseNet.Common` | Legalsó szintű absztrakciók (`SnDataContext`) |
| **BlobStorage** | `SenseNet.BlobStorage` | Blob interfészek + MSSQL implementáció együtt élnek |
| **Storage** | `SenseNet.Storage` | `DataProvider` → `RelationalDataProviderBase` (abstract) |
| **ContentRepository.MsSql** | `SenseNet.ContentRepository.MsSql` | MSSQL-specifikus `MsSqlDataProvider` |
| **ContentRepository.InMemory** | `SenseNet.ContentRepository.InMemory` | In-memory teszt provider |

### DI wiring

A `MsSqlExtensions.cs` (72 sor) regisztrálja az összes szatellit providert a DI containerben.

---

## 2. Érintett fájlok és kódmennyiség

### 🔴 Teljesen újraírandó fájlok (a PostgreSQL provider-ben)

| Fájl | Sorok | Feladat |
|------|------:|---------|
| `MsSqlDataProvider.cs` | 630 | → `PgSqlDataProvider.cs` |
| `MsSqlDataProviderScripts.cs` | 1 344 | → **~70 SQL script** átírása PostgreSQL dialektusra |
| `MsSqlDataContext.cs` | 225 | → `PgSqlDataContext.cs` (Npgsql-lel) |
| `MsSqlDataInstaller.cs` | 489 | → PostgreSQL adatfeltöltő |
| `MsSqlDatabaseInstaller.cs` | 357 | → CREATE DATABASE PostgreSQL módra |
| `MsSqlSchemaInstaller.cs` | 197 | → Bulk insert Npgsql-lel |
| `MsSqlSchemaWriter.cs` | 113 | → Schema writer |
| `MsSqlExclusiveLockDataProvider.cs` | 189 | → PgSql exclusive lock |
| `MsSqlSharedLockDataProvider.cs` | 224 | → PgSql shared lock |
| `MsSqlStatisticalDataProvider.cs` | 423 | → PgSql statisztika |
| `MsSqlPackagingDataProvider.cs` | 427 | → PgSql packaging |
| `MsSqlAccessTokenDataProvider.cs` | 284 | → PgSql token kezelés |
| `MsSqlClientStoreDataProvider.cs` | 352 | → PgSql client store |
| `MsSqlExtensions.cs` | 72 | → PgSql DI regisztráció |
| `SqlScriptReader.cs` | 53 | → Nem kell (`GO` MSSQL-specifikus) |
| `Create_SenseNet_Database.sql` | 1 114 | → PostgreSQL DDL |
| `MsSqlInstall_Security.sql` | 120 | → PostgreSQL security DDL |
| **BlobStorage MsSql fájlok** (5 fájl) | ~1 208 | → PgSql blob kezelés |
| **Components/** (3 fájl) | ~160 | → PgSql component-ek |
| **Összesen** | **~7 980** | Teljes újraírás |

### ⚠️ Javítandó az absztrakt rétegben (MSSQL szivárgás)

| Fájl | Probléma | Javítás |
|------|----------|---------|
| `SnDataContext.cs` (Common) | `SqlException` referencia a `ShouldRetryOnError()`-ban | Absztrakt `IsRetriableException()` metódus bevezetése |
| `RelationalDataProviderBase.cs` (Storage) | `SqlException` elkapás az `IsDatabaseReady()`-ben (~3 helyen) | Virtual/abstract error handling delegálás |
| `SenseNet.Common.csproj` | `Microsoft.Data.SqlClient` NuGet hivatkozás | Opcionálissá tenni vagy kiemelni |
| BlobStorage regisztráció | Hardcoded `MsSqlBlobMetaDataProvider` default | Provider-agnosztikus default |
| Connection string key | `"SnCrMsSql"` hardcoded | → `"SensenetRepository"` (generikus név) |

### ContentRepository.MsSql projekt teljes fájllistája (~6 800 sor)

| Fájl | Sorok | Cél |
|------|------:|-----|
| `MsSqlDataProvider.cs` | 630 | Fő provider — query override-ok, exception kezelés, timestamp-ek |
| `MsSqlDataProviderScripts.cs` | 1 344 | ~40 SQL script property (partial class) |
| `MsSqlDataInstaller.cs` | 489 | Kezdeti adatok `SqlBulkCopy`-val |
| `MsSqlDatabaseInstaller.cs` | 357 | CREATE DATABASE, login-ok, role-ok |
| `MsSqlSchemaInstaller.cs` | 197 | Bulk-insert séma metaadatok |
| `MsSqlSchemaWriter.cs` | 113 | Delegálás a schema installernek |
| `MsSqlExclusiveLockDataProvider.cs` | 189 | Exclusive (app) lock-ok |
| `MsSqlSharedLockDataProvider.cs` | 224 | Shared content lock-ok |
| `MsSqlStatisticalDataProvider.cs` | 423 | Aggregáció / statisztika |
| `MsSqlPackagingDataProvider.cs` | 427 | Csomagkezelés |
| `MsSqlAccessTokenDataProvider.cs` | 284 | Token CRUD dinamikus collation-nel |
| `MsSqlClientStoreDataProvider.cs` | 352 | OAuth client/secret tár |
| `MsSqlExtensions.cs` | 72 | DI regisztráció |
| `SqlScriptReader.cs` | 53 | Script-ek szétválasztása `GO`-nál |
| `Components/MsSqlExclusiveLockComponent.cs` | ~50 | SnComponent patch |
| `Components/MsSqlStatisticsComponent.cs` | ~50 | SnComponent patch |
| `Components/MsSqlClientStoreComponent.cs` | ~60 | SnComponent patch |

### BlobStorage MsSql réteg (~1 208 sor)

| Fájl | Sorok | Cél |
|------|------:|-----|
| `MsSqlBlobMetaDataProvider.cs` | 677 | Blob metadata CRUD |
| `MsSqlBlobMetaDataProviderScripts.cs` | 216 | SQL scriptek blob műveletekhez |
| `BuiltInBlobProvider.cs` | 254 | `VARBINARY(MAX)` olvasás/írás |
| `MsSqlBlobProviderSelector.cs` | 33 | Provider kiválasztás |
| `MsSqlBlobProviderExtensions.cs` | 28 | DI regisztráció |

---

## 3. MSSQL → PostgreSQL SQL konverziók

### 3.1 Típus-megfeleltetések

| MSSQL típus | PostgreSQL típus | Megjegyzés |
|-------------|-----------------|------------|
| `INT IDENTITY(1,1)` | `SERIAL` / `GENERATED ALWAYS AS IDENTITY` | |
| `NVARCHAR(n)` | `VARCHAR(n)` / `TEXT` | PostgreSQL natívan Unicode |
| `NVARCHAR(MAX)` | `TEXT` | |
| `NTEXT` | `TEXT` | |
| `VARBINARY(MAX)` | `BYTEA` | Vagy Large Objects (`lo`) nagy fájloknál |
| `BIT` | `BOOLEAN` | |
| `DATETIME2` | `TIMESTAMP` / `TIMESTAMPTZ` | |
| `TINYINT` | `SMALLINT` | PostgreSQL-ben nincs 1-byte integer |
| `ROWVERSION` / `TIMESTAMP` | Nincs közvetlen megfelelő | → `BIGINT` + trigger, vagy `xmin` system column |
| `MONEY` | `NUMERIC(19,4)` | |
| `IMAGE` | `BYTEA` | |

### 3.2 SQL szintaxis konverziók (~70 script)

| MSSQL funkció | PostgreSQL megfelelő | Érintett scriptek száma |
|---------------|---------------------|------------------------|
| `@@IDENTITY` | `RETURNING id` záradék | ~12 |
| `@@ROWCOUNT` | `GET DIAGNOSTICS row_count` | ~5 |
| `COLLATE Latin1_General_CI_AS` | `COLLATE "und-x-icu"` vagy `ILIKE` | ~8 |
| `STRING_SPLIT(value, ',')` | `string_to_array()` + `unnest()` | ~3 |
| `OUTPUT INSERTED.*` | `RETURNING *` | ~6 |
| `OUTPUT DELETED.*` | `DELETE ... RETURNING *` | ~2 |
| `GETUTCDATE()` | `NOW() AT TIME ZONE 'UTC'` / `CURRENT_TIMESTAMP` | ~8 |
| `DATEADD(minute, -X, GETUTCDATE())` | `NOW() - INTERVAL 'X minutes'` | ~5 |
| `DATALENGTH()` | `octet_length()` / `length()` | ~4 |
| `LEN()` | `length()` | ~2 |
| `CONVERT(type, expr)` | `CAST(expr AS type)` / `expr::type` | ~5 |
| `BEGIN TRY/CATCH` | `BEGIN...EXCEPTION WHEN` (PL/pgSQL) | ~4 |
| `ERROR_NUMBER()` IN (2601, 2627) | `SQLSTATE = '23505'` (unique_violation) | ~2 |
| `RAISERROR` | `RAISE EXCEPTION` | ~2 |
| `WITH (NOLOCK)` | Eltávolítható (MVCC natív) | ~3 |
| `WITH (TABLOCK)` / `(TABLOCKX)` | `LOCK TABLE ... IN ...` | ~2 |
| `CURSOR DECLARE/FETCH` | PL/pgSQL `FOR rec IN query LOOP` | ~3 |
| `ROW_NUMBER() OVER(...)` CTE | Ugyanaz (standard SQL) ✅ | ~2 |
| `INFORMATION_SCHEMA.TABLES` | Ugyanaz ✅ | ~1 |
| `OBJECT_ID('table', 'U')` | `pg_class` / `to_regclass('table')` | ~2 |
| `sys.tables/columns/identity_columns` | `information_schema.*` / `pg_catalog.*` | ~4 |
| `sys.server_principals/databases` | `pg_roles` / `pg_database` | ~2 |
| `sp_addrolemember` | `GRANT role TO user` | ~1 |
| `TRUNCATE TABLE` | Ugyanaz ✅ | ~2 |
| `GO` batch separator | Nem szükséges | SqlScriptReader kihagyható |
| `PRINT` | `RAISE NOTICE` | ~3 |
| `N'string'` prefix | Nem szükséges (natív Unicode) | ~20+ |
| `TOP n` | `LIMIT n` | ~5 |
| Tábla-változók `DECLARE @t TABLE(...)` | `TEMP TABLE` / CTE | ~2 |
| `LIKE ... ESCAPE '\'` | Ugyanaz ✅ (de `\\` PostgreSQL-ben) | ~3 |

### 3.3 A `ROWVERSION` probléma — a legnagyobb kihívás

Az MSSQL `rowversion` (korábbi nevén `timestamp`) egy **automatikusan inkrementálódó 8-byte bináris érték**, amely minden UPDATE-nél változik. A sensenet ezt használja **optimistic concurrency control**-ra: `NodeTimestamp` és `VersionTimestamp`.

PostgreSQL-ben **nincs közvetlen megfelelő**. Lehetséges megoldások:

| Megoldás | Előny | Hátrány |
|----------|-------|---------|
| `BIGINT` + trigger | Hű viselkedés | Trigger overhead, manuális karbantartás |
| `xmin` system column | Zero overhead | Nem stabil tranzakciók között, nem exportálható |
| Application-level version | Egyszerű | Minden UPDATE-nél manuálisan kell növelni |
| `pg_advisory_lock` | Erős lock | Szemantikailag más |

**Javasolt megoldás**: `BIGINT` oszlop + `BEFORE UPDATE` trigger, ami automatikusan növeli az értéket:

```sql
CREATE SEQUENCE global_timestamp_seq;

CREATE OR REPLACE FUNCTION update_timestamp_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."Timestamp" = nextval('global_timestamp_seq');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Alkalmazás minden érintett táblára:
CREATE TRIGGER trg_nodes_timestamp
BEFORE UPDATE ON "Nodes"
FOR EACH ROW EXECUTE FUNCTION update_timestamp_column();
```

### 3.4 Teljes SQL script katalógus

#### MsSqlDataProviderScripts.cs (~40 script property)

| Script Property | MSSQL-specifikus elemek |
|----------------|------------------------|
| `InsertNodeAndVersionScript` | `@@IDENTITY`, `DECLARE @table AS TABLE(...)`, `OUTPUT INSERTED.*` |
| `UpdateNodeScript` / `UpdateVersionScript` | rowversion összehasonlítás, `@@ROWCOUNT` |
| `CopyVersionFromScript` | `@@IDENTITY`, rowversion |
| `DeleteNodeScript` | Multi-step FK sorrenddel |
| `MoveNodeScript` | `COLLATE Latin1_General_CI_AS`, `REPLACE()`, `LIKE...ESCAPE '\'` |
| `QueryNodesByTypeAndPathAndNameScript` | `COLLATE Latin1_General_CI_AS`, `TOP`, `LIKE` escape-pel |
| `LoadNodesScript` | Multi-result-set reader, `STRING_SPLIT()`, `NVARCHAR(MAX)` cast |
| `LoadNodeHeadScript` / `LoadNodeHeadByMembersScript` | `STRING_SPLIT()` |
| `GetTreeSizeScript` | `DATALENGTH()`, `SUM()`, CTE-k |
| `LoadNodeHeadsByPathAndNameScript` | `COLLATE Latin1_General_CI_AS` |
| TreeLock scriptek (Acquire, IsLocked, Release, GetAll, Delete) | `GETUTCDATE()`, `DATEADD()`, `@@IDENTITY` |
| IndexDocument scriptek (Save, Load) | Standard INSERT/SELECT |
| IndexingActivity scriptek (Load, Register, Update, Delete, GetLastId) | `@@IDENTITY`, CTE-k `ROW_NUMBER() OVER(...)`, `@@TRANCOUNT`, `BEGIN TRAN`/`COMMIT TRAN`, `CURSOR`, `@@FETCH_STATUS`, `RAISERROR` |
| `GetNodeTimestampScript` / `GetVersionTimestampScript` | rowversion |
| `AppModelScript` | `COLLATE Latin1_General_CI_AS` |
| SchemaModification scriptek (Load, Start/Finish) | rowversion, `@@ROWCOUNT` |
| `LoggingScript` (WriteAuditEvent) | `@@IDENTITY`, `GETUTCDATE()` |
| Provider tool scriptek (GetNameOfLastNode, LoadChildTypesToAllow, stb.) | `NOLOCK` hint, `CURSOR`, `WITH (TABLOCK)`, `WITH (TABLOCKX)` |
| `GetDatabaseUsageScript` | `DATALENGTH()`, multi-result-set, MSSQL metadata (`sys.tables`, `sys.identity_columns`) |
| `GetHealthScript` | Standard SELECT |

#### Szatellit provider SQL (C# fájlokba beágyazva)

| Provider | SQL műveletek | MSSQL funkciók |
|----------|--------------|----------------|
| ExclusiveLock | Acquire, Refresh, Release, IsLocked, Create Table | `OUTPUT INSERTED.*`, `WITH (NOLOCK)`, `OBJECT_ID('...', 'U')`, `CREATE INDEX ... ON [PRIMARY]` |
| SharedLock | Create, Refresh, Modify, Get, Delete, Cleanup | `GETUTCDATE()`, `DATEADD()`, `TRUNCATE TABLE` |
| Statistical | WriteData, LoadAggregation, CleanupAggregation, EnumerateData, WriteAggregation, LoadFirst/LastAggregationTimeStamp, LoadUsagePeriod | `GETUTCDATE()`, `datetime2(7)`, `BEGIN TRY/END TRY BEGIN CATCH/END CATCH` (upsert), `TEXTIMAGE_ON [PRIMARY]` |
| Packaging | LoadInstalled/Incomplete Components, LoadPackages, SavePackage, UpdatePackage, PackageExistence, DeletePackage, DeleteAll, LoadManifest, GetContentPathsWhereTheyAreAllowedChildren | `@@IDENTITY`, `TRUNCATE TABLE` |
| AccessToken | Create, GetById/Value, Exists, GetAll, Update, Delete, DeleteByUser/Content, Cleanup, Create Table | `OUTPUT INSERTED.*`, `@@IDENTITY`, `GETUTCDATE()`, dinamikus collation (`sys.columns`/`sys.tables` → `_CI_` → `_CS_`) |
| ClientStore | GetAll/ById, Upsert, Delete, DeleteByHost, SaveSecret, Create Table | `BEGIN TRY INSERT / END TRY BEGIN CATCH IF ERROR_NUMBER() IN (2601, 2627) UPDATE END CATCH` (upsert), `FOREIGN KEY`, komplex index definíciók |

#### BlobStorage SQL (MsSqlBlobMetaDataProviderScripts.cs)

| Művelet | MSSQL funkciók |
|---------|----------------|
| InsertBinaryProperty | `@@IDENTITY`, `UPDATE...OUTPUT DELETED.*` |
| DeleteBinaryProperty | Standard DELETE |
| InsertStagingBinary | `@@IDENTITY`, `CONVERT(varbinary, '')` |
| UpdateStream / WriteStagingChunk | `VARBINARY(MAX)` kezelés |
| CommitChunk | `DATALENGTH()` |

---

## 4. C# kód változások

### 4.1 NuGet csomagok

| Jelenlegi (MSSQL) | Szükséges (PostgreSQL) |
|-------------------|----------------------|
| `Microsoft.Data.SqlClient` | `Npgsql` (≥8.0) |
| — | `Npgsql.EntityFrameworkCore.PostgreSQL` (opcionális) |

### 4.2 ADO.NET osztály-megfeleltetések

| MSSQL (`Microsoft.Data.SqlClient`) | PostgreSQL (`Npgsql`) |
|------------------------------------|----------------------|
| `SqlConnection` | `NpgsqlConnection` |
| `SqlCommand` | `NpgsqlCommand` |
| `SqlDataReader` | `NpgsqlDataReader` |
| `SqlParameter` | `NpgsqlParameter` |
| `SqlTransaction` | `NpgsqlTransaction` |
| `SqlException` | `NpgsqlException` / `PostgresException` |
| `SqlBulkCopy` | Nincs → `COPY` command / `NpgsqlBinaryImporter` |
| `SqlConnectionStringBuilder` | `NpgsqlConnectionStringBuilder` |
| `SqlDbType` | `NpgsqlDbType` |

### 4.3 SqlBulkCopy → PostgreSQL COPY

Az `MsSqlSchemaInstaller` és `MsSqlDataInstaller` `SqlBulkCopy`-t használnak tömeges adatbetöltéshez. PostgreSQL-ben ennek megfelelője:

```csharp
// MSSQL:
using var bulkCopy = new SqlBulkCopy(connection, 
    SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepIdentity, transaction);
bulkCopy.DestinationTableName = "Nodes";
bulkCopy.WriteToServer(dataTable);

// PostgreSQL:
await using var writer = await connection.BeginBinaryImportAsync(
    "COPY \"Nodes\" (\"NodeId\", \"NodeTypeId\", \"Name\", \"Path\") FROM STDIN (FORMAT BINARY)");
foreach (var row in data) {
    await writer.StartRowAsync();
    await writer.WriteAsync(row.NodeId, NpgsqlDbType.Integer);
    await writer.WriteAsync(row.NodeTypeId, NpgsqlDbType.Integer);
    await writer.WriteAsync(row.Name, NpgsqlDbType.Varchar);
    await writer.WriteAsync(row.Path, NpgsqlDbType.Varchar);
}
await writer.CompleteAsync();
```

### 4.4 Hibakezelés konverzió

```csharp
// MSSQL:
catch (SqlException ex) when (ex.Number == 2627) { /* unique violation */ }
catch (SqlException ex) when (ex.Number == 1205) { /* deadlock */ }
catch (SqlException ex) when (ex.Number == 4060 || ex.Number == 233) { /* db not ready */ }

// PostgreSQL:
catch (PostgresException ex) when (ex.SqlState == "23505") { /* unique violation */ }
catch (PostgresException ex) when (ex.SqlState == "40P01") { /* deadlock */ }
catch (NpgsqlException ex) when (ex.InnerException is SocketException) { /* db not ready */ }
```

### 4.5 Blob kezelés

Az MSSQL `VARBINARY(MAX)` → PostgreSQL `BYTEA` konverzió viszonylag egyszerű kis fájloknál, de nagy fájloknál (>1GB) a PostgreSQL Large Objects API-t érdemes használni:

```csharp
// MSSQL (BuiltInBlobProvider):
cmd.CommandText = "SELECT Stream FROM Files WHERE FileId = @Id";
reader.GetBytes(0, offset, buffer, 0, count);

// PostgreSQL (kis fájlok - BYTEA):
cmd.CommandText = "SELECT \"Stream\" FROM \"Files\" WHERE \"FileId\" = @id";
// GetBytes() működik BYTEA-val is Npgsql-ben

// PostgreSQL (nagy fájlok - Large Objects):
var manager = new NpgsqlLargeObjectManager(connection);
using var stream = await manager.OpenReadAsync(oid);
```

### 4.6 MSSQL-specifikus C# API használat a jelenlegi kódban

| Típus | Használva itt |
|-------|---------------|
| `SqlConnection` | MsSqlDataContext, MsSqlDatabaseInstaller, MsSqlDataInstaller, MsSqlSchemaInstaller, BuiltInBlobProvider |
| `SqlCommand` | MsSqlDataContext, MsSqlDatabaseInstaller, MsSqlDataInstaller, MsSqlSchemaInstaller |
| `SqlDataReader` | MsSqlDataContext |
| `SqlParameter` | MsSqlDataContext, MsSqlSchemaInstaller |
| `SqlConnectionStringBuilder` | MsSqlDatabaseInstaller |
| `SqlTransaction` | MsSqlDataContext, MsSqlDatabaseInstaller |
| `SqlBulkCopy` | MsSqlDataInstaller, MsSqlSchemaInstaller |
| `SqlException` | MsSqlDataProvider, MsSqlDatabaseInstaller, **SnDataContext** ⚠️, **RelationalDataProviderBase** ⚠️ |
| `SqlInfoMessageEventArgs` | MsSqlDataContext (overloaded), MsSqlStatisticalDataProvider |
| `CommandBehavior` | MsSqlDataContext (SequentialAccess) |

---

## 5. Adatbázis-séma konverzió

### 5.1 Adatbázis-séma (jelenlegi MSSQL)

#### Core táblák (MsSqlInstall_Schema.sql)

| Tábla | Kulcs oszlopok / Megjegyzések |
|-------|-------------------------------|
| `Nodes` | `NodeId` (PK, IDENTITY), `Name`, `Path` (NVARCHAR 450, unique ix), `Timestamp` (rowversion) |
| `Versions` | `VersionId` (PK, IDENTITY), `NodeId` (FK→Nodes), `MajorNumber`, `MinorNumber`, `ChangedData` (NTEXT), `Timestamp` (rowversion) |
| `BinaryProperties` | `BinaryPropertyId` (PK, IDENTITY), `VersionId` (FK→Versions), `FileId` (FK→Files) |
| `Files` | `FileId` (PK, IDENTITY), `ContentType`, `FileNameWithoutExtension`, `Extension`, `Size` (BIGINT), `Stream` (VARBINARY(MAX)), `Staging` (BIT) |
| `LongTextProperties` | `LongTextPropertyId`, `VersionId`, `PropertyTypeId`, `Value` (NTEXT) |
| `ReferenceProperties` | `ReferencePropertyId`, `VersionId`, `PropertyTypeId`, `ReferredNodeId` |
| `IndexingActivities` | `IndexingActivityId` (PK, IDENTITY), `ActivityType`, `CreationDate`, `RunningState`, `NodeId`, `VersionId`, `Path` |
| `TreeLocks` | `TreeLockId`, `Path`, `LockedAt` |
| `LogEntries` | `LogId` (PK, IDENTITY), `EventId`, `Category`, `Priority`, `FormattedMessage`, `Title`, stb. |
| `Packages` | `Id` (PK, IDENTITY), `PackageType`, `ComponentId`, `ComponentVersion`, `Description`, `Manifest` |
| `SchemaModification` | `SchemaModificationId`, `ModificationDate`, `Timestamp` (rowversion) |
| `JournalItems` | `Id`, `When`, `Wherewith`, `What` |
| `WorkflowNotification` | `NotificationId`, `NodeId`, `WorkflowInstanceId`, `WorkflowNodePath` |

#### Security táblák (MsSqlInstall_Security.sql)

| Tábla | Kulcs oszlopok |
|-------|----------------|
| `EFEntities` | `Id` (PK), `OwnerId`, `ParentId` (FK→self), `IsInherited` |
| `EFEntries` | `EFEntityId` (FK), `EntryType`, `IdentityId`, `LocalOnly`, összetett PK |
| `EFMemberships` | `GroupId`, `MemberId`, `IsUser`, összetett PK |
| `EFMessages` | `Id` (PK, IDENTITY), `SavedBy`, `SavedAt`, `ExecutionState`, `LockedBy`, `LockedAt`, `Body` |

#### Szatellit táblák (Component patch-ek hozzák létre)

| Tábla | Létrehozva |
|-------|-----------|
| `ExclusiveLocks` | MsSqlExclusiveLockComponent |
| `StatisticalUsage` | MsSqlStatisticsComponent |
| `ClientApps` | MsSqlClientStoreComponent |
| `ClientSecrets` | MsSqlClientStoreComponent |
| `AccessTokens` | MsSqlAccessTokenDataProvider (inline) |
| `SharedLocks` | MsSqlSharedLockDataProvider (inline) |

#### View-k

- `NodeInfoView` — Nodes ↔ Versions ↔ NodeTypes join
- `ReferencesInfoView` — ReferenceProperties ↔ PropertyTypes ↔ Nodes join
- `PermissionInfoView` — EFEntries ↔ EFEntities ↔ Nodes join
- `MembershipInfoView` — EFMemberships ↔ Nodes join

#### Adatbázis-szintű beállítások (MsSqlDatabaseInstaller alkalmazása)

A telepítő 20+ `ALTER DATABASE` utasítást futtat, többek között:
`ALLOW_SNAPSHOT_ISOLATION ON`, `READ_COMMITTED_SNAPSHOT ON`, `RECOVERY SIMPLE`, `PARAMETERIZATION FORCED`, `PAGE_VERIFY CHECKSUM`, `AUTO_CREATE_STATISTICS ON`, `AUTO_UPDATE_STATISTICS ON`, `AUTO_SHRINK OFF`.

### 5.2 Fő táblák PostgreSQL átalakítása (példa)

```sql
-- MSSQL:
CREATE TABLE [Nodes] (
    [NodeId]       INT            IDENTITY (1, 1) NOT NULL,
    [NodeTypeId]   INT            NOT NULL,
    [Name]         NVARCHAR(450)  NOT NULL,
    [Path]         NVARCHAR(450)  NOT NULL,
    [Timestamp]    ROWVERSION     NOT NULL,
    [IsDeleted]    TINYINT        NULL,
    ...
    CONSTRAINT [PK_Nodes] PRIMARY KEY CLUSTERED ([NodeId])
);

-- PostgreSQL:
CREATE SEQUENCE global_timestamp_seq;

CREATE TABLE "Nodes" (
    "NodeId"       SERIAL         PRIMARY KEY,
    "NodeTypeId"   INTEGER        NOT NULL,
    "Name"         VARCHAR(450)   NOT NULL,
    "Path"         VARCHAR(450)   NOT NULL,
    "Timestamp"    BIGINT         NOT NULL DEFAULT nextval('global_timestamp_seq'),
    "IsDeleted"    SMALLINT       NULL,
    ...
);

CREATE TRIGGER trg_nodes_timestamp
BEFORE UPDATE ON "Nodes"
FOR EACH ROW EXECUTE FUNCTION update_timestamp_column();
```

### 5.3 Elnevezési konvenció

Az MSSQL PascalCase konvenciót használ (`NodeId`, `NodeTypeId`), míg PostgreSQL-ben a konvenció snake_case (`node_id`, `node_type_id`). **Két lehetőség:**

1. **Megtartani a PascalCase-t** idézőjelekkel (`"NodeId"`) — egyszerűbb migráció, de nem idiomatikus
2. **Átírni snake_case-re** — idiomatikus, de az összes C# kódban is módosítani kell az oszlopneveket

**Javasolt**: Megtartani a PascalCase-t idézőjelekkel a kompatibilitás érdekében, mivel ~70 SQL scriptben hivatkoznak rá, és a `RelationalDataProviderBase` is használja az oszlopneveket a result set olvasásakor.

### 5.4 PostgreSQL adatbázis-szintű beállítások (MSSQL megfelelők)

| MSSQL beállítás | PostgreSQL megfelelő |
|----------------|---------------------|
| `ALLOW_SNAPSHOT_ISOLATION ON` | Nincs szükség rá — PostgreSQL natívan MVCC |
| `READ_COMMITTED_SNAPSHOT ON` | Alap viselkedés PostgreSQL-ben |
| `RECOVERY SIMPLE` | `wal_level = minimal` (nem ajánlott prodban) |
| `PARAMETERIZATION FORCED` | `plan_cache_mode = force_generic_plan` (PG14+) |
| `PAGE_VERIFY CHECKSUM` | `data_checksums = on` (initdb-nél) |
| `AUTO_CREATE_STATISTICS ON` | Alapértelmezett — `autovacuum` kezeli |
| `AUTO_UPDATE_STATISTICS ON` | Alapértelmezett |
| `AUTO_SHRINK OFF` | Nincs ilyen koncepció |

---

## 6. Az absztrakt réteg szükséges javításai

### 6.1 `SnDataContext` — SqlException szivárgás megszüntetése

```csharp
// Jelenlegi (hibás - MSSQL-specifikus az abstract base-ben):
protected virtual bool ShouldRetryOnError(Exception ex)
{
    return (ex is SqlException && ex.Message.Contains("A network-related..."));
}

// Javított:
protected abstract bool ShouldRetryOnError(Exception ex);
// Vagy:
protected virtual bool ShouldRetryOnError(Exception ex) => false;
```

### 6.2 `RelationalDataProviderBase` — `IsDatabaseReady()` javítás

```csharp
// Jelenlegi (hibás):
catch (SqlException ex) when (ex.Number == 4060 || ex.Number == 233) { ... }

// Javított:
catch (Exception ex) when (IsDatabaseNotReadyException(ex)) { ... }
protected abstract bool IsDatabaseNotReadyException(Exception ex);
```

### 6.3 `Microsoft.Data.SqlClient` eltávolítása a Common projektből

A `SenseNet.Common.csproj`-ból ki kell venni a `Microsoft.Data.SqlClient` függőséget, és az `MsSqlDataContext`-et (ami jelenleg itt él!) át kell helyezni a `ContentRepository.MsSql` projektbe.

### 6.4 Tranzakció-kezelési minták

A jelenlegi kódban a tranzakciókezelés három szinten történik:

1. **SnDataContext-managed tranzakciók**: A legtöbb provider `BeginTransaction()` → műveletek → `Commit()`. A context `SqlTransaction`-t wrappel.
2. **SQL-szintű tranzakciók**: Egyes scriptek `BEGIN TRAN` / `COMMIT TRAN` inline-t használnak (IndexingActivity register/update scriptek) `@@TRANCOUNT` ellenőrzéssel.
3. **SqlBulkCopy tranzakciók**: `MsSqlSchemaInstaller` explicit `SqlTransaction`-t hoz létre és átadja a `SqlBulkCopy`-nak.

### 6.5 Retry logika

A `MsSqlDataContext` connection pool hibáknál és SQL Server hálózati hibáknál retry-ol az `IRetrier`-en keresztül (`SqlException` ellenőrzéssel).

### 6.6 Upsert minták

Két megközelítés a jelenlegi kódban:
- `BEGIN TRY INSERT / END TRY BEGIN CATCH UPDATE END CATCH` az `ERROR_NUMBER()` ellenőrzéssel (ClientStore, Statistical)
- `OUTPUT INSERTED.*` az UPDATE...FROM subquery-vel (ExclusiveLock acquire)

PostgreSQL-ben mindkettő egyszerűsíthető:
```sql
INSERT INTO ... ON CONFLICT (...) DO UPDATE SET ...
RETURNING *;
```

### 6.7 Connection string kezelés

- `MsSqlDataContext` a konstruktoron keresztül kapja a connection stringet és `SqlConnectionStringBuilder`-t használ.
- `MsSqlDatabaseInstaller` `SqlConnectionStringBuilder`-rel **lecseréli az initial catalog-ot** (átlép `master` db-re a CREATE DATABASE-hez, majd vissza a cél db-re).
- A connection string `IOptions<ConnectionStringOptions>` DI-on keresztül van konfigurálva.

### 6.8 Stored Procedures

**Nincs felhasználó által definiált stored procedure** a kódbázisban. Minden SQL inline. Viszont:
- `sp_addrolemember` (rendszer SP) hívás a `MsSqlDatabaseInstaller`-ben a `db_owner` role kiosztásához.

---

## 7. Munkaterv és becslés

### Fázis 1: Absztrakt réteg tisztítása (előfeltétel)

| Feladat | Becsült idő |
|---------|------------|
| `SnDataContext` MSSQL szivárgás javítása | 2-3 nap |
| `RelationalDataProviderBase` MSSQL szivárgás javítása | 2-3 nap |
| `MsSqlDataContext` áthelyezése Common → MsSql projektbe | 1-2 nap |
| BlobStorage regisztráció generikussá tétele | 1 nap |
| Connection string key generikussá tétele | 0.5 nap |
| Tesztek futtatása, regresszió ellenőrzés | 2-3 nap |
| **Fázis 1 összesen** | **~9-12 nap** |

### Fázis 2: PostgreSQL provider core

| Feladat | Becsült idő |
|---------|------------|
| `PgSqlDataContext` implementálása (Npgsql) | 2-3 nap |
| DDL scriptek átírása (CREATE TABLE, indexek, FK-k) | 3-5 nap |
| ROWVERSION pótlása (sequence + trigger) | 2-3 nap |
| `PgSqlDataProvider` + 70 SQL script átírása | 8-12 nap |
| `PgSqlDataInstaller` (COPY-alapú bulk insert) | 2-3 nap |
| `PgSqlDatabaseInstaller` (CREATE DATABASE PG-módra) | 1-2 nap |
| **Fázis 2 összesen** | **~18-28 nap** |

### Fázis 3: Szatellit providerek

| Feladat | Becsült idő |
|---------|------------|
| `PgSqlExclusiveLockDataProvider` | 1-2 nap |
| `PgSqlSharedLockDataProvider` | 1-2 nap |
| `PgSqlStatisticalDataProvider` | 2-3 nap |
| `PgSqlPackagingDataProvider` | 2-3 nap |
| `PgSqlAccessTokenDataProvider` | 1-2 nap |
| `PgSqlClientStoreDataProvider` | 1-2 nap |
| **Fázis 3 összesen** | **~8-14 nap** |

### Fázis 4: Blob storage

| Feladat | Becsült idő |
|---------|------------|
| `PgSqlBlobMetaDataProvider` | 2-3 nap |
| `PgSqlBuiltInBlobProvider` (BYTEA/Large Objects) | 3-5 nap |
| Chunked upload/download tesztelés | 2-3 nap |
| **Fázis 4 összesen** | **~7-11 nap** |

### Fázis 5: Tesztelés és integráció

| Feladat | Becsült idő |
|---------|------------|
| Test platform létrehozása (`PgSqlPlatform`) | 1-2 nap |
| Integrációs tesztek futtatása és javítás | 5-10 nap |
| Docker compose PostgreSQL-lel | 1-2 nap |
| Teljesítmény tesztelés | 3-5 nap |
| CI/CD pipeline bővítés | 1-2 nap |
| **Fázis 5 összesen** | **~11-21 nap** |

### 📊 Összesítés

| | Optimista | Pesszimista |
|--|-----------|-------------|
| **Teljes idő** | **~53 nap** (~10.5 hét) | **~86 nap** (~17 hét) |
| **Érintett/új fájlok** | **~29 fájl** | |
| **Érintett kódsorok** | **~12 500 sor** | |

---

## 8. Kockázatok és kihívások

| Kockázat | Súlyosság | Mitigáció |
|----------|-----------|-----------|
| `ROWVERSION` pótlása nem triviális — race condition lehetőség | 🔴 Magas | Alapos tesztelés, `pg_advisory_lock` fallback |
| `COLLATE` viselkedés eltér (case-insensitive keresés) | 🟡 Közepes | `CITEXT` extension vagy `ILIKE` használata |
| `SqlBulkCopy` → `COPY` átalakítás nem 1:1 | 🟡 Közepes | `NpgsqlBinaryImporter` jól dokumentált |
| Blob kezelés nagy fájloknál (>1GB) | 🟡 Közepes | Large Objects API |
| Absztrakt réteg módosítása megtörheti az MSSQL providert | 🔴 Magas | Regressziós tesztek futtatása minden lépésnél |
| Tranzakció-kezelés eltérései (nested transactions) | 🟡 Közepes | PostgreSQL `SAVEPOINT` használata |
| A ~70 SQL script manuális konverziója hibalehetőség | 🔴 Magas | Script-enkénti unit tesztek |
| `STRING_SPLIT()` és egyéb MSSQL 2016+ funkciók | 🟢 Alacsony | PostgreSQL-ben natív alternatívák vannak |
| Az InMemory provider nem fedi le az összes relációs edge-case-t | 🟡 Közepes | PostgreSQL-specifikus integrációs tesztek kellenek |
| MSSQL-specifikus error code-ok (deadlock 1205, unique 2627, stb.) | 🟡 Közepes | PostgreSQL SQLSTATE kódok jól dokumentáltak |

---

## 9. Javasolt projektstruktúra

```
src/
├── ContentRepository.MsSql/            ← meglévő (változatlan)
│   ├── MsSqlDataProvider.cs
│   ├── MsSqlDataProviderScripts.cs
│   ├── MsSqlDataContext.cs             ← ide áthelyezve Common-ból (Fázis 1)
│   ├── MsSqlDataInstaller.cs
│   ├── MsSqlDatabaseInstaller.cs
│   ├── MsSqlSchemaInstaller.cs
│   ├── MsSqlSchemaWriter.cs
│   ├── MsSqlExclusiveLockDataProvider.cs
│   ├── MsSqlSharedLockDataProvider.cs
│   ├── MsSqlStatisticalDataProvider.cs
│   ├── MsSqlPackagingDataProvider.cs
│   ├── MsSqlAccessTokenDataProvider.cs
│   ├── MsSqlClientStoreDataProvider.cs
│   ├── MsSqlExtensions.cs
│   ├── SqlScriptReader.cs
│   ├── Components/
│   └── Scripts/
│
├── ContentRepository.PostgreSql/       ← ÚJ PROJEKT
│   ├── PgSqlDataProvider.cs
│   ├── PgSqlDataProviderScripts.cs
│   ├── PgSqlDataContext.cs
│   ├── PgSqlDataInstaller.cs
│   ├── PgSqlDatabaseInstaller.cs
│   ├── PgSqlSchemaInstaller.cs
│   ├── PgSqlSchemaWriter.cs
│   ├── PgSqlExclusiveLockDataProvider.cs
│   ├── PgSqlSharedLockDataProvider.cs
│   ├── PgSqlStatisticalDataProvider.cs
│   ├── PgSqlPackagingDataProvider.cs
│   ├── PgSqlAccessTokenDataProvider.cs
│   ├── PgSqlClientStoreDataProvider.cs
│   ├── PgSqlExtensions.cs
│   ├── Components/
│   │   ├── PgSqlExclusiveLockComponent.cs
│   │   ├── PgSqlStatisticsComponent.cs
│   │   └── PgSqlClientStoreComponent.cs
│   ├── Scripts/
│   │   ├── Create_SenseNet_PostgreSql_Database.sql
│   │   └── PgSqlInstall_Security.sql
│   └── SenseNet.ContentRepository.PostgreSql.csproj
│
├── BlobStorage/
│   └── Data/
│       ├── MsSqlClient/                ← meglévő
│       │   ├── MsSqlBlobMetaDataProvider.cs
│       │   ├── BuiltInBlobProvider.cs
│       │   └── ...
│       └── PgSqlClient/               ← ÚJ
│           ├── PgSqlBlobMetaDataProvider.cs
│           ├── PgSqlBlobMetaDataProviderScripts.cs
│           ├── PgSqlBuiltInBlobProvider.cs
│           ├── PgSqlBlobProviderSelector.cs
│           └── PgSqlBlobProviderExtensions.cs
│
├── Common/
│   └── Storage/Data/
│       └── SnDataContext.cs            ← javítva (SqlException eltávolítva)
│
├── Storage/
│   └── Data/
│       └── RelationalDataProviderBase.cs ← javítva (SqlException eltávolítva)
│
└── Tests/
    └── SenseNet.IntegrationTests.PostgreSql/  ← ÚJ
        ├── Platforms/
        │   └── PgSqlPlatform.cs
        └── ...
```

---

## 10. Összefoglalás

A PostgreSQL provider létrehozásához **~12 500 sor kódot** kell írni/átírni **~29 fájlban**, plusz javítani az absztrakt réteg MSSQL-szivárgásait. A legnagyobb kihívások:

1. **70 SQL script** átírása PostgreSQL dialektusra (különösen az `@@IDENTITY` → `RETURNING`, `ROWVERSION` pótlása, és a `COLLATE` kezelés)
2. **Az absztrakt réteg megtisztítása** az MSSQL-specifikus kódrészletektől (`SqlException`, `MsSqlDataContext` áthelyezése)
3. **A blob storage** PostgreSQL-re portolása (BYTEA vs Large Objects döntés)
4. **Integrációs tesztelés** — a meglévő teszt-infrastruktúra jól felépített, egy `PgSqlPlatform` létrehozásával a legtöbb teszt futtatható lesz

A projekt **reálisan 12-17 hét** egy tapasztalt fejlesztő számára, az **előfeltétel** az absztrakt réteg megtisztítása, ami önmagában ~2 hét.

### Pozitívumok

- ✅ Az architektúra **alapvetően jól strukturált** — a `DataProvider` → `RelationalDataProviderBase` → konkrét provider hierarchia működik
- ✅ Minden SQL **inline string**, nem stored procedure — könnyű megtalálni és átírni
- ✅ A tesztelési infrastruktúra (`IPlatform`, `IntegrationTest<T>`) jól támogatja az új platformok hozzáadását
- ✅ Az `InMemoryDataProvider` precedenst teremt alternatív provider implementálására
- ✅ PostgreSQL-ben az MSSQL-specifikus funkciók szinte mindegyikére van natív alternatíva

### Negatívumok

- ❌ Az absztrakt réteg (`SnDataContext`, `RelationalDataProviderBase`) **MSSQL-szivárgást** tartalmaz
- ❌ A `MsSqlDataContext` a **Common** projektben él, nem az MsSql projektben
- ❌ A `ROWVERSION` pótlása **nem triviális** és potenciálisan race condition-ökhöz vezethet
- ❌ A blob storage réteg **szorosan csatolt** az MSSQL-hez
- ❌ Nincs egységes migrációs keretrendszer — a séma evolúció Component patch-eken keresztül történik, mindegyik saját DDL-lel
