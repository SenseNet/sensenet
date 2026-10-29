# Health endpoint

`GET /health` returns the latest health snapshot collected by background workers. A request never starts a database, blob, search or identity probe, waits for a probe, or serializes a health response. It selects a prepared UTF-8 response and writes it to the connection.

For implementation details and the exact component measurement call sites, see [Health monitoring for developers](health-for-developers.md).

## Visibility

Anonymous callers and authenticated users without diagnostic access receive only the overall color and four component colors:

```json
{
  "Health": {
    "Color": "Yellow",
    "Database": { "Color": "Green" },
    "BlobStorage": { "Color": "Green" },
    "Search": { "Color": "Yellow" },
    "Identity": { "Color": "Green" }
  }
}
```

The configured `HealthCheckerUser`, members of `Administrators`, and members of `/Root/IMS/BuiltIn/Portal/Developers` receive the detailed response from the same URL. Indirect group membership is supported. The detailed response retains `Repository_Status`, `Health`, and `Details`, including `Details.Repository_StatusHistory`. Component health also includes collection state, measurement time, last successful measurement time, and validity timestamps. Details have their own collection and validity timestamps.

All `Color` values are strings: `Green`, `Yellow`, or `Red`. The overall color is the worst effective component or repository lifecycle color. HTTP status remains `200`, including unhealthy results; clients must inspect the colors. Responses use `application/json; charset=utf-8` and `Cache-Control: no-store`. `HEAD` returns the same response headers without a body. The existing `/health` segment-prefix matching is retained.

## Pipeline configuration

`AddSenseNet` registers the background service and its default collectors. Add the early middleware before repository-dependent CORS, authentication, membership or application middleware, and keep the normal health middleware after authentication:

```csharp
app.UseRouting();
app.UseSenseNetHealthEarly();
app.UseSenseNetCors();
app.UseSenseNetAuthentication();
app.UseSenseNetMembershipExtenders();
app.UseAuthorization();
app.UseSenseNetHealth();
// Other application middleware...
```

The early branch serves anonymous requests and the configured health key directly from memory, even before repository startup completes. It bypasses normal user lookup, repository CORS and request-statistics collection. Health CORS origins therefore have a separate, static `AllowedOrigins` list. An empty list grants no cross-origin access; it does not restrict server-to-server clients.

The key is read from the `apikey` query parameter first, then the `apikey` header. A recognized dedicated health key takes precedence over other credentials; it must still be valid. Empty keys never authenticate. The configured key retains the existing one-year lifetime from validator creation. General API keys and bearer tokens continue through normal authentication. JWT cookies are recognized when `AuthenticationOptions.AddJwtCookie` is enabled.

Normal user authentication may still load repository data or validate a token remotely. Its cost is separate from health collection. Use anonymous requests or the dedicated health key for dependency-independent monitoring. During repository startup, normal user credentials receive only colors. Failed authentication never grants detailed access. Exceptions before a health response starts fall back to the public snapshot.

The normal branch evaluates the user captured by `UseSenseNetUser`, before later middleware can replace `User.Current`. In particular, the Admin sample applications' automatic administrator assignment does not expose diagnostics to anonymous callers. Custom authentication integrations must use the normal sensenet user middleware or implement `IHealthAccessEvaluator` from their verified request identity.

## Collection and freshness

| Component | Background operation | Default interval | Timeout | Maximum result age |
| --- | --- | --- | --- | --- |
| Database | `DataProvider.GetHealthAsync`; the SQL provider reads the root node path. | 10 s | 3 s | 30 s |
| BlobStorage | `IBlobStorage.GetHealthAsync`; reads the first blob's metadata/context. | 10 s | 3 s | 30 s |
| Search | Reads indexing activity status. Configuration comparison runs with details collection. | 10 s | 3 s | 30 s |
| Identity | Fetches and checks OpenID configuration using MetadataHost or Authority. | 30 s | 4 s | 90 s |

Intervals start after the previous collection cycle and vary by up to 10%. The first measurement starts after repository initialization and the startup callback, with 0–1 second jitter. Failure does not increase the interval. Details collection runs after the status measurement when due, normally every five minutes. It has a separate five-second timeout and ten-minute maximum age. Failed details collection retries on later component cycles.

Each component has at most one active operation, including details collection. On timeout, the result turns red and cancellation is requested. A provider that ignores cancellation retains its slot until it finishes; repeated ticks cannot create more blocked operations. Late results from timed-out operations are discarded. Other components and publication continue independently.

Unmeasured components start yellow and turn red after the startup grace period. Missing required providers, timeouts and expired measurements are red. Unavailable or expired diagnostic details raise a component to at least yellow; a detected Search configuration mismatch is red. Explicitly disabled optional checks are green with `Disabled` in their detailed state; they do not represent successful measurements. Local-only authentication or an application with no external authentication scheme/authority reports Identity as `NotApplicable`.

Publication runs on component/status changes and at least once per second. If publication stops, or a result reaches a color-changing expiry before the next publication, the request selects an already prepared `SnapshotExpired` response. All colors in that fallback are red because current health is unknown. A new publication restores the ordinary response.

The snapshot is local to the application instance. In NLB deployments, a response describes the instance that handled that request. The blob check verifies metadata access, not every external provider's ability to read and write blob bytes.

## Configuration

All values are under `sensenet:HealthMonitoring` and take effect after restart. Example with the defaults:

```json
{
  "sensenet": {
    "HealthMonitoring": {
      "Database": { "Enabled": true, "Required": true, "Interval": "00:00:10", "Timeout": "00:00:03", "MaxAge": "00:00:30" },
      "BlobStorage": { "Enabled": true, "Required": true, "Interval": "00:00:10", "Timeout": "00:00:03", "MaxAge": "00:00:30" },
      "Search": { "Enabled": true, "Required": true, "Interval": "00:00:10", "Timeout": "00:00:03", "MaxAge": "00:00:30" },
      "Identity": { "Enabled": true, "Required": true, "Interval": "00:00:30", "Timeout": "00:00:04", "MaxAge": "00:01:30" },
      "PublishInterval": "00:00:01",
      "PublisherMaxAge": "00:00:05",
      "StartupGracePeriod": "00:01:00",
      "InitialJitter": "00:00:01",
      "JitterRatio": 0.1,
      "DetailsInterval": "00:05:00",
      "DetailsTimeout": "00:00:05",
      "DetailsMaxAge": "00:10:00",
      "ShutdownTimeout": "00:00:05",
      "AllowedOrigins": []
    }
  }
}
```

| Option | Meaning |
| --- | --- |
| Component `Enabled` | Starts or disables collection for that component. |
| Component `Required` | A disabled required component is red; a disabled optional component is green and explicitly marked disabled. Missing enabled providers remain errors. |
| Component `Interval` | Delay between completed collection cycles, before jitter. |
| Component `Timeout` | Maximum time to accept a measurement result. Does not forcibly terminate a provider. |
| Component `MaxAge` | Maximum age of an accepted measurement before it turns red. |
| `PublishInterval` | Maximum normal wait between publications; change notifications may publish sooner. |
| `PublisherMaxAge` | Maximum permitted publication age before the cached expiry response is used. |
| `StartupGracePeriod` | Time allowed for the first measurement and full repository startup. |
| `InitialJitter` | Maximum random initial delay after repository readiness. |
| `JitterRatio` | Fractional variation around component intervals; allowed range 0–0.5. |
| `DetailsInterval` | Time after the last successful details collection before another is due. |
| `DetailsTimeout` | Timeout for a details operation, using the same component slot. |
| `DetailsMaxAge` | Maximum accepted age of diagnostic details. |
| `ShutdownTimeout` | Upper bound on waiting for monitoring workers during host shutdown. |
| `AllowedOrigins` | Exact origins allowed on the early health branch; no wildcard matching or repository lookup. |

Durations must be positive, except `InitialJitter`, which may be zero. `PublisherMaxAge` must exceed `PublishInterval`. Component `MaxAge` must accommodate the interval including jitter, measurement timeout, details timeout and publication interval. `DetailsMaxAge` must also accommodate `DetailsInterval` plus that cycle. Invalid options fail during service initialization.

## Extension and migration

The passive interfaces are `IComponentHealthSource` and `IHealthSnapshotStore`. Active work belongs to `IComponentHealthCollector`, optionally with `IComponentHealthDetailsCollector`. Register exactly one collector for each of Database, BlobStorage, Search and Identity. Replace the corresponding collector registration rather than adding a second collector with the same name. Collectors must honor cancellation and return bounded, detached diagnostic data; the monitor copies results before publication.

The built-in collectors create a scope for each operation. Custom singleton collectors must not capture request-scoped services. Configuration data is serialized and credential fields are redacted in the background. Custom provider text should never contain secrets in the first place.

`IHealthHandler.GetHealthResponseAsync` remains available for callers that need a response object. This compatibility API parses a detached object from the stored JSON. The HTTP endpoint uses `IHealthSnapshotStore` directly and performs no such parsing. Replacing `IHealthHandler` alone no longer changes the endpoint; migrate custom health collection to the collector interfaces.

The `UseSenseNetHealth` before/after callbacks are retained on the authenticated branch. They do not run on the early branch. Avoid repository-dependent work in health callbacks. Existing apps need both middleware registrations to obtain the dependency-independent public/health-key path.

The implementation is in `src/Services.Core/Diagnostics`; the collection, concurrency, expiry and access tests are in `src/Tests/SenseNet.Services.Core.Tests/Health*Tests.cs`.
