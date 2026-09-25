# Repository-local authentication (SB-167)

`SenseNet.Authentication.Local` is an optional ASP.NET Core module. The default mode is
`Disabled`: it adds no endpoints or authentication schemes and leaves existing external
authentication unchanged. It does not require changes in the SNAuth service.

## Enable in a repository host

The dedicated `src/WebApps/SnWebApplication.Api.Sql.LocalAuth` host registers the module
from `sensenet:Authentication:Local`. The existing TokenAuth webapps and their Docker
images do not reference this module and remain unchanged. The dedicated host is a full
SQL repository API with local authentication, not a separate authentication server.
Its checked-in mode is `Disabled`; enable it explicitly in deployment configuration.

For a custom host, reference the module and call
`services.AddSenseNetLocalAuthentication(configuration)` **after** registering the external
scheme. Register an ILocalAuthenticationUserLock implementation that serializes each user
across all replicas of the repository. Missing lock registration fails startup. Call `app.UseSenseNetLocalAuthentication()` after CORS and before
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

`InternalOnly` skips external bearer registration in the dedicated host. `Secondary` retains
the external scheme (`Bearer` by default), alongside the local scheme. Clients must choose
a provider explicitly; there is no fallback to local authentication when external auth fails.
The unverified issuer is used only to route requests; the chosen scheme must validate the token.

Use a unique HTTPS issuer for each repository. Mount an RSA private PEM key (at least 2048 bits)
from a secret store outside the source tree. Do not commit a private key or put its contents in
appsettings. The module validates the configuration and signing capability at startup.

Both network and user/group allowlists are required. A match in either user or group list permits
the user, subject to enabled state and MFA. Secondary-mode administrators require registered MFA
even if `RequireMultiFactor` is false. Provision and test MFA before relying on break-glass access;
InternalOnly mode supports first-time enrollment after password verification; Secondary-mode
administrative access does not enroll a new authenticator.

## Build and deploy the dedicated SQL host

Build from the repository root; the Docker build context is `src`:

```sh
docker build -f src/WebApps/SnWebApplication.Api.Sql.LocalAuth/Dockerfile -t sensenetcsp/sn-api-sql-localauth:sb167 src
```

The image starts `SnWebApplication.Api.Sql.LocalAuth.dll`. Use a separate image name/tag
from the standard `sn-api-sql` images so normal releases are independent of this test.
The dedicated `Docker image - SenseNet SQL LocalAuth` GitHub Actions workflow uses
the image name `sensenetcsp/sn-api-sql-localauth` and the existing reusable builder.
It currently validates builds only; registry publishing is not enabled.
See [Docker image workflows](docker-image-workflows.md#dedicated-localauth-image).

Mount the signing key read-only and inject the SQL connection string, API keys and Local
configuration through deployment secrets/environment variables. No credentials are supplied
by the new launch profile. For local development use user secrets or environment variables;
the profile listens on `https://localhost:44372` and requires a trusted development certificate.

When replacing an existing repository container, preserve its database, index volumes,
repository URL, issuer, signing key and local-auth policy. Back up the effective Compose
configuration first and recreate only the selected repository service. Keep the Admin UI
separate and use a client version that supports SB-167 capability discovery.

Example Compose override (the base service supplies database and repository settings):

```yaml
services:
  snrepo:
    image: sensenetcsp/sn-api-sql-localauth:sb167
    pull_policy: never
    env_file:
      - ./local-auth.env
    volumes:
      - ./secrets/local-auth-private.pem:/run/secrets/local-auth-private.pem:ro
```

After deployment, verify `/authentication/capabilities`, an authenticated repository
operation, local login, refresh and logout. Restoring the previous image/configuration
rolls back the host integration; this feature introduces no database schema migration.

## Network and proxy policy

HTTPS and an identifiable client address are mandatory for login, refresh and revocation.
`TokenNetworks` restricts every local bearer request. If omitted, it inherits `LoginNetworks`;
an empty array denies every local bearer request. IPv4-mapped IPv6 addresses are normalized.

CIDRs are explicit: `127.0.0.0/8`, `::1/128`, an office/VPN subnet, or deliberately
`0.0.0.0/0` and `::/0`. No LAN/VPN classification is inferred. To deliberately disable IP restriction in a test
deployment, set **both** `LoginNetworks` and `TokenNetworks` to
`["0.0.0.0/0", "::/0"]`. To re-enable it, replace those entries in both lists with
the actual allowed client/VPN CIDRs and recreate the dedicated host. Do not broaden
`KnownProxies`: proxy trust is separate from client access. A VPN-only policy requires
traffic to actually pass through the VPN, not merely an active VPN connection.

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
| POST | /authentication/local/login | username, password, optional twoFactorCode; requestMfaChallenge enables progressive MFA |
| POST | /authentication/local/mfa | challengeToken, twoFactorCode; session only after verification |
| POST | /authentication/local/forgot-password | email; generic 202, only when recovery enabled |
| POST | /authentication/local/reset-password | token, password; 204 on success, generic 400 otherwise |
| POST | /authentication/local/refresh | refreshToken |
| POST | /authentication/local/logout | refreshToken; idempotent 204 |
| POST | /authentication/local/revoke | refreshToken; idempotent 204 |

POST bodies must be JSON and at most 8192 characters. Login and refresh return
`{ accessToken, refreshToken, expiresIn, tokenType: "Bearer" }`. Failures are generic;
429 includes Retry-After. Legacy login accepts an MFA code together with credentials. Progressive login returns
202 with { challengeToken, expiresIn, manualEntryKey?, qrCodeSetupImageUrl? } only after
successful password verification; all unauthenticated failures remain generic.

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

## Login appearance, recovery and MFA

The **Admin UI renders the local login page** using native HTML/CSS, without Material UI
controls. The repository supplies public appearance options in
`GET /authentication/capabilities`; it does not serve or bundle the Admin UI.
SNAuth continues to render its own external-provider login independently.

Configure each repository under `sensenet:Authentication:Local`:

```json
{
  "Appearance": {
    "Title": "Login to My Repository",
    "BackgroundImageUrl": "https://assets.example/login-background.png",
    "LogoUrl": "https://assets.example/logo.svg",
    "BackgroundColor": "#d4f3fa",
    "BrandColor": "#38a9cb",
    "ButtonColor": "#38a9cb",
    "ButtonTextColor": "#ffffff",
    "TextColor": "#343b43",
    "PanelColor": "#ffffff"
  },
  "PasswordRecovery": {
    "Enabled": true,
    "ResetUrl": "https://admin.example/",
    "TokenLifetime": "00:15:00",
    "MinimumPasswordLength": 12,
    "RequireSmtpTls": true
  }
}
```

Colors accept six- or eight-digit hex values. `BrandColor` colors the left logo panel;
`PanelColor` colors the login/form panel. Images must use absolute HTTPS URLs without
credentials and be accessible before login, e.g. public static assets. Leave image URLs
unset to use the bundled SNAuth mountain background and sensenet logo.
Restart the repository and reload Admin UI after configuration changes; no UI rebuild is needed.

Docker environment examples:

```dotenv
sensenet__Authentication__Local__Appearance__Title=Login to My Repository
sensenet__Authentication__Local__Appearance__ButtonColor=#38a9cb
sensenet__Authentication__Local__Appearance__PanelColor=#ffffff
sensenet__Authentication__Local__Appearance__BackgroundImageUrl=https://assets.example/background.png
sensenet__Authentication__Local__PasswordRecovery__Enabled=true
sensenet__Authentication__Local__PasswordRecovery__ResetUrl=https://admin.example/
```

### Forgotten password

Recovery is opt-in and uses the repository's existing `sensenet:Email` settings:
`Server`, `Port`, `FromAddress`, `SenderName`, `Username`, `Password`.
The isolated `ILocalPasswordResetSender` SMTP implementation validates TLS certificates,
requires STARTTLS (or TLS on port 465), propagates delivery errors and bounds send time.
A custom host can replace that interface for a transactional mail provider.
For a private, disposable Mailpit test only, explicitly set
`PasswordRecovery:RequireSmtpTls=false`; do not use that setting for public SMTP.

The account must be enabled, allowed by the local-auth user/group policy, and have a
unique Email value. The response is the same for unknown, disallowed, disabled and
undeliverable accounts. Duplicate email addresses do not receive a reset link.
The trusted `ResetUrl` is configured on the server, never accepted from a request.
For a local Admin UI, `http://localhost:8080/` is permitted; open the email on
the machine running that UI. Production URLs must use HTTPS.

The link includes the repository issuer as `repoUrl` and a random token in the URL
fragment. Admin UI immediately removes the fragment from the address bar and retains
it only in memory. It is not sent in page requests, referrers, or browser storage.
Only SHA-256 token digests are stored in the database. Links expire (15 minutes by
default), are single-use, and newer requests invalidate older links. A failed mail
delivery also invalidates the newly issued token.

Reset requires at least 12 password characters (configurable minimum, up to 128);
repository password validation still applies. It revokes all of the user's local-auth
sessions and pending MFA/reset challenges, without deleting API keys or MFA enrollment.
The user must sign in again, including the authenticator code when MFA is enabled.
It does not reset the authenticator or log the user in automatically.

### Authenticator enrollment and sign-in

Enable the account's existing `MultiFactorEnabled` field, or configure the repository's
existing forced MFA policy. `RequireMultiFactor` is an additional local access condition;
it does not silently change user settings. A user with MFA disabled cannot sign in when
this condition is true.

New clients send `requestMfaChallenge:true` to the login endpoint. After a valid password,
an MFA-enabled account receives HTTP 202 with a five-minute challenge, **not a session**.
An unregistered account in InternalOnly mode also receives the QR/manual setup key.
Add it to an authenticator app and enter the current code. The first valid code registers
the authenticator and issues the session. Subsequent logins request only the code and
never expose the setup key again. Secondary-mode administrative break-glass accounts
must already be enrolled.

Challenges are hashed in persistent storage and can be completed on another repository
replica. Consumption, enrollment and password changes are serialized with the repository's
`ILocalAuthenticationUserLock`; the dedicated SQL host uses database-scoped sp_getapplock
on a dedicated unpooled connection, released by disconnect. It does not use the legacy
ExclusiveLocks provider/table. Custom hosts must register an equivalent cross-replica lock. Each challenge succeeds
once. Account/IP throttling applies to MFA attempts as well. Existing single-request
username/password/twoFactorCode clients remain compatible.

### Manual acceptance checks

1. Open the separate SB-167 Admin UI with `?repoUrl=https%3A%2F%2Frepository.example`.
   Check custom colors/logo/background and a narrow mobile viewport.
2. Log in with an allowed non-MFA test account; verify content access, refresh and logout.
3. Enable MFA on a disposable test account, sign in, scan its QR, then enter a valid
   authenticator code. Log out and verify the next login requests a code without showing
   the enrollment secret. A wrong/expired code must not create a session.
4. Set that account's unique email. Request a reset, open the received link, and choose
   a new password. The old password and sessions must fail; a second use of the same
   link must fail; the new password must still require the enrolled MFA code.
5. Unknown-email requests must return the same confirmation. Inspect only operation/status
   logs; passwords, reset links, authenticator secrets and tokens must never be logged.

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
dotnet build src/WebApps/SnWebApplication.Api.Sql.LocalAuth/SnWebApplication.Api.Sql.LocalAuth.csproj -p:LangVersion=12.0
```

C# 12 avoids an unrelated existing `paths.Reverse()` overload conflict when this repository
is built with SDK 10 and its current LangVersion=latest setting. The production Dockerfiles use SDK 8.

Tests exercise real InMemory repository credentials, session storage, JWT validation, refresh,
revocation, allowlists, disabled users, real TOTP enrollment, challenge expiry/replay, recovery
concurrency, SMTP failure, preserved MFA, proxy spoofing, throttling, and key rotation.
The provider-neutral LocalAuthenticationTestCases lifecycle also passes with InMemPlatform.
MsSqlLocalAuthenticationTests is compiled and ready to run with the existing integration-test
connection-string setup; run it only against a disposable database. An MSSQL/PostgreSQL deployment
must additionally run the lifecycle against its disposable
database. PostgreSQL provider sources are not on the develop base used for this feature.
Admin UI and .NET client integration are separate parts of SB-167 and are not provided by this package.

Set SB167_TEST_SQL to a disposable SQL Server connection string to run the SQL lock test
and all auth flows with the dedicated host's real application lock implementation.
Without it, auth tests use an in-process test lock and the SQL-specific test is skipped.
Never point these tests at a shared/live repository database.
