# Repository-local authentication (SB-167)

`SenseNet.Authentication.Local` is an optional ASP.NET Core module. The default mode is
`Disabled`: it adds no endpoints or authentication schemes and leaves existing external
authentication unchanged. It does not require changes in the SNAuth service.

## Enable in a repository host

The six TokenAuth sample webapps already register the module from
`sensenet:Authentication:Local`. For a custom host, reference the module and call
`services.AddSenseNetLocalAuthentication(configuration)` **after** registering the external
scheme. Call `app.UseSenseNetLocalAuthentication()` after CORS and before
`app.UseSenseNetAuthentication()`.

Example configuration (replace paths, identifiers and networks with deployment values):

```json
{
  "sensenet": {
    "Authentication": {
      "AddJwtCookie": false,
      "Local": {
        "Mode": "InternalOnly",
        "Issuer": "https://repository.example",
        "Audience": "sensenet",
        "SigningKeyPath": "/run/secrets/local-auth-private.pem",
        "SigningKeyId": "local-2026-09",
        "LoginNetworks": [ "192.0.2.0/24" ],
        "TokenNetworks": [ "192.0.2.0/24" ],
        "AllowedGroupIds": [ 12345 ],
        "RequireMultiFactor": true,
        "AccessTokenLifetime": "00:05:00",
        "SessionLifetime": "08:00:00"
      }
    }
  }
}
```

`InternalOnly` skips external bearer registration in the sample hosts. `Secondary` retains
the external scheme (`Bearer` by default), alongside the local scheme. Clients must choose
a provider explicitly; there is no fallback to local authentication when external auth fails.
The unverified issuer is used only to route requests; the chosen scheme must validate the token.

Use a unique HTTPS issuer for each repository. Mount an RSA private PEM key (at least 2048 bits)
from a secret store outside the source tree. Do not commit a private key or put its contents in
appsettings. The module validates the configuration and signing capability at startup.

Both network and user/group allowlists are required. A match in either user or group list permits
the user, subject to enabled state and MFA. Secondary-mode administrators require registered MFA
even if `RequireMultiFactor` is false. Provision and test MFA before relying on break-glass access;
this endpoint does not enroll a new authenticator.

## Network and proxy policy

HTTPS and an identifiable client address are mandatory for login, refresh and revocation.
`TokenNetworks` restricts every local bearer request. If omitted, it inherits `LoginNetworks`;
an empty array denies every local bearer request. IPv4-mapped IPv6 addresses are normalized.

CIDRs are explicit: `127.0.0.0/8`, `::1/128`, an office/VPN subnet, or deliberately
`0.0.0.0/0` and `::/0`. No LAN/VPN classification is inferred.

Behind a proxy, configure `KnownProxies` or `KnownNetworks` and optionally `ForwardLimit`
(default 1). Only these proxies can supply matching X-Forwarded-For/X-Forwarded-Proto pairs.
No default loopback proxy is implicitly trusted. Unconsumed forwarded headers are rejected by
the local policy; a proxy must remove client-supplied forwarding headers before creating its own.
Do not enable a separate middleware or hosting setting that trusts forwarding from every peer.

IP/account limits default to 30/5 attempts per minute. Username variants are also limited by
resolved repository user ID. The bounded limiter is per application process. For multiple replicas,
enforce aggregate limits at the trusted ingress too; restarting a process resets its counters.

## API and client contract

All responses set Cache-Control: no-store. Credentials and tokens must never be logged.

| Method | Path | Body/result |
| --- | --- | --- |
| GET | /authentication/capabilities | mode, local endpoints if this request is eligible, external authority in Secondary |
| POST | /authentication/local/login | username, password, optional twoFactorCode |
| POST | /authentication/local/refresh | refreshToken |
| POST | /authentication/local/logout | refreshToken; idempotent 204 |
| POST | /authentication/local/revoke | refreshToken; idempotent 204 |

POST bodies must be JSON and at most 8192 characters. Login and refresh return
`{ accessToken, refreshToken, expiresIn, tokenType: "Bearer" }`. Failures are generic;
429 includes Retry-After. Login accepts an MFA code together with credentials and does not
disclose whether an account exists or has MFA.

Send access tokens in the Authorization bearer header. Cookie authentication is intentionally
not supported: enabling this module with `AddJwtCookie=true` fails startup, so the legacy
JWT cookie middleware cannot introduce an unprotected CSRF path. Browser clients must use
authenticated fetch for binary downloads as well.

Capabilities contain neither secrets nor allowlists. Disabled mode exposes no module endpoints;
clients may treat 404 as a legacy repository, but must not interpret an auth service error as
permission to switch providers. Store sessions per repository and provider, handle refresh and
session loss, and call only the chosen issuer's logout endpoint. Human passwords belong only in
the explicit login call, never persistent service configuration. Machine clients should use
existing API keys or client credentials; local M2M credentials are not implemented.

## Session persistence and revocation

The module uses `IAccessTokenDataProvider` with feature `local-auth-session`.
Only a SHA-256 digest of the 384-bit random refresh token is stored. JWTs contain the repository
user ID (`sub`) and session digest (`sid`). Each bearer validation reads session state and
rechecks the enabled user and allowlist; it never contacts an external authenticator.

Refresh tokens have an absolute session lifetime (default eight hours, maximum one day).
Refresh does not extend it and returns the same refresh token with a new short-lived access token.
Sessions can therefore be used across replicas without in-process refresh-rotation races.
Logout/revoke deletes the stored session; subsequent access and refresh requests fail on every
replica. A request already authenticated before revocation may complete.

To revoke a lost device's sessions administratively, use an authorized repository operation to
delete that user's tokens for feature `local-auth-session` through the data provider. Never delete
all access tokens indiscriminately: the same table also holds API keys and MFA material.

## Rotation and break-glass operations

1. Provision the replacement private key through the secret store and assign a new unique kid.
2. Add the previous public PEM key under `PreviousKeys`, with `Path` and its original `KeyId`.
3. Deploy the configuration consistently to all replicas. New access tokens use the new kid;
   existing access tokens remain valid while their session and previous key remain valid.
4. After the maximum access-token lifetime has elapsed, remove the previous public key.
   Refresh sessions do not depend on the old signing key.
5. For a compromised key, remove its public key immediately and revoke affected sessions.

Before an outage, verify the allowed network path, a dedicated break-glass group, MFA enrollment,
and a local login while the external service is unavailable. After use, revoke the session and
review the structured local authentication login/refresh/logout/revoke audit events.
The module logs operation and status without credential values.

## Validation and current boundaries

Run:

```powershell
dotnet test src/Tests/SenseNet.Authentication.Local.Tests/SenseNet.Authentication.Local.Tests.csproj -p:LangVersion=12.0
dotnet build src/WebApps/SnWebApplication.Api.InMem.TokenAuth/SnWebApplication.Api.InMem.TokenAuth.csproj -p:LangVersion=12.0
```

C# 12 avoids an unrelated existing `paths.Reverse()` overload conflict when this repository
is built with SDK 10 and its current LangVersion=latest setting. The production Dockerfiles use SDK 8.

Tests exercise real InMemory repository credentials, session storage, JWT validation, refresh,
revocation, allowlists, disabled users, MFA, proxy spoofing, throttling, and key rotation.
The provider-neutral LocalAuthenticationTestCases lifecycle also passes with InMemPlatform.
MsSqlLocalAuthenticationTests is compiled and ready to run with the existing integration-test
connection-string setup; run it only against a disposable database. An MSSQL/PostgreSQL deployment
must additionally run the lifecycle against its disposable
database. PostgreSQL provider sources are not on the develop base used for this feature.
Admin UI and .NET client integration are separate parts of SB-167 and are not provided by this package.
