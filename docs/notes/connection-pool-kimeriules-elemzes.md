# 🔍 Connection Pool kimerülés elemzése — MSSQL provider

## Összefoglaló

Ez a dokumentum a sensenet MSSQL providerében található **potenciális connection pool szivárgási pontokat** elemzi. A probléma jellemzően **nagy terhelés alatt** (pl. sérülékenységi vizsgálatok, stressz tesztek) jelentkezik, amikor a `SqlConnection` pool eléri a maximális limitet és az alkalmazás nem tud új kapcsolatokat nyitni.

> **Fontos**: Ez a dokumentum **nem az absztrakciós szivárgásról** (abstraction leak) szól, amit a [megvalósíthatósági elemzés](megvalosithatosagi-elemzes.md) tárgyal. Az absztrakciós szivárgás azt jelenti, hogy az adatbázis-független rétegben MSSQL-specifikus kód szerepel (pl. `SqlException` referencia az `SnDataContext`-ben). A jelen dokumentum a **valódi connection pool kimerülés** okait vizsgálja.

---

## 1. A connection pool működése

Az ADO.NET `SqlConnection` pool alapértelmezetten **100 kapcsolatot** engedélyez connection stringenként. Amikor egy `SqlConnection.Open()` hívás történik, a pool egy szabad kapcsolatot ad vissza. Ha nincs szabad, és a limit elérve, a hívás **várakozik** (alapértelmezetten 15 mp), majd `InvalidOperationException`-t dob: `"Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool."`.

---

## 2. Az `SnDataContext` connection-kezelési modellje

Az `SnDataContext` (fájl: `src/Common/Storage/Data/SnDataContext.cs`) `IDisposable` és manuálisan kezeli a kapcsolatot:

```csharp
protected DbConnection OpenConnection()
{
    if (_connection?.State == ConnectionState.Closed || _connection?.State == ConnectionState.Broken)
    {
        _connection.Dispose();
        _connection = null;
    }
    if (_connection == null)
    {
        _connection = CreateConnection();
        _connection.Open();
    }
    return _connection;
}
```

### A modell lényege

- Egy `SnDataContext` példány **egy connection-t tart életben** a teljes élettartama alatt.
- A connection csak a `Dispose()` hívásakor szabadul fel.
- Ha bármelyik hívó **elfelejtkezik a `Dispose()`-ról** (vagyis nem `using` blokkban használja), a connection **nem kerül vissza a pool-ba** amíg a GC nem gyűjti be.

---

## 3. Gyanús pontok a kódbázisban

### 3.1 🔴 `ExecuteReaderAsync` — connection tartása callback futása alatt

**Fájl**: `src/Common/Storage/Data/MsSqlClient/MsSqlDataContext.cs`

```csharp
public async Task<T> ExecuteReaderAsync<T>(string script, Action<SqlCommand> setParams,
    Func<SqlDataReader, CancellationToken, Task<T>> callbackAsync)
{
    // ...
    cmd.Connection = (SqlConnection) OpenConnection();  // ← connection megnyitva
    // ...
    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
    {
        var result = await callbackAsync(reader, cancellationToken);  // ← callback futása alatt foglalt!
        return result;
    }
}
```

**Probléma**: Ha a `callbackAsync` **lassú** (pl. nagy eredményhalmaz feldolgozása, sok sor iterálás), vagy ha **sok párhuzamos kérés** futtat ilyet egyszerre, a connection pool kimerülhet.

**Hatás**: A sérülékenységi vizsgálatok tipikusan **sok párhuzamos, gyors egymás utáni kérést** generálnak, ami pontosan ezt a helyzetet idézi elő — az összes connection foglalt marad a callback-ek futása alatt.

### 3.2 🔴 Retry logika exception közben

**Fájl**: `src/Common/Storage/Data/SnDataContext.cs`, `RetryAsync()` metódus

```csharp
protected virtual bool ShouldRetryOnError(Exception ex)
{
    return (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
}
```

**Probléma**: Ha a retry megnyit egy újabb kapcsolatot, de az exception kezelés nem dispose-olja az előzőt, akkor **két connection is foglalt** lehet egyidejűleg ugyanarra a műveletre. Retry hurokban ez multiplikálódhat.

### 3.3 🟡 `MsSqlDataContext` tranzakció timeout

**Fájl**: `src/Common/Storage/Data/SnDataContext.cs`, `BeginTransaction()`

A `TransactionWrapper` egy tranzakciót tart nyitva, ami alatt a connection **végig foglalt**. Ha a tranzakció timeout-ol vagy deadlock-ba kerül, a connection **a timeout lejártáig blokkolva marad**, mielőtt visszakerülne a pool-ba.

### 3.4 🟡 Sync-over-async (`GetAwaiter().GetResult()`)

**Fájlok**:
- `src/ContentRepository.MsSql/Packaging/Steps/InstallInitialData.cs`
- `src/ContentRepository/Packaging/Steps/Internal/CheckDatabaseConnection.cs`

A `GetAwaiter().GetResult()` minta **deadlock-ot okozhat** ASP.NET kontextusban, ami azt eredményezi, hogy a connection **soha nem szabadul fel**, mert a continuation nem fut le.

### 3.5 🟡 Bulk insert exception közben

**Fájl**: `src/ContentRepository.MsSql/MsSqlDataInstaller.cs`

Ha a `SqlBulkCopy.WriteToServerAsync` exception-t dob, a connection nem biztos, hogy megfelelően felszabadul, különösen ha az explicit tranzakció is rollback-re vár.

### 3.6 🟢 Kézi `SqlConnection` kezelés (biztonságos)

**Fájl**: `src/ContentRepository.MsSql/MsSqlDatabaseInstaller.cs`

```csharp
private async Task ExecuteSqlCommandAsync(string sql, string connectionString)
{
    using (var cn = new SqlConnection(connectionString))
    using (var cmd = new SqlCommand(sql, cn))
    {
        cmd.CommandType = CommandType.Text;
        cn.Open();
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
```

Ez **rendben van** — a `using` blokk biztosítja, hogy exception esetén is felszabadul a connection.

---

## 4. A legvalószínűbb root cause nagy terhelésnél

### Forgatókönyv

1. Sérülékenységi vizsgáló eszköz **N párhuzamos kérést** küld (N > 50-100)
2. Minden kérés létrehoz egy `SnDataContext`-et
3. Minden context megnyit egy `SqlConnection`-t
4. Az `ExecuteReaderAsync` callback-je **tartja a connection-t** amíg a válasz feldolgozása tart
5. Ha N eléri a pool limitet (alapértelmezetten 100), az új kérések **várakoznak**
6. A timeout (15 mp) lejárta után `InvalidOperationException` dobódik

### Miért nem jelentkezik normál terhelésnél?

Normál terhelésnél a kérések **szekvenciálisan vagy mérsékelt párhuzamossággal** érkeznek, a callback-ek gyorsan lefutnak, és a connection-ök visszakerülnek a pool-ba mielőtt az kimerülne.

---

## 5. Diagnosztikai eszközök

### 5.1 SQL Server oldali monitoring

```sql
-- Aktuális connection pool állapot:
SELECT 
    DB_NAME(dbid) AS DatabaseName,
    COUNT(dbid) AS NumberOfConnections,
    loginame AS LoginName,
    status
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid, loginame, status
ORDER BY NumberOfConnections DESC;

-- Várakozó / blokkolt kapcsolatok:
SELECT 
    r.session_id,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.command,
    t.text AS query_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.blocking_session_id > 0;

-- Connection-ök állapota az alkalmazás felhasználóhoz:
SELECT 
    c.session_id,
    c.connect_time,
    c.last_read,
    c.last_write,
    s.status,
    s.host_name,
    s.program_name,
    s.login_name
FROM sys.dm_exec_connections c
JOIN sys.dm_exec_sessions s ON c.session_id = s.session_id
WHERE s.login_name = 'sensenet_app_user'  -- az alkalmazás SQL user-e
ORDER BY c.connect_time;
```

### 5.2 .NET oldali monitoring

```bash
# dotnet-counters a connection pool metrikák monitorozásához:
dotnet-counters monitor Microsoft.Data.SqlClient.EventSource \
    --counters active-hard-connections,active-soft-connections,\
    number-of-active-connection-pool-groups,\
    number-of-active-connection-pools,\
    number-of-free-connections,\
    number-of-stasis-connections
```

### 5.3 Connection string diagnosztikai beállítások

```
Server=...;Database=...;
Min Pool Size=0;
Max Pool Size=100;
Connection Lifetime=300;
Connection Timeout=30;
Application Name=SenseNetDiagnostics;
```

Az `Application Name` beállítás segít a SQL Server oldalon azonosítani az alkalmazás kapcsolatait a `sys.dm_exec_sessions.program_name` oszlopban.

---

## 6. Javasolt javítások

### 6.1 Azonnali (quick wins)

| # | Javítás | Hatás | Erőfeszítés |
|---|---------|-------|-------------|
| 1 | **Connection string-ben `Max Pool Size` növelése** (pl. 200-ra) | Tüneti kezelés, de azonnali segítség | Konfig változás |
| 2 | **`Connection Lifetime=300` hozzáadása** a connection stringhez | Automatikus connection recycle 5 percenként | Konfig változás |
| 3 | **`dotnet-counters` monitoring** bekapcsolása éles környezetben | Láthatóság a pool állapotáról | Ops feladat |

### 6.2 Középtávú (kódmódosítás)

| # | Javítás | Érintett fájl | Erőfeszítés |
|---|---------|---------------|-------------|
| 4 | **`SnDataContext` Dispose ellenőrzés** — figyelmeztetés/log ha a context Dispose nélkül kerül GC-re | `src/Common/Storage/Data/SnDataContext.cs` | 1 nap |
| 5 | **`ExecuteReaderAsync` timeout** hozzáadása a callback-hez | `src/Common/Storage/Data/MsSqlClient/MsSqlDataContext.cs` | 1 nap |
| 6 | **Retry logika connection kezelés** auditálása — biztosítani, hogy retry előtt az előző connection dispose-olódjon | `src/Common/Storage/Data/SnDataContext.cs` | 2 nap |
| 7 | **`GetAwaiter().GetResult()` eliminálása** a packaging step-ekből | `src/ContentRepository.MsSql/Packaging/Steps/` | 2-3 nap |

### 6.3 Hosszú távú (architekturális)

| # | Javítás | Hatás | Erőfeszítés |
|---|---------|-------|-------------|
| 8 | **Connection-per-command modell** bevezetése — a connection ne a `SnDataContext` élettartamához legyen kötve, hanem minden SQL parancs saját connection-t nyisson és azonnal visszaadja | A pool terhelése drasztikusan csökken | 2-3 hét |
| 9 | **Connection pool monitoring middleware** — ASP.NET middleware ami logol ha a pool kihasználtsága >80% | Korai figyelmeztetés | 1-2 nap |
| 10 | **Rate limiting** middleware nagy terhelésű endpoint-okra | Védekezés a vizsgálatok jellegű terhelés ellen | 2-3 nap |

### 6.4 GC-alapú Dispose ellenőrzés implementáció (4. pont részletezése)

A következő minta figyelmeztet, ha egy `SnDataContext` Dispose nélkül kerül a GC-be:

```csharp
public abstract class SnDataContext : IDisposable
{
    private bool _disposed;
    private readonly string _creationStackTrace;

    protected SnDataContext()
    {
#if DEBUG
        _creationStackTrace = Environment.StackTrace;
#endif
    }

    ~SnDataContext()
    {
        if (!_disposed)
        {
            // LOG: Connection leak detected!
            var message = "SnDataContext was not disposed properly. " +
                          "This may cause connection pool exhaustion.";
#if DEBUG
            message += $" Created at: {_creationStackTrace}";
#endif
            SnTrace.Database.WriteError(message);
            
            Dispose(false);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _connection?.Dispose();
            _transaction?.Dispose();
        }
        _disposed = true;
    }
}
```

---

## 7. Összefoglalás

| Prioritás | Probléma | Valószínűség |
|-----------|----------|-------------|
| 🔴 **Kritikus** | `ExecuteReaderAsync` callback tartja a connection-t → sok párhuzamos kérésnél pool kimerülés | **Nagyon magas** |
| 🔴 **Kritikus** | Retry logika többszörös connection foglalást okozhat | **Közepes** |
| 🟡 **Fontos** | Tranzakció timeout → connection blokkolva marad | **Közepes** |
| 🟡 **Fontos** | Sync-over-async deadlock → connection soha nem szabadul | **Alacsony-közepes** (csak packaging step-ekben) |
| 🟡 **Fontos** | Bulk insert exception → connection nem szabadul | **Alacsony** (csak telepítéskor) |

A legvalószínűbb root cause: **sok párhuzamos kérés egyszerre tartja foglalva a connection-öket a callback-ek futása alatt**, és a pool (alapértelmezetten 100) kimerül. Az azonnali megoldás a pool limit növelése, de hosszú távon a connection kezelési modell felülvizsgálata szükséges.
