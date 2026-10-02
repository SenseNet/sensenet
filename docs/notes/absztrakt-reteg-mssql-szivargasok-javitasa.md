# 🔧 Absztrakt réteg MSSQL szivárgás javítása — Részletes terv

## Összefoglaló

Az `SnDataContext` és `RelationalDataProviderBase` osztályok **elvileg adatbázis-független absztrakt rétegek**, de mindkettő közvetlenül hivatkozik MSSQL-specifikus kódra. Ez megakadályozza, hogy bármilyen nem-MSSQL provider (PostgreSQL, MySQL, stb.) tisztán implementálható legyen. Ez a dokumentum részletesen leírja, **mit, hol és hogyan** kell javítani.

---

## 1. A probléma pontos leírása

### 1.1 `SnDataContext` — 3 MSSQL-szivárgás

**Fájl**: `src/Common/Storage/Data/SnDataContext.cs` (226 sor)  
**Projekt**: `SenseNet.Common` — a legalsó szintű közös csomag, minden más projekt hivatkozik rá

| # | Sor | Probléma | Súlyosság |
|---|-----|----------|-----------|
| 1 | 4. sor | `using Microsoft.Data.SqlClient;` import | 🔴 Kritikus |
| 2 | 191-193 | `ShouldRetryOnError()` — `ex is SqlException` ellenőrzés | 🔴 Kritikus |
| 3 | — | `SenseNet.Common.csproj` — `Microsoft.Data.SqlClient` NuGet csomag függőség | 🔴 Kritikus |

#### A jelenlegi hibás kód (`SnDataContext.cs`, 189-194. sor):

```csharp
internal static bool ShouldRetryOnError(Exception ex)
{
    //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
    return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
           (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
}
```

> ⚠️ A fejlesztők **tudatában vannak** a problémának — a `//TODO` komment 2018 óta ott van.

#### Miért probléma?

- A `SenseNet.Common` NuGet csomagot **minden** sensenet projekt hivatkozza
- Ez azt jelenti, hogy **minden projekt tranzitíven függ** a `Microsoft.Data.SqlClient`-től
- Egy PostgreSQL provider nem tudja elkerülni az MSSQL driver betöltését
- Ráadásul a `ShouldRetryOnError()` metódus `internal static` — **nem overridolható**

### 1.2 `RelationalDataProviderBase` — 2 MSSQL-szivárgás

**Fájl**: `src/Storage/Data/RelationalDataProviderBase.cs` (2759 sor)  
**Projekt**: `SenseNet.Storage`

| # | Sor | Probléma | Súlyosság |
|---|-----|----------|-----------|
| 1 | 5. sor | `using Microsoft.Data.SqlClient;` import | 🔴 Kritikus |
| 2 | 2565-2571 | `IsDatabaseReadyAsync()` — `catch (SqlException ex)` + error number `4060`/`233` | 🔴 Kritikus |
| 3 | 2751-2755 | `ShouldRetryOnError()` — `ex is SqlException` ellenőrzés (duplikáció!) | 🟡 Közepes |

#### A jelenlegi hibás kód #1 (`IsDatabaseReadyAsync`, 2546-2578. sor):

```csharp
public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
{
    const string schemaCheckSql = @"
SELECT CASE WHEN EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Nodes'
)
THEN CAST(1 AS BIT)
ELSE CAST(0 AS BIT) END";

    using var op = SnTrace.Database.StartOperation("RelationalDataProviderBase: IsDatabaseReady()");

    using var ctx = CreateDataContext(cancellationToken);
    bool result;
    try
    {
        var dbResult = await ctx.ExecuteScalarAsync(schemaCheckSql).ConfigureAwait(false);
        result = Convert.ToBoolean(dbResult);
    }
    catch (SqlException ex)                    // ← MSSQL-specifikus!
    {
        if (ex.Number is 4060 or 233)          // ← MSSQL error kódok!
            result = false;
        else
            throw;
    }
    op.Successful = true;
    return result;
}
```

#### A jelenlegi hibás kód #2 (`ShouldRetryOnError`, 2751-2755. sor):

```csharp
protected virtual bool ShouldRetryOnError(Exception ex)
{
    //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
    return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
           (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
}
```

> Ez **szó szerint ugyanaz a kód** mint az `SnDataContext.ShouldRetryOnError()` — duplikáció.

### 1.3 `MsSqlDataContext` rossz helyen van

**Fájl**: `src/Common/Storage/Data/MsSqlClient/MsSqlDataContext.cs` (225 sor)  
**Projekt**: `SenseNet.Common` ← **ITT A BAJ!**

Az MSSQL-specifikus `MsSqlDataContext` a **Common** projektben él, nem a `ContentRepository.MsSql` projektben. Ez az oka, hogy a `SenseNet.Common.csproj`-nak szüksége van a `Microsoft.Data.SqlClient` NuGet csomagra.

---

## 2. Mi a helyes architektúra?

### Jelenlegi állapot (hibás):

```
SenseNet.Common (NuGet: Microsoft.Data.SqlClient ⚠️)
  ├── SnDataContext (abstract, de SqlException-t használ ⚠️)
  └── MsSqlClient/
      └── MsSqlDataContext (konkrét MSSQL — rossz helyen! ⚠️)

SenseNet.Storage (tranzitív: Microsoft.Data.SqlClient ⚠️)
  └── RelationalDataProviderBase (abstract, de SqlException-t használ ⚠️)

SenseNet.ContentRepository.MsSql
  └── MsSqlDataProvider (konkrét MSSQL — rendben ✅)
```

### Cél állapot (tiszta):

```
SenseNet.Common (NEM függ Microsoft.Data.SqlClient-től ✅)
  └── SnDataContext (abstract, NINCS SqlException referencia ✅)

SenseNet.Storage (NEM függ Microsoft.Data.SqlClient-től ✅)
  └── RelationalDataProviderBase (abstract, NINCS SqlException referencia ✅)

SenseNet.ContentRepository.MsSql (NuGet: Microsoft.Data.SqlClient ✅)
  ├── MsSqlDataContext (ide áthelyezve ✅)
  └── MsSqlDataProvider (konkrét MSSQL — rendben ✅)

SenseNet.ContentRepository.PostgreSql (NuGet: Npgsql ✅)  ← LEHETŐVÉ VÁLIK
  ├── PgSqlDataContext
  └── PgSqlDataProvider
```

---

## 3. Javítási terv — `SnDataContext`

### 3.1 A `ShouldRetryOnError()` átalakítása

#### Jelenlegi kód (189-194. sor):

```csharp
internal static bool ShouldRetryOnError(Exception ex)
{
    //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
    return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
           (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
}
```

#### Javasolt megoldás:

A metódust **kettéválasztjuk**: az adatbázis-független rész marad az absztrakt osztályban, az adatbázis-specifikus rész `protected virtual` lesz:

```csharp
/// <summary>
/// Determines whether the given exception is a transient error that should be retried.
/// The base implementation handles connection pool exhaustion (ADO.NET provider-independent).
/// Override in derived classes to add database-specific transient error detection.
/// </summary>
protected virtual bool IsTransientError(Exception ex)
{
    // Connection pool exhaustion — ez ADO.NET szinten adatbázis-független
    return ex is InvalidOperationException &&
           ex.Message.Contains("connection from the pool");
}
```

A `RetryAsync` metódus a belső `ShouldRetryOnError` helyett az `IsTransientError`-t hívja:

```csharp
public Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancel)
{
    return _retrier.RetryAsync(action,
        shouldRetryOnError: (ex, _) => IsTransientError(ex),  // ← módosítva
        onAfterLastIteration: (_, ex, i) =>
        {
            SnTrace.Database.WriteError(
                $"Data layer error: {ex.Message}. Retry cycle ended after {i} iterations.");
            throw new InvalidOperationException("Data layer timeout occurred.", ex);
        },
        cancel: cancel);
}
```

#### A `MsSqlDataContext`-ben az override:

```csharp
public class MsSqlDataContext : SnDataContext
{
    protected override bool IsTransientError(Exception ex)
    {
        // Adatbázis-független transient hibák (connection pool)
        if (base.IsTransientError(ex))
            return true;
        
        // MSSQL-specifikus hálózati hibák
        return ex is SqlException sqlEx &&
               sqlEx.Message.Contains("A network-related or instance-specific error occurred");
    }
}
```

#### Egy jövőbeli `PgSqlDataContext`-ben:

```csharp
public class PgSqlDataContext : SnDataContext
{
    protected override bool IsTransientError(Exception ex)
    {
        if (base.IsTransientError(ex))
            return true;

        // PostgreSQL-specifikus transient hibák
        return ex is NpgsqlException npgEx &&
               (npgEx.IsTransient ||
                (npgEx is PostgresException pgEx && pgEx.SqlState == "57P01")); // admin_shutdown
    }
}
```

### 3.2 A `using Microsoft.Data.SqlClient;` eltávolítása

Miután a `ShouldRetryOnError()`-ből eltávolítjuk a `SqlException` referenciát, a `using Microsoft.Data.SqlClient;` sor törölhető a 4. sorból.

### 3.3 Teljes diff az `SnDataContext.cs`-ben

```diff
 using System;
 using System.Data;
 using System.Data.Common;
-using Microsoft.Data.SqlClient;
 using System.Threading;
 using System.Threading.Tasks;
 using System.Transactions;
@@ -186,11 +185,16 @@
         }
 
-        internal static bool ShouldRetryOnError(Exception ex)
+        /// <summary>
+        /// Determines whether the given exception is a transient error that should be retried.
+        /// Override in derived classes to add database-specific transient error detection.
+        /// </summary>
+        protected virtual bool IsTransientError(Exception ex)
         {
-            //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
-            return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
-                   (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
+            // Connection pool exhaustion — ADO.NET szinten adatbázis-független
+            return ex is InvalidOperationException &&
+                   ex.Message.Contains("connection from the pool");
         }
 
@@ -209,7 +213,7 @@
         public Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancel)
         {
             return _retrier.RetryAsync(action,
-                shouldRetryOnError: (ex, _) => ShouldRetryOnError(ex),
+                shouldRetryOnError: (ex, _) => IsTransientError(ex),
                 onAfterLastIteration: (_, ex, i) =>
                 {
                     SnTrace.Database.WriteError(
```

### 3.4 Hatáselemzés

| Elem | Hatás |
|------|-------|
| `MsSqlDataContext` | Override-olnia kell az `IsTransientError()`-t az MSSQL-specifikus ellenőrzéssel |
| `InMemoryDataContext` | Nem érintett — az alapértelmezett viselkedés megfelelő |
| Teszt `DataContext`-ek | Nem érintett — a connection pool check továbbra is működik |
| Visszafelé kompatibilitás | A viselkedés **nem változik** az MSSQL provider-nél, ha az override helyes |

---

## 4. Javítási terv — `RelationalDataProviderBase`

### 4.1 Az `IsDatabaseReadyAsync()` átalakítása

#### Jelenlegi kód (2546-2578. sor):

```csharp
public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
{
    const string schemaCheckSql = @"
SELECT CASE WHEN EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Nodes'
)
THEN CAST(1 AS BIT)
ELSE CAST(0 AS BIT) END";

    using var ctx = CreateDataContext(cancellationToken);
    bool result;
    try
    {
        var dbResult = await ctx.ExecuteScalarAsync(schemaCheckSql).ConfigureAwait(false);
        result = Convert.ToBoolean(dbResult);
    }
    catch (SqlException ex)
    {
        if (ex.Number is 4060 or 233)
            result = false;
        else
            throw;
    }
    return result;
}
```

#### Problémák:

1. `catch (SqlException ex)` — csak MSSQL exception-öket kap el
2. `ex.Number is 4060 or 233` — MSSQL-specifikus hibakódok:
   - `4060` = "Cannot open database requested by the login"
   - `233` = "A connection was successfully established with the server, but then an error occurred"
3. Az SQL script `CAST(1 AS BIT)` — MSSQL-specifikus (PostgreSQL-ben `CAST(1 AS BOOLEAN)` lenne)

#### Javasolt megoldás — A: Template Method minta

```csharp
public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
{
    using var op = SnTrace.Database.StartOperation("RelationalDataProviderBase: IsDatabaseReady()");

    using var ctx = CreateDataContext(cancellationToken);
    bool result;
    try
    {
        var dbResult = await ctx.ExecuteScalarAsync(SchemaCheckScript).ConfigureAwait(false);
        result = Convert.ToBoolean(dbResult);
    }
    catch (Exception ex) when (IsDatabaseNotAvailableException(ex))
    {
        // The database does not exist yet or is not accessible.
        result = false;
    }
    op.Successful = true;

    return result;
}

/// <summary>
/// Gets the SQL script that checks whether the database schema is ready.
/// Should return a boolean-compatible scalar (1/true or 0/false).
/// </summary>
protected abstract string SchemaCheckScript { get; }

/// <summary>
/// Determines whether the given exception indicates that the database
/// is not available (e.g. does not exist, login failed, not accessible).
/// </summary>
protected abstract bool IsDatabaseNotAvailableException(Exception ex);
```

#### `MsSqlDataProvider` implementáció:

```csharp
protected override string SchemaCheckScript => @"
SELECT CASE WHEN EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Nodes'
)
THEN CAST(1 AS BIT)
ELSE CAST(0 AS BIT) END";

protected override bool IsDatabaseNotAvailableException(Exception ex)
{
    // 4060: Cannot open database requested by the login. The login failed.
    // 233: Connection established but then error occurred during login.
    return ex is SqlException sqlEx && sqlEx.Number is 4060 or 233;
}
```

#### Egy jövőbeli `PgSqlDataProvider` implementáció:

```csharp
protected override string SchemaCheckScript => @"
SELECT CASE WHEN EXISTS (
    SELECT * FROM information_schema.tables WHERE table_name = 'Nodes'
)
THEN true
ELSE false END";

protected override bool IsDatabaseNotAvailableException(Exception ex)
{
    // 3D000: invalid_catalog_name (database does not exist)
    // 28P01: invalid_password
    // 28000: invalid_authorization_specification
    return ex is PostgresException pgEx &&
           pgEx.SqlState is "3D000" or "28P01" or "28000";
}
```

### 4.2 A `ShouldRetryOnError()` átalakítása (duplikáció eltávolítása)

#### Jelenlegi kód (2751-2755. sor):

```csharp
protected virtual bool ShouldRetryOnError(Exception ex)
{
    //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
    return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
           (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
}
```

Ez **duplikáció** — ugyanaz a kód mint az `SnDataContext.ShouldRetryOnError()`.

#### Javasolt megoldás:

A `RelationalDataProviderBase` `ShouldRetryOnError()` metódusát **delegáljuk az `SnDataContext`-re**:

```csharp
protected virtual bool ShouldRetryOnError(Exception ex)
{
    // Delegate to the data context's transient error detection.
    // This avoids duplicating the database-specific retry logic.
    // The concrete data context (MsSql, PostgreSQL, etc.) provides
    // the database-specific checks via IsTransientError() override.
    using var ctx = CreateDataContext(CancellationToken.None);
    return ctx.IsTransientError(ex);
}
```

**Alternatíva** (ha a context létrehozás overhead nem kívánatos):

Az `SnDataContext.IsTransientError()` logikáját kiemelhetjük egy statikus utility-be, vagy a `RelationalDataProviderBase`-ben is bevezethetünk egy abstract `IsTransientError()`-t:

```csharp
/// <summary>
/// Determines whether the exception is a transient database error that should be retried.
/// Must be overridden in database-specific implementations.
/// </summary>
protected abstract bool IsTransientError(Exception ex);

protected bool ShouldRetryOnError(Exception ex)
{
    // Connection pool exhaustion — ADO.NET szinten adatbázis-független
    if (ex is InvalidOperationException && ex.Message.Contains("connection from the pool"))
        return true;

    // Database-specific transient errors
    return IsTransientError(ex);
}
```

#### `MsSqlDataProvider` implementáció:

```csharp
protected override bool IsTransientError(Exception ex)
{
    return ex is SqlException sqlEx &&
           sqlEx.Message.Contains("A network-related or instance-specific error occurred");
}
```

### 4.3 A `using Microsoft.Data.SqlClient;` eltávolítása

Az import az 5. sorban törölhető, miután mindkét `SqlException` referencia eltávolításra került.

### 4.4 Teljes diff a `RelationalDataProviderBase.cs`-ben

```diff
 using System;
 using System.Collections.Generic;
 using System.Data;
 using System.Data.Common;
-using Microsoft.Data.SqlClient;
 using System.Globalization;
 using System.Linq;

@@ -2543,6 +2542,9 @@
         protected abstract string LoadEntityTreeScript { get; }
 
+        protected abstract string SchemaCheckScript { get; }
+        protected abstract bool IsDatabaseNotAvailableException(Exception ex);
+
         public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
         {
-            const string schemaCheckSql = @"
-SELECT CASE WHEN EXISTS (
-    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Nodes'
-)
-THEN CAST(1 AS BIT)
-ELSE CAST(0 AS BIT) END";
-
             using var op = SnTrace.Database.StartOperation("RelationalDataProviderBase: IsDatabaseReady()");
 
             using var ctx = CreateDataContext(cancellationToken);
             bool result;
             try
             {
-                var dbResult = await ctx.ExecuteScalarAsync(schemaCheckSql).ConfigureAwait(false);
+                var dbResult = await ctx.ExecuteScalarAsync(SchemaCheckScript).ConfigureAwait(false);
                 result = Convert.ToBoolean(dbResult);
             }
-            catch (SqlException ex)
+            catch (Exception ex) when (IsDatabaseNotAvailableException(ex))
             {
-                if (ex.Number is 4060 or 233)
-                    result = false;
-                else
-                    throw;
+                result = false;
             }
             op.Successful = true;
 
@@ -2748,10 +2740,12 @@
 
-        protected virtual bool ShouldRetryOnError(Exception ex)
+        protected abstract bool IsTransientError(Exception ex);
+
+        protected bool ShouldRetryOnError(Exception ex)
         {
-            //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
-            return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
-                   (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
+            if (ex is InvalidOperationException && ex.Message.Contains("connection from the pool"))
+                return true;
+            return IsTransientError(ex);
         }
```

---

## 5. Javítási terv — `MsSqlDataContext` áthelyezése

### 5.1 Jelenlegi helyzet

A `MsSqlDataContext.cs` (225 sor) jelenleg itt van:
```
src/Common/Storage/Data/MsSqlClient/MsSqlDataContext.cs
```

Ez a `SenseNet.Common` projektben van, ami a `Microsoft.Data.SqlClient` NuGet csomagot is ide kényszeríti.

### 5.2 Célhelyzet

Áthelyezés ide:
```
src/ContentRepository.MsSql/MsSqlDataContext.cs
```

### 5.3 Lépések

1. **Fájl áthelyezése**: `MsSqlDataContext.cs` mozgatása `Common/Storage/Data/MsSqlClient/` → `ContentRepository.MsSql/`
2. **Namespace megtartása**: A `namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient` marad, nem kell változtatni
3. **`SenseNet.Common.csproj` módosítása**: `Microsoft.Data.SqlClient` PackageReference eltávolítása
4. **`SenseNet.ContentRepository.MsSql.csproj` módosítása**: `Microsoft.Data.SqlClient` PackageReference hozzáadása (ha még nincs)
5. **Referenciák ellenőrzése**: Minden fájl ami `MsSqlDataContext`-et használ, a `ContentRepository.MsSql` projektre kell hivatkozzon

### 5.4 Hatáselemzés

| Elem | Hatás |
|------|-------|
| `SenseNet.Common.csproj` | `Microsoft.Data.SqlClient` NuGet csomag eltávolítható → ~5MB-tal kisebb dependency |
| `SenseNet.ContentRepository.MsSql.csproj` | Már hivatkozik rá tranzitíven, explicit PackageReference hozzáadandó |
| Az `MsSqlDataContext`-et használó összes fájl | Mind a `ContentRepository.MsSql` projektben vannak → nincs hatás |
| A `Common`-ra hivatkozó projektek | **Nem függnek többé** MSSQL drivertől → PostgreSQL provider lehetővé válik |

### 5.5 Kockázat

⚠️ **Ha bármely más projekt** (nem `ContentRepository.MsSql`) közvetlenül hivatkozik az `MsSqlDataContext`-re, az **fordítási hibát** okoz. Ellenőrzendő:

- `SenseNet.Storage` — NEM használja közvetlenül ✅
- `SenseNet.ContentRepository` — NEM használja közvetlenül ✅
- `SenseNet.BlobStorage` — Ellenőrizni kell! A `BuiltInBlobProvider` saját `SqlConnection`-t hoz létre, de nem `MsSqlDataContext`-et
- Tesztek — Az `MsSqlTests` projekt hivatkozik rá, de az már hivatkozik `ContentRepository.MsSql`-re is

---

## 6. A `SenseNet.Common.csproj` módosítás

### Jelenlegi:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
```

### Javított:

Ez a sor **törlendő** a `SenseNet.Common.csproj`-ból, és **hozzáadandó** a `SenseNet.ContentRepository.MsSql.csproj`-hoz.

---

## 7. Összefoglalás — Végrehajtási sorrend

### Lépés 1: `SnDataContext.ShouldRetryOnError()` → `IsTransientError()` (1 nap)

| # | Tennivaló | Fájl |
|---|-----------|------|
| 1a | `ShouldRetryOnError()` átnevezése → `IsTransientError()`, `internal static` → `protected virtual` | `SnDataContext.cs` |
| 1b | Az MSSQL-specifikus ellenőrzés kiemelése a base-ből | `SnDataContext.cs` |
| 1c | `RetryAsync()` módosítása: `ShouldRetryOnError` → `IsTransientError` hívás | `SnDataContext.cs` |
| 1d | `using Microsoft.Data.SqlClient;` törlése | `SnDataContext.cs` |
| 1e | `MsSqlDataContext`-ben `IsTransientError()` override bevezetése | `MsSqlDataContext.cs` |
| 1f | Unit tesztek futtatása | — |

### Lépés 2: `RelationalDataProviderBase` tisztítása (1 nap)

| # | Tennivaló | Fájl |
|---|-----------|------|
| 2a | `IsDatabaseReadyAsync()` — `SchemaCheckScript` abstract property bevezetése | `RelationalDataProviderBase.cs` |
| 2b | `IsDatabaseReadyAsync()` — `IsDatabaseNotAvailableException()` abstract method bevezetése | `RelationalDataProviderBase.cs` |
| 2c | `IsDatabaseReadyAsync()` — `catch (SqlException)` → `catch (Exception) when (...)` | `RelationalDataProviderBase.cs` |
| 2d | `ShouldRetryOnError()` duplikáció eltávolítása + `IsTransientError()` abstract bevezetése | `RelationalDataProviderBase.cs` |
| 2e | `using Microsoft.Data.SqlClient;` törlése | `RelationalDataProviderBase.cs` |
| 2f | `MsSqlDataProvider`-ben a 3 új abstract member implementálása | `MsSqlDataProvider.cs` |
| 2g | Integrációs tesztek futtatása | — |

### Lépés 3: `MsSqlDataContext` áthelyezése + NuGet tisztítás (1 nap)

| # | Tennivaló | Fájl |
|---|-----------|------|
| 3a | `MsSqlDataContext.cs` áthelyezése `Common` → `ContentRepository.MsSql` | fájlrendszer |
| 3b | `Microsoft.Data.SqlClient` eltávolítása `SenseNet.Common.csproj`-ból | `SenseNet.Common.csproj` |
| 3c | `Microsoft.Data.SqlClient` hozzáadása `SenseNet.ContentRepository.MsSql.csproj`-hoz | csproj |
| 3d | Fordítás + referenciahibák javítása | — |
| 3e | Teljes teszt futtatás | — |

### Lépés 4: Validáció (0.5-1 nap)

| # | Tennivaló |
|---|-----------|
| 4a | `dotnet build` az egész solution-re |
| 4b | Összes unit teszt futtatás |
| 4c | MSSQL integrációs tesztek futtatása |
| 4d | Ellenőrzés: `SenseNet.Common.dll` assembly-ben nincs `Microsoft.Data.SqlClient` referencia |
| 4e | Ellenőrzés: `SenseNet.Storage.dll` assembly-ben nincs `Microsoft.Data.SqlClient` referencia |

---

## 8. Új abstract memberek összefoglalása

A javítás után a `MsSqlDataProvider`-ben (és bármely jövőbeli provider-ben) implementálandó új abstract memberek:

| Osztály | Új member | Típus | Cél |
|---------|-----------|-------|-----|
| `SnDataContext` | `IsTransientError(Exception)` | `protected virtual bool` | Transient hiba detektálás (retry) |
| `RelationalDataProviderBase` | `SchemaCheckScript` | `protected abstract string` | SQL az adatbázis készenlét ellenőrzésére |
| `RelationalDataProviderBase` | `IsDatabaseNotAvailableException(Exception)` | `protected abstract bool` | Adatbázis nem elérhető hiba detektálás |
| `RelationalDataProviderBase` | `IsTransientError(Exception)` | `protected abstract bool` | Transient hiba detektálás (retry) — a provider szinten |

> **Megjegyzés**: Az `IsDeadlockException(Exception)` már **helyesen abstract** a `DataProvider` base class-ban, és az `MsSqlDataProvider` helyesen override-olja. Ez nem szorul javításra.

---

## 9. Kockázatok és mitigáció

| Kockázat | Súlyosság | Mitigáció |
|----------|-----------|-----------|
| Az `IsTransientError()` nem-`static` lett → más a hívási minta | 🟡 Közepes | A `RetryAsync()` metódus ugyanúgy hívja, nincs API törés |
| `MsSqlDataContext` áthelyezés referenciatörés | 🟡 Közepes | Fordítás-idejű hiba, azonnal látható |
| `Microsoft.Data.SqlClient` eltávolítása más projektet is érint | 🟡 Közepes | Minden hivatkozó projektet ellenőrizni |
| A `ShouldRetryOnError` visszafelé kompatibilitás | 🟢 Alacsony | A régi `internal static` metódus nem volt publikus API |
| Teszt projektek `MsSqlDataContext` hivatkozása | 🟡 Közepes | Teszt projektekbe is hozzáadni a `ContentRepository.MsSql` referenciát |

---

## 10. Becsült idő

| Fázis | Idő |
|-------|-----|
| `SnDataContext` javítás + `MsSqlDataContext` override | **1 nap** |
| `RelationalDataProviderBase` javítás + `MsSqlDataProvider` implementáció | **1 nap** |
| `MsSqlDataContext` áthelyezés + NuGet tisztítás | **1 nap** |
| Validáció, teszt futtatás, edge-case javítás | **0.5-1 nap** |
| **Összesen** | **3.5-4 nap** |

Ez az eredeti becslés (2×2-3 nap = 4-6 nap) **alsó tartományába** esik, mert a javítások jól definiáltak és a kódbázis áttekinthető.
