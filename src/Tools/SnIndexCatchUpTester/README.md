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
| 5–6 | Indítsd újra a webappot. Az app létrehoz, módosít, töröl és mozgat teszttartalmakat, valamint eldob egy zárolt piszkozatot. |
| 7–9 | Állítsd le a webappot. Tedd félre a jelenlegi indexkönyvtárat, másold vissza a 4. lépés mentését, és indítsd el a webappot a 6. lépés utáni SQL-adatbázissal. |
| 10 | Az app ellenőrzi az indexdokumentumokat, a törölt verzióazonosítók hiányát és az aktivitás-checkpointot. Minden ellenőrzés egy `OK` vagy `HIBA` sort kap. |
| 11 | Siker esetén az app törli a saját tesztmappáját, majd megvárja, amíg rendezed a félretett indexmásolatokat. Ha az indexellenőrzés hibát talál, `t` és Enter törli, az üres Enter megőrzi a tesztmappát. |

A teszter minden sikeres tartalmi művelethez egy sorban kiírja a műveletet és az útvonalat. Az indexellenőrzés a mentés utáni létrehozást, az ismételt módosítást, a részfatörlést, a törölt útvonal újrafelhasználását, az eldobott piszkozatot és az érintetlen kontrollt fedi le. Az index trace-ben az `Execution frame catch-up from A… through A…` bejegyzéssel ellenőrizheted a visszajátszás indítását.

A kilépési kód siker esetén `0`, hiba esetén `1`, megszakításnál `130`. Megszakításkor és API-hibánál a konzol kiírja a már létrehozott tesztmappa helyét, hogy később ellenőrizhesd vagy eltávolíthasd.
