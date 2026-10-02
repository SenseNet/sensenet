# ⚡ PostgreSQL vs MSSQL teljesítmény-összehasonlítás a sensenet kontextusában

## Összefoglaló

A PostgreSQL provider bevezetése **önmagában nem garantál jobb teljesítményt**. A sensenet workload jellemzői (hierarchikus fa, sok egyedi kérés, blob kezelés, optimistic concurrency) alapján vannak területek ahol a PostgreSQL **jobb**, és vannak ahol **rosszabb** lenne. Az alábbiakban részletesen elemezzük.

---

## 1. Gyors válasz

| Szempont | PostgreSQL | MSSQL | Megjegyzés |
|----------|:----------:|:-----:|------------|
| **Általános OLTP** | ≈ | ≈ | Érdemi különbség nincs modern verziókban |
| **Párhuzamos olvasás** | ✅ Jobb | — | MVCC natív, nincs lock escalation |
| **Hierarchikus lekérdezések** | ✅ Jobb | — | Recursive CTE optimalizáltabb |
| **JSON/dinamikus tartalom** | ✅ Jobb | — | `jsonb` natív indexeléssel |
| **Blob kezelés (nagy fájlok)** | — | ✅ Jobb | `FILESTREAM` > `BYTEA`/Large Objects |
| **Bulk insert** | — | ✅ Jobb | `SqlBulkCopy` > `COPY` kis adatnál |
| **Connection pooling** | ✅ Jobb* | — | *PgBouncer-rel, natívan nem |
| **ROWVERSION (optimistic concurrency)** | — | ✅ Jobb | Natív vs. emulált (trigger) |
| **Full-text search** | ≈ | ≈ | Mindkettő képes, más megközelítés |
| **Licencköltség** | ✅ Ingyenes | — | Enterprise licenc drága |
| **Windows integráció** | — | ✅ Jobb | AD auth, SSMS, stb. |

**Összesítve**: A sensenet tipikus workloadjánál **±10-15% eltérés** várható egyik vagy másik irányba, a konkrét művelettől függően.

---

## 2. Ahol a PostgreSQL jobb lenne

### 2.1 MVCC és párhuzamos olvasás (🟢 Jelentős előny)

Az MSSQL alapértelmezetten **pessimistic locking**-ot használ (`READ COMMITTED` lock-based isolation). A sensenet kódjában ezért találunk `WITH (NOLOCK)` hinteket:

```sql
-- Jelenlegi MSSQL:
SELECT * FROM Nodes WITH (NOLOCK) WHERE Path = @Path
```

A PostgreSQL **natívan MVCC-alapú** — az olvasók **soha nem blokkolják** az írókat és fordítva. Ez azt jelenti:

- **A `WITH (NOLOCK)` hintekre nincs szükség** (és nincs dirty read kockázat)
- **Nagy terhelés alatt kevesebb lock contention** → kevesebb várakozás
- **A connection pool probléma enyhülhet**, mert a connection-ök rövidebb ideig foglaltak

**Várható hatás**: Nagy párhuzamos olvasási terhelésnél (pl. sok felhasználó egyszerre böngészi a tartalom fát) **10-30% javulás a válaszidőben**.

### 2.2 Hierarchikus lekérdezések (🟢 Közepes előny)

A sensenet egy **fa struktúrájú tartalomkezelő** — a `Path` alapú lekérdezések kritikusak. PostgreSQL-ben a recursive CTE-k **jobban optimalizáltak**:

```sql
-- Recursive CTE teljesítmény:
-- PostgreSQL: natív work table optimalizáció, cycle detection
-- MSSQL: hasonló, de a PostgreSQL optimizer gyakran jobb tervet választ fa-bejárásnál
WITH RECURSIVE subtree AS (
    SELECT node_id, path, parent_node_id FROM nodes WHERE path = '/Root'
    UNION ALL
    SELECT n.node_id, n.path, n.parent_node_id 
    FROM nodes n JOIN subtree s ON n.parent_node_id = s.node_id
)
SELECT * FROM subtree;
```

Emellett a PostgreSQL `ltree` extension **kifejezetten hierarchikus adatokra** lett tervezve:

```sql
-- ltree extension (opcionális, de nagy előny lenne):
CREATE EXTENSION ltree;
ALTER TABLE nodes ADD COLUMN path_ltree ltree;
-- Ezután:
SELECT * FROM nodes WHERE path_ltree <@ 'Root.Content.Documents';
-- GiST index-szel ez O(log n) a LIKE '/Root/Content/Documents%' O(n)-je helyett
```

**Várható hatás**: Mély fa lekérdezéseknél **20-50% javulás**, de csak ha az `ltree` extension-t is kihasználjuk.

### 2.3 Connection pooling PgBouncer-rel (🟢 Jelentős előny)

A PostgreSQL ökoszisztémában a **PgBouncer** egy érett, dedikált connection pooler:

```
Alkalmazás (1000 connection) → PgBouncer (50 pooled) → PostgreSQL (50 backend)
```

| Tulajdonság | ADO.NET SqlConnection pool | PgBouncer |
|-------------|---------------------------|-----------|
| Típus | In-process | Külső process |
| Transaction pooling | ❌ | ✅ |
| Statement pooling | ❌ | ✅ |
| Multi-app pooling | ❌ (app-onként külön pool) | ✅ (közös pool) |
| Connection limit kontroll | Csak per connection string | Globális |
| Monitoring | `dotnet-counters` | Dedikált admin konzol |

**Várható hatás**: A [connection pool kimerülés](connection-pool-kimeriules-elemzes.md) probléma **nagyrészt megoldódna** PgBouncer transaction pooling módban, mert a connection a tranzakció végén azonnal visszakerülne a pool-ba, nem a `SnDataContext.Dispose()` hívásakor.

### 2.4 Partícionálás (🟢 Hosszú távú előny)

Nagy adatbázisoknál a PostgreSQL **deklaratív partícionálása** egyszerűbb:

```sql
-- PostgreSQL natív partícionálás:
CREATE TABLE versions (
    version_id INTEGER,
    node_id INTEGER,
    creation_date TIMESTAMPTZ
) PARTITION BY RANGE (creation_date);

CREATE TABLE versions_2024 PARTITION OF versions
    FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');
CREATE TABLE versions_2025 PARTITION OF versions
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
```

Az MSSQL-ben ez **Enterprise Edition** funkció (partition function + partition scheme), ami **jelentős licencköltséget** jelent.

---

## 3. Ahol az MSSQL jobb marad

### 3.1 ROWVERSION / optimistic concurrency (🔴 MSSQL egyértelműen jobb)

A sensenet **intenzíven használja** a `ROWVERSION`-t (`NodeTimestamp`, `VersionTimestamp`) az optimistic concurrency controlhoz. Az MSSQL-ben ez:

- **Zero-overhead** — a storage engine automatikusan kezeli
- **Garantáltan monoton növekvő** — adatbázis szinten
- **Lock-free** — nem kell trigger, nem kell sequence

PostgreSQL-ben ezt **emulálni kell** (`BIGINT` + trigger + sequence), ami:

- **Trigger overhead**: minden UPDATE-nél fut a trigger → **~5-15% lassulás** UPDATE-intenzív munkaterheléseknél
- **Sequence contention**: nagy párhuzamosságnál a `nextval()` szűk keresztmetszet lehet
- **Nem atomi**: a trigger és az UPDATE nem garantáltan atomi (bár PostgreSQL-ben a trigger a tranzakción belül fut, tehát ez kezelhető)

**Várható hatás**: Content szerkesztés-intenzív workload-nál **5-15% lassulás** az MSSQL-hez képest.

### 3.2 Blob kezelés nagy fájloknál (🔴 MSSQL jobb)

Az MSSQL `FILESTREAM` / `FILETABLE` natívan támogatja a nagy fájlok fájlrendszeri tárolását:

| Szempont | MSSQL FILESTREAM | PostgreSQL BYTEA | PostgreSQL Large Objects |
|----------|-----------------|------------------|------------------------|
| Max méret | 2 TB (fájlrendszer limit) | 1 GB (BYTEA limit) | 4 TB |
| Streaming | ✅ Win32 API | ❌ Teljes betöltés | ✅ De API bonyolultabb |
| Backup | ✅ Integrált | ✅ | ⚠️ Külön `pg_dump -b` |
| WAL terhelés | Alacsony | 🔴 Magas (teljes blob WAL-ba) | Közepes |
| Teljesítmény >100MB | ✅ Kiváló | 🔴 Gyenge | 🟡 Közepes |

**Várható hatás**: Ha a sensenet **nagy fájlokat** kezel (videók, képek >10MB), a PostgreSQL **20-50% lassabb** lehet a blob műveleteknél, különösen a WAL terhelés miatt.

> **Megjegyzés**: A sensenet már most is támogat külső blob provider-eket (Azure Blob Storage, stb.), ami ezt a problémát megkerüli. Ha külső blob storage-ot használnak, ez a pont irreleváns.

### 3.3 Bulk insert teljesítmény (🟡 MSSQL kicsit jobb)

| Művelet | MSSQL `SqlBulkCopy` | PostgreSQL `COPY` |
|---------|---------------------|-------------------|
| Kis adatmennyiség (<1000 sor) | ✅ Gyorsabb (kevesebb overhead) | 🟡 Kicsit lassabb |
| Nagy adatmennyiség (>100K sor) | ≈ | ≈ (mindkettő kiváló) |
| `IDENTITY INSERT` | ✅ Natív `KeepIdentity` flag | 🟡 `OVERRIDING SYSTEM VALUE` |
| Minimal logging | ✅ | ✅ (`COPY` is minimálisan logol) |

**Várható hatás**: Csak telepítéskor és import műveleteknél releváns, **nem éles terhelésnél**.

### 3.4 Query optimizer (🟡 MSSQL néha jobb)

Az MSSQL query optimizer **jobban kezeli a paraméteres lekérdezéseket** (parameter sniffing révén), míg a PostgreSQL generic plan-t választhat, ami nem optimális:

```sql
-- PostgreSQL-ben a prepared statement 5. futtatás után generic plan-t használ
-- Ez nem mindig optimális ha az adateloszlás egyenetlen
PREPARE get_node(int) AS SELECT * FROM nodes WHERE parent_node_id = $1;
EXECUTE get_node(1);  -- 1-5: custom plan, 6+: generic plan
```

**Várható hatás**: Egyes lekérdezéseknél **5-20% eltérés** mindkét irányba, de ez finomhangolható.

---

## 4. Ahol nincs érdemi különbség

| Művelet | Megjegyzés |
|---------|-----------|
| Egyszerű CRUD (INSERT/UPDATE/DELETE/SELECT by PK) | Mindkettő ~1ms alatt |
| Index-alapú keresés | B-tree mindkettőben, hasonló teljesítmény |
| `JOIN` műveletek | Hasonló optimizer stratégiák |
| Tranzakció kezelés | Mindkettő ACID |
| `COUNT(*)` nagy táblákon | Mindkettő lassú index scan nélkül |

---

## 5. Teljesítmény-becslés sensenet workload típusonként

### 5.1 Content böngészés (olvasás-intenzív, ~80% a forgalomból)

| Metrika | MSSQL (baseline) | PostgreSQL (becslés) |
|---------|:-----------------:|:--------------------:|
| Egyszerű node lekérdezés | 100% | ~100% |
| Fa-bejárás (gyerekek listázása) | 100% | ~110-130% ✅ |
| Párhuzamos olvasás (50+ user) | 100% | ~115-130% ✅ |
| Path-alapú keresés | 100% | ~100-120% ✅ |
| **Átlagos olvasási teljesítmény** | **100%** | **~105-120%** ✅ |

### 5.2 Content szerkesztés (írás-intenzív, ~15% a forgalomból)

| Metrika | MSSQL (baseline) | PostgreSQL (becslés) |
|---------|:-----------------:|:--------------------:|
| Node INSERT | 100% | ~95-100% |
| Node UPDATE (ROWVERSION trigger) | 100% | ~85-95% ⚠️ |
| Verzió létrehozás | 100% | ~90-100% |
| Bulk import | 100% | ~95-105% |
| **Átlagos írási teljesítmény** | **100%** | **~90-100%** ⚠️ |

### 5.3 Blob műveletek (~5% a forgalomból)

| Metrika | MSSQL (baseline) | PostgreSQL (becslés) |
|---------|:-----------------:|:--------------------:|
| Kis blob (<1MB) | 100% | ~95-100% |
| Közepes blob (1-10MB) | 100% | ~80-90% ⚠️ |
| Nagy blob (>10MB) | 100% | ~50-80% 🔴 |
| Külső blob storage-zal | 100% | ~100% ✅ |

### 5.4 Összesített becslés (tipikus sensenet workload)

```
Összesített = (Olvasás × 0.80) + (Írás × 0.15) + (Blob × 0.05)

MSSQL:      (100% × 0.80) + (100% × 0.15) + (100% × 0.05) = 100%
PostgreSQL: (112% × 0.80) + (95% × 0.15)  + (90% × 0.05)  = ~108%
PostgreSQL + PgBouncer + ltree:                              = ~115%
PostgreSQL + külső blob + PgBouncer + ltree:                 = ~118%
```

**Összesítve**: A PostgreSQL **~5-18% javulást** hozhat a tipikus sensenet workload-nál, de ez erősen függ:
- Használnak-e külső blob storage-ot (ha igen → nagyobb előny)
- Mennyire írás-intenzív a workload (ha nagyon → kisebb előny)
- Használnak-e PgBouncer-t (ha igen → connection pool probléma megoldva)
- Kihasználják-e a PostgreSQL-specifikus funkciókat (ltree, jsonb)

---

## 6. A connection pool probléma szempontjából

A [connection pool kimerülés elemzésben](connection-pool-kimeriules-elemzes.md) leírt probléma szempontjából a PostgreSQL **közvetett javulást** hozhat:

| Szempont | MSSQL | PostgreSQL |
|----------|-------|------------|
| Lock contention az olvasók között | ⚠️ Lehet | ✅ MVCC → nincs |
| Connection idő olvasásnál | Hosszabb (lock wait) | Rövidebb (nincs lock wait) |
| Külső connection pooler | ❌ Nincs érett megoldás | ✅ PgBouncer |
| Connection per command lehetőség | Lehetséges de nem szokás | PgBouncer transaction mode-dal natív |

**De**: A connection pool probléma **gyökéroka az alkalmazás rétegben van** (az `SnDataContext` tartja a connection-t a callback futása alatt), ami **mindkét adatbázisnál fennáll**. A PostgreSQL a tüneteket enyhíti (kevesebb lock → rövidebb connection idő), de a gyökérokot nem oldja meg.

---

## 7. Nem-teljesítmény előnyök

A PostgreSQL-re váltásnak vannak **nem-teljesítmény jellegű előnyei** is, amelyek hosszú távon fontosabbak lehetnek:

| Előny | Részletek |
|-------|-----------|
| **💰 Licencköltség** | PostgreSQL ingyenes. MSSQL Enterprise: ~$15K/core/év. Egy 8-core szerveren ez ~$120K/év megtakarítás. |
| **🐳 Konténerizálhatóság** | PostgreSQL natívan fut Linuxon, kis image méret (~150MB vs MSSQL ~1.5GB) |
| **☁️ Cloud portabilitás** | Minden felhőben elérhető: AWS RDS/Aurora, Azure Database for PostgreSQL, GCP Cloud SQL |
| **🔓 Vendor lock-in csökkentése** | Nem függ a Microsoft licenc politikájától |
| **🧩 Extension ökoszisztéma** | `ltree`, `pg_trgm`, `PostGIS`, `TimescaleDB`, stb. |
| **👥 Közösség** | Gyorsabban növekvő közösség, több contributor |

---

## 8. Javaslat

### Ha a fő motiváció a teljesítmény javítása:
❌ **Nem érdemes** — a PostgreSQL provider fejlesztése ~12-17 hét, ami ~5-18% javulást hoz. Ugyanezt az időt fordítva az MSSQL provider és az `SnDataContext` optimalizálására (connection kezelés, query tuning) **nagyobb és gyorsabb eredményt** lehet elérni.

### Ha a fő motiváció a connection pool probléma megoldása:
⚠️ **Részben segít** — a PgBouncer + MVCC enyhíti a tüneteket, de a gyökérok (application-level connection kezelés) mindkét adatbázisnál javítandó.

### Ha a fő motiváció a költségcsökkentés és portabilitás:
✅ **Érdemes** — az MSSQL licencköltség megtakarítása 1-2 év alatt megtérülhet, és a konténerizálhatóság / cloud portabilitás stratégiai előny.

### Ha a fő motiváció az összes fenti:
✅ **Érdemes, de fázisoltan** — először javítani az absztrakt réteget és a connection kezelést (ami mindkét provider-nek jó), aztán a PostgreSQL providert ráépíteni.
