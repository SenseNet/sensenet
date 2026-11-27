# Index integrity checker

The SQL TokenAuth webapp can compare its SQL Server repository with its local Lucene 2.9 index and repair selected versions. The default provider uses the local writer in the application instance that owns the index. SQL Server must allow snapshot isolation for the check. Keep the index files on a filesystem supported by the local Lucene engine.

Enable the checker in the host configuration and restart the webapp.

```json
{
  "sensenet": {
    "indexIntegrity": {
      "enabled": true,
      "keepPreviousChecks": false
    }
  }
}
```

The host selects the provider at startup. A custom host with sensenet authentication and OData references `SenseNet.IndexIntegrity` and calls `AddSenseNetIndexIntegrityChecker<TProvider>` with its `sensenet:indexIntegrity` configuration section. The provider implements `IIndexIntegrityProvider` and declares whether it supports checking and repair.

## Admin endpoints

The operations are available on the repository root to sensenet administrators. For cookie-authenticated POST requests, send the host's antiforgery token in its configured request header (`RequestVerificationToken` by default).

| Request | Result |
| --- | --- |
| `POST /OData.svc/('Root')/IndexIntegrity/StartCheck` | Queues a full check. Send an empty body or `{}`. The response includes the check ID and a `Location` header for status polling. |
| `GET /OData.svc/('Root')/IndexIntegrity/GetRuns` | Lists retained checks and their repairs, newest first. Each check includes `checkId`, status, start time, duration, and a `repairs` array with the same details for each `repairId`. |
| `GET /OData.svc/('Root')/IndexIntegrity/GetReport?checkId={checkId}` | Downloads the completed comparison report. Add `&view=status` for JSON status and `&repairId={repairId}` for repair status. An unfinished download returns `202` with `Retry-After`. |
| `POST /OData.svc/('Root')/IndexIntegrity/Repair` | Queues a repair for `{"checkId":"{checkId}","differenceIds":[1,5]}` or `{"checkId":"{checkId}","kinds":["NotInIndex"]}`. The `Location` header includes the repair ID. |

Send `{"checkId":"{checkId}","kinds":"All"}` to select every difference in the report. The keyword is case-insensitive and can also be the sole value in the `kinds` array. An empty report completes with zero selected differences.

`GetRuns` returns `status`, `startedAtUtc` in UTC, and `durationMs` in milliseconds for each check and repair. The start time is `null` until the run starts. Duration is recorded when a started run ends. A check without repairs has an empty `repairs` array.

The POSTs accept an optional `Idempotency-Key` while the original run is retained. An active integrity run for the same index or an incompatible repair report returns `409`. A provider that lacks the requested operation returns `501`. Use explicit difference IDs for a selection made in a filtered report view.

## Report and storage

Schema version 3 writes `comparison.csv` in UTF-8 with comma-separated columns and one record per detected difference. The file includes the header even when there are no differences. Missing values are empty cells. The check summary is saved as `summary.json` in the same directory and is also available in the `summary` property of `GetReport?checkId={checkId}&view=status`. Retained JSONL reports from schema versions 1 and 2 remain available for download and repair.

```csv
differenceId,kind,versionId,nodeId,field,expected,actual,repairableByVersionId,nodeModificationDateUtc,versionModificationDateUtc,sqlNodeTimestamp,sqlVersionTimestamp,indexNodeTimestamp,indexVersionTimestamp
```

The node and version modification dates, when available, are recorded as UTC. The row versions are decimal strings so a browser can preserve their full precision. The summary records the start, end and duration of the check and its SQL, Lucene and comparison phases. SQL and Lucene snapshots are independently captured, so live indexing can produce transient differences.

For example, with a 40-second allowance, the client compares the later available SQL modification date with the check start minus 40 seconds. A date at or after the boundary marks the difference as uncertain. A row with no SQL modification date has unknown timing. This classification is a client-side interpretation of the report.

Files are stored under the webapp content root in `App_Data/index-integrity-check/`. An accepted new check removes earlier closed checks for the same index before exporting. Active downloads finish before cleanup. Set `keepPreviousChecks` to retain previous reports for the configured `retention` (seven days by default). Expired history is cleaned on startup and when another check runs. Options also include `timeout`, `maxRunBytes`, `maxTotalBytes` and `minimumFreeBytes`. Mount this folder on a persistent volume for access across restarts.

## Repair behavior

The default provider reloads the selected version's current SQL and Lucene data. It updates or deletes by the indexed `VersionId` using the running writer and preserves the existing index activity checkpoint. Missing and ambiguous version identifiers require manual review. A change that has already been indexed is reported as resolved. A competing write can produce a retryable conflict. The status reports how many selected items were repaired, resolved, changed, in conflict, or require attention.

Normal repository writes and indexing continue during repair. The writer checks its Lucene revision before applying a batch. A SQL change after the repair reads the source can still go undetected while its indexing activity is pending. For example, a repair can add a version to Lucene after that version has been deleted in SQL. The repair can report `repaired` even if the indexed document is stale. Successful processing of the normal indexing activity typically corrects the index. The discrepancy can persist if that activity fails to apply the change.

Repair is applied in bounded batches. A stopped or failed repair can have completed earlier batches. Submitting a new repair rereads the current state. Each repair keeps its status and JSONL results under its own `repairId` for as long as its check is retained, including after subsequent repairs. A saved report may still be downloaded after a compatible host change, and repair requires the saved provider identifier and repair format version to match the active provider.

## Building across the two repositories

The TokenAuth host uses the integrity capability in `SenseNet.Search.Lucene29.Local` 7.6.0-alpha-1.2. Before the packages are published, add a local NuGet source and build `SenseNet.Search` 2.6.0-alpha-1.2 into it. Restore and build the Lucene Local project in `sn-search-lucene29` with the same source, then build the webapp with those packages.
