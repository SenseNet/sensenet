# Interaktív index catch-up próba

A .NET 8 konzolapp végigvezeti a mentés, visszaállítás és ellenőrzés próbáját. Használj helyi Lucene-indexszel és `AddSenseNetExecutionFrameIndexCatchUpProvider()` regisztrációval konfigurált tesztpéldányt. A repó SQL-es TokenAuth és LocalAuth mintahostja a webapp munkakönyvtára alatti `App_Data/LocalIndex` mappát használja.

Állítsd be az `SN_INDEX_CATCHUP_API_KEY` vagy az `SN_INDEX_CATCHUP_BEARER_TOKEN` környezeti változót adminisztrátori hitelesítéssel. A repó gyökeréből indítsd a konzolappot a tesztpéldány URL-jével és egy szabad futásnévvel.

```powershell
dotnet run --project src/Tools/SnIndexCatchUpTester -- https://localhost:44362 demo-01
```

Az app a kézi lépéseknél megáll. Az **Enter** a művelet elvégzését jelzi, a **q**, majd az **Enter** megszakítja a próbát.

| Lépés | Teendő |
| --- | --- |
| 1–2 | Indítsd el a webappot. Az app létrehozza a `/Root/Content/index-catchup-<futásnév>` tesztmappát, előkészíti a tartalmakat, és megvárja az index commitját. |
| 3–4 | Állítsd le a webappot, majd másold le a teljes indexkönyvtárat egy külön helyre. |
| 5–6 | Indítsd újra a webappot. Az app 24-szer módosít egy mappát, 12 módosítás után eldob egy piszkozatot, és létrehoz 8 ideiglenes mappát. Ezután létrehoz 210 gyerekmappát egy mozgatandó részfában, törli az ideiglenes mappák részfáját és a célútvonalon lévő részfát, majd a megüresedett célútvonalra mozgatja a nagy részfát. |
| 7–9 | Állítsd le a webappot. Tedd félre a jelenlegi indexkönyvtárat, másold vissza a 4. lépés mentését, és indítsd el a webappot a 6. lépés utáni SQL-adatbázissal. |
| 10 | Az app ellenőrzi az indexdokumentumokat, a törölt verzióazonosítók hiányát és az aktivitás-checkpointot. Minden ellenőrzés egy `OK` vagy `FAIL` sort kap. |
| 11 | Siker esetén az app törli a saját tesztmappáját, majd megvárja, amíg rendezed a félretett indexmásolatokat. Ha az indexellenőrzés hibát talál, `t` és Enter törli, az üres Enter megőrzi a tesztmappát. |

A 6. lépés végén a teszter kiírja a várt activity-tartományt. Az indexellenőrzés az eldobott piszkozat és az ideiglenes mappák hiányát, a módosított mappa végső állapotát, az áthelyezett részfa mind a 212 verziójának új útvonalát és az érintetlen kontrollt ellenőrzi.

A részletes összehasonlításhoz kapcsold be az `Index` trace-t, és keresd az `Execution frame catch-up from A… through A…` bejegyzést. A teszter kiírja az ismételten módosított mappa, az eldobott piszkozat és a nyolc ideiglenes mappa `VersionId`-ját, így azonosító szerint találod meg őket az indexírási sorokban. A mappa 24 módosítása egy `Upsert by VersionId` tétellé vonható össze, a piszkozat és az ideiglenes mappák pedig `Delete by VersionId` tételek lesznek. A `processed frame` sorok jelzik a frame-határokat. A nagy részfa `AddTree` feldolgozásánál a 212 verzió a 200-as köteghatár miatt legalább két `Upsert by VersionId` sorban jelenik meg.

A `Skipped activities` és `Coalesced activities` listák activityazonosítókat tartalmaznak. A `Delete by VersionId` és `Upsert by VersionId` listák a köteg indexműveleteit mutatják. Mindegyik listában három egymást követő azonosítótól kezdve intervallum jelenik meg, például `[100, 101, 105-107]`.

A kilépési kód siker esetén `0`, hiba esetén `1`, megszakításnál `130`. Megszakításkor és API-hibánál a konzol kiírja a már létrehozott tesztmappa helyét, hogy később ellenőrizhesd vagy eltávolíthasd.
