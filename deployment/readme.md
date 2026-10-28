# Local Docker demos: Compose or PowerShell

These are local demos for trying sensenet. The 26 demo scenarios in the tables below each use one `-f` argument and have distinct default project names and published ports, so different demos can run side by side without configuration. Databases, networks, RabbitMQ and named volumes belong to their own project. Host-database scenarios still require an existing SQL server. This provider checkout includes two PostgreSQL demos; the older `docker-compose.postgres.yml` and `docker-compose.mssql.yml` developer stacks are separate and retain their existing ports and reset/tools workflows (see the final section).

The files share service definitions through `compose.services.yml`; no generator or installer script runs when Compose starts. Docker Compose v2.24 or newer is required. Published ports bind to `127.0.0.1`. The PowerShell installer's historical ports are available through the optional `legacy-ports.env` file.

Prepare once from the repository root, using PowerShell 7.2+:

```powershell
Set-Location .\deployment
if (!(Test-Path .env)) { Copy-Item .env.template .env }
./scripts/create-devcert.ps1 -CertPsw 'SuP3rS3CuR3P4sSw0Rd'
./scripts/create-localauth-key.ps1
```

The certificate helper exports/trusts the local .NET developer certificate. If you provide a certificate yourself, place `snapp.pfx` in `temp/certificates`, or set `CERT_FOLDER` and `CERT_PASSWORD` in `.env`. LocalAuth also requires `signing-key.pem` in `temp/localauth` (or `LOCAL_AUTH_KEY_FOLDER`). A small, network-disabled `prerequisites` container checks these required files before any service starts, including on Docker Desktop where an absent bind-file source can become an empty directory. A missing certificate/key stops startup instead of putting the app in a restart loop. Read the error with `docker compose -f <file> logs prerequisites`, run the named helper, then repeat `up`. If a missing file has become a directory, remove that empty directory before running the helper. The first database installation may take a few minutes; `db-init` exiting with code 0 means success.

Set `ADMIN_UI_URL` to the origin of the Admin UI you use; its template value is a placeholder. `.env`, certificates and generated signing keys are ignored by Git. The default admin login is `admin` / `admin`; passwords/API keys are public demo values.

To log in and browse repository content:

1. Open the [Admin UI](https://admin.sensenet.com).
2. Enter the repository URL from the command table, for example `https://localhost:18016` for `docker-compose.yml`.
3. Log in with `admin` / `admin` on the authentication page opened by Admin UI. For the default demo its IdentityServer is at `https://localhost:18017`.

The repository URLs in the tables are REST API endpoints. Opening their root directly shows `sensenet is listening`; that is normal and does not display the Admin UI. The examples use `localhost`. IdentityServer demos also accept `127.0.0.1` as the repository hostname and route that alias to the same internal repository; the authentication server's canonical URL remains `localhost`.

For webapps present in the current checkout, `up -d --build` builds the local source instead. An existing local floating image tag may be older than the registry tag; `--pull always` refreshes published images. The LocalAuth scenario always needs its local build.

Start commands for PowerShell (run from `deployment/`). Each row is one independent command; select the demo you want. The repository base URL is `https://localhost:<API port>`. LocalAuth's login page is `/authentication/local/login` on that same port.

| Demo | PowerShell command | API port |
| --- | --- | --- |
| SQL + IdentityServer | `docker compose -f docker-compose.yml up -d --pull always` | 18016 |
| SQL + SNAuth | `docker compose -f docker-compose.with-auth.yml up -d --pull always` | 18026 |
| SQL + IdentityServer, persistent data | `docker compose -f docker-compose.with-volume.yml up -d --pull always` | 18036 |
| SQL + SNAuth, persistent data | `docker compose -f docker-compose.with-volume-auth.yml up -d --pull always` | 18046 |
| SQL + search service + IdentityServer | `docker compose -f docker-compose.with-search.yml up -d --pull always` | 18056 |
| SQL + search service + SNAuth | `docker compose -f docker-compose.with-search-auth.yml up -d --pull always` | 18066 |
| SQL + search service + IdentityServer, persistent data | `docker compose -f docker-compose.complete.yml up -d --pull always` | 18076 |
| SQL + search service + SNAuth, persistent data | `docker compose -f docker-compose.complete-auth.yml up -d --pull always` | 18086 |
| In-memory + IdentityServer | `docker compose -f docker-compose.in-memory.yml up -d --pull always` | 18176 |
| In-memory + SNAuth | `docker compose -f docker-compose.in-memory-auth.yml up -d --pull always` | 18186 |
| SQL + document previews + SNAuth | `docker compose -f docker-compose.preview.yml up -d --pull always` | 18196 |
| SQL + search service + document previews + SNAuth | `docker compose -f docker-compose.preview-search.yml up -d --pull always` | 18206 |
| SQL + LocalAuth | `docker compose -f docker-compose.local-auth.yml up -d --build` | 18216 |
| SQL + LocalAuth, persistent data | `docker compose -f docker-compose.local-auth-volume.yml up -d --build` | 18226 |
| PostgreSQL + SNAuth, local source | `docker compose -f docker-compose.demo-postgres.yml up -d --build` | 18256 |
| PostgreSQL + SNAuth, persistent data, local source | `docker compose -f docker-compose.demo-postgres-volume.yml up -d --build` | 18266 |

The following demos use an existing SQL server. Set `DATASOURCE`, `SQL_USER` and `SQL_PASSWORD` in `.env` before starting one:

| Demo | PowerShell command | API port |
| --- | --- | --- |
| Host SQL + IdentityServer | `docker compose -f docker-compose.host-db.yml up -d --pull always` | 18096 |
| Host SQL + SNAuth | `docker compose -f docker-compose.host-db-auth.yml up -d --pull always` | 18106 |
| Host SQL + IdentityServer, persistent data | `docker compose -f docker-compose.host-db-volume.yml up -d --pull always` | 18116 |
| Host SQL + SNAuth, persistent data | `docker compose -f docker-compose.host-db-volume-auth.yml up -d --pull always` | 18126 |
| Host SQL + search service + IdentityServer | `docker compose -f docker-compose.host-db-search.yml up -d --pull always` | 18136 |
| Host SQL + search service + SNAuth | `docker compose -f docker-compose.host-db-search-auth.yml up -d --pull always` | 18146 |
| Host SQL + search service + IdentityServer, persistent data | `docker compose -f docker-compose.host-db-volume-search.yml up -d --pull always` | 18156 |
| Host SQL + search service + SNAuth, persistent data | `docker compose -f docker-compose.host-db-volume-search-auth.yml up -d --pull always` | 18166 |
| Host SQL + LocalAuth | `docker compose -f docker-compose.local-auth-host-db.yml up -d --build` | 18236 |
| Host SQL + LocalAuth, persistent data | `docker compose -f docker-compose.local-auth-host-db-volume.yml up -d --build` | 18246 |

To stop a demo while keeping its containers for the next start, for example:

```powershell
docker compose -f docker-compose.local-auth.yml stop
```

All default service ports (each scenario can run alongside the other scenarios):

| Compose file | Repository | Auth | Search | DB | Rabbit management |
| --- | --- | --- | --- | --- | --- |
| `docker-compose.yml` | 18016 | 18017 | - | 18019 | - |
| `docker-compose.with-auth.yml` | 18026 | 18027 | - | 18029 | - |
| `docker-compose.with-volume.yml` | 18036 | 18037 | - | 18039 | - |
| `docker-compose.with-volume-auth.yml` | 18046 | 18047 | - | 18049 | - |
| `docker-compose.with-search.yml` | 18056 | 18057 | 18058 | 18059 | 18055 |
| `docker-compose.with-search-auth.yml` | 18066 | 18067 | 18068 | 18069 | 18065 |
| `docker-compose.complete.yml` | 18076 | 18077 | 18078 | 18079 | 18075 |
| `docker-compose.complete-auth.yml` | 18086 | 18087 | 18088 | 18089 | 18085 |
| `docker-compose.host-db.yml` | 18096 | 18097 | - | Host | - |
| `docker-compose.host-db-auth.yml` | 18106 | 18107 | - | Host | - |
| `docker-compose.host-db-volume.yml` | 18116 | 18117 | - | Host | - |
| `docker-compose.host-db-volume-auth.yml` | 18126 | 18127 | - | Host | - |
| `docker-compose.host-db-search.yml` | 18136 | 18137 | 18138 | Host | 18135 |
| `docker-compose.host-db-search-auth.yml` | 18146 | 18147 | 18148 | Host | 18145 |
| `docker-compose.host-db-volume-search.yml` | 18156 | 18157 | 18158 | Host | 18155 |
| `docker-compose.host-db-volume-search-auth.yml` | 18166 | 18167 | 18168 | Host | 18165 |
| `docker-compose.in-memory.yml` | 18176 | 18177 | - | - | - |
| `docker-compose.in-memory-auth.yml` | 18186 | 18187 | - | - | - |
| `docker-compose.preview.yml` | 18196 | 18197 | - | 18199 | - |
| `docker-compose.preview-search.yml` | 18206 | 18207 | 18208 | 18209 | 18205 |
| `docker-compose.local-auth.yml` | 18216 | Internal | - | 18219 | - |
| `docker-compose.local-auth-volume.yml` | 18226 | Internal | - | 18229 | - |
| `docker-compose.local-auth-host-db.yml` | 18236 | Internal | - | Host | - |
| `docker-compose.local-auth-host-db-volume.yml` | 18246 | Internal | - | Host | - |
| `docker-compose.demo-postgres.yml` | 18256 | 18257 | - | 18259 | - |
| `docker-compose.demo-postgres-volume.yml` | 18266 | 18267 | - | 18269 | - |

Each scenario reserves a ten-port block: repository at `+6`, auth at `+7`, search at `+8`, container DB at `+9`, RabbitMQ management at `+5`. AMQP stays inside the Compose network. Optional `REPOSITORY_PORT`, `AUTH_PORT`, `SEARCH_PORT`, `SQL_PORT` and `RABBIT_PORT` overrides also update the application/auth URLs. Leave these unset in a shared `.env` to retain the non-conflicting defaults. To run multiple copies of the *same* scenario, use distinct `-p` project names and distinct overrides for all its published ports.

If you copied `.env.template` from an earlier revision, remove its `SQL_PORT=9999`, `POSTGRES_PORT=5532` and `RABBIT_PORT=51005` lines to enable the new defaults. Select `legacy-ports.env` explicitly when you want those old ports.

Stop a demo with the same file (`docker compose -f <file> down`); adding `--volumes` also deletes its persistent data. Other projects keep running. Use this Compose cleanup for Compose resources, rather than the PowerShell uninstaller. Projects created by earlier revisions keep their original names; use their original `-p` name when addressing them. New default names do not adopt or migrate those containers.

For the historical PowerShell port layout, select the compatibility file explicitly:

```powershell
docker compose --env-file .env --env-file legacy-ports.env -f docker-compose.with-auth.yml up -d
# Use the same arguments for cleanup:
docker compose --env-file .env --env-file legacy-ports.env -f docker-compose.with-auth.yml down
```

The second env file changes ports only. In compatibility mode, SQL containers publish 9999 and RabbitMQ management 51005. Scenarios using the same historical API/DB/Rabbit ports must run one at a time. The following table shows the historical SQL port calculation; it is also what the PowerShell installer continues to use:

| Database | Persistent data | Search service | Repository/auth/search ports | IdentityServer file | SNAuth file |
| --- | --- | --- | --- | --- | --- |
| Container | No | No | 51016 / 51017 | `docker-compose.yml` | `docker-compose.with-auth.yml` |
| Host | No | No | 51026 / 51027 | `docker-compose.host-db.yml` | `docker-compose.host-db-auth.yml` |
| Container | Yes | No | 51036 / 51037 | `docker-compose.with-volume.yml` | `docker-compose.with-volume-auth.yml` |
| Host | Yes | No | 51046 / 51047 | `docker-compose.host-db-volume.yml` | `docker-compose.host-db-volume-auth.yml` |
| Container | No | Yes | 51056 / 51057 / 51058 | `docker-compose.with-search.yml` | `docker-compose.with-search-auth.yml` |
| Host | No | Yes | 51066 / 51067 / 51068 | `docker-compose.host-db-search.yml` | `docker-compose.host-db-search-auth.yml` |
| Container | Yes | Yes | 51076 / 51077 / 51078 | `docker-compose.complete.yml` | `docker-compose.complete-auth.yml` |
| Host | Yes | Yes | 51086 / 51087 / 51088 | `docker-compose.host-db-volume-search.yml` | `docker-compose.host-db-volume-search-auth.yml` |

In-memory compatibility ports are 51011/51012. LocalAuth and Preview compatibility ports follow their matching SQL row (LocalAuth has no external auth port). The older PowerShell installer shares its `sensenet-rabbit` container; Compose demos each own a RabbitMQ container. The installer checks its exact `sensenet` network name, so existing Compose networks do not prevent it from starting.

For host SQL, configure `DATASOURCE`, `SQL_USER` and `SQL_PASSWORD` in `.env`. The login needs database-creation permission. The default Docker host address is `host.docker.internal`; an explicit `host-gateway` mapping also supports Linux hosts. Host scenarios create an empty database idempotently but never start or reset your SQL server.

The `*.Admin` projects and `WebHookHandler` have no Docker image definitions in this repository. `Sql.TokenAuth.NLB` uses an in-process index and is separate from the published `sn-api-nlb` image, which is built from `Sql.SearchService.TokenAuth`.

LocalAuth is build-only in CI. Prepare its RSA key with PowerShell 7.2+, then build locally:

```powershell
./scripts/create-localauth-key.ps1
docker compose -f docker-compose.local-auth.yml up -d --build
# Equivalent installer, building the local checkout:
./install-sensenet.ps1 -WebApp LocalAuth -CreateImages -LocalSn
```

This demo enables `InternalOnly`, bearer transport, the built-in admin user (Id=1), and private/loopback client networks. It explicitly disables mandatory MFA for the demo account. The login page is `/authentication/local/login`; never reuse the generated signing key or these demo settings for a hosted installation. Data persistence does not control key persistence: keep the same ignored key when restarting an existing demo.

PostgreSQL demos are available when this checkout contains `SnWebApplication.Api.PostgreSql.TokenAuth`. The separate `docker-compose.demo-postgres*.yml` files support local source builds, use ports 18256/18266 for the API, and keep the original `docker-compose.postgres.yml` developer entry point intact. Use `--build` to test local changes without publishing an image; `POSTGRES_IMAGE` controls its local image tag. The PowerShell equivalent is `./install-sensenet.ps1 -WebApp PostgreSql -CreateImages -LocalSn`. A checkout without the PostgreSQL webapp rejects PostgreSQL selection before Docker/Git operations. PostgreSQL host-database/search-service and LocalAuth search-service combinations remain unavailable.

`-WebApp Preview` selects the `sn-api-sql-prv` / `sn-api-nlb-prv` images. `-ImageTag` defaults to `preview` and is used consistently for local build and run. `-SensenetDockerImage` selects an explicit application image. `-CreateImages` also builds the selected external auth server (SNAuth or IdentityServer), and the installer reuses those builds without pulling over them. Repeated installation starts an existing stopped container and keeps a running container. Changed image/environment/volume settings require explicit recreation; repeated installation does not silently replace existing configuration or delete unusual container states.

Validate the deployment changes without starting services:

```powershell
pwsh -NoProfile -File ./tests/Test-Deployment.ps1
```

Opt-in runtime tests create isolated, randomly named Compose projects, a temporary certificate/key without changing the trust store, and fresh databases. They validate OData, admin login, an authenticated admin read, repeated `up`, and restart, then remove only their own containers/volumes. `Parallel` starts LocalAuth and SNAuth together on their default ports, checks that either can restart while the other keeps serving authenticated requests, and verifies missing-key/certificate startup errors. The PostgreSQL volume test also checks the configured Admin UI Origin and recreates the whole stack, signs in again (SNAuth sessions are in memory), and verifies that a created content item keeps the same Id. The selected scenarios' default ports must be free:

```powershell
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario WithAuth
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario LocalAuth
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Postgres
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Postgres -UseVolume
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Search
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Parallel
# Optional historical-port runtime check; historical ports must be free:
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario InMemory -LegacyPorts
```

For repeatable PostgreSQL SQL-contract, fresh-install and baseline-to-current upgrade checks, see [PostgreSQL provider validation](../docs/postgres-provider-validation.md).

For an already running IdentityServer demo, test the exact repository URL and Admin UI origin involved in a login failure:

```powershell
pwsh -NoProfile -File ./tests/Test-IdentityServerLogin.ps1 -RepositoryUrl https://127.0.0.1:18016 -AdminUiOrigin https://admin.example.invalid
```

Replace the example origin with your actual Admin UI origin. This test checks the client bootstrap, discovery/signing-key/token CORS responses, the login form with its anti-forgery cookie, authorization-code/PKCE exchange, and an authenticated Admin read. It does not follow the external redirect or print credentials, cookies, authorization codes or tokens. It checks the HTTP authentication flow; browser-specific permissions and existing browser session state require a separate browser check.

# Install sensenet on local Docker with PowerShell

This article describes how to install sensenet in a docker environment on your local machine. It is a Powershell script that will install sensenet from Docker images. It will create a Docker network and start the necessary containers.

## Prerequisites
Before you begin, please take a look at the prerequisites.

### Docker 

The installer uses the docker cli and works with docker containers. You have to have docker installed on your machine.

By default the installer uses the publicly available sensenet Docker images.

### Powershell 

As this installer is built of Powershell scripts you have to have Powershell installed on your machine. 

The installer is built of multiple scripts. By default Windows will ask confirmation before running every script, so it is recommended to set the _ExecutionPolicy_ for the current process:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass 
```

This change on process scope will only affect the current powershell session. To learn more see the documentation on [execution policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7.3#example-6-set-the-execution-policy-for-the-current-powershell-session).
	
### dotnet cli OR a valid certificate

Sensenet services will use the `snapp.pfx` certificate file from under `./temp/certificates` folder. If it is not present the installer will create a developer certificate in this folder and it uses dotnet cli for this. On linux the dotnet `--trust` switch won't work. so you have to either create or trust the created certificate manually.

### git cli (in case of source install)
By default the installer uses the publicly available sensenet Docker images. In case you want to create the Docker images from source code, you will have to have the _git cli_ installed. The _CreateImages_ switch will download the necessary git repositories in order to create Docker images from sensenet service solutions on-the-fly and it uses the git cli for this.

## Simple install

The script is preconfigured so you don't have to know every switch if you just want to try it out. With the preconfigured settings the installer will create an IdentityServer, an MS SQL Server and a sensenet application container. Certificates are mounted read-only before the processes start. Repository/database/search data is persisted only when the selected scenario requests it.

To install sensenet, execute the following script:

```powershell
.\install-sensenet.ps1 
```

After the script completes, your repository will be accessible on the URL displayed by the script. In case of the default installation this is `https://localhost:51016` but it may be different in case of in-memory or search service scenarios. 

You can log in by visiting [https://admin.sensenet.com](https://admin.sensenet.com) and providing the repository url. The default credentials are:

```text
username: admin
password: admin
```

> Using the `-OpenInBrowser` switch will automatically open the admin ui in your default browser.

## Parameters
You can customize the installation with the following parameters:

### InMemPlatform 

By default the installer will create a sensenet repository that uses an MS SQL database, but there is an in memory sensenet repository that does not need a physical database. This version will start an identity server and a sensenet application container only.

```powershell
.\install-sensenet.ps1 -InMemPlatform
```

### CreateImages

The `-CreateImages` switch will download the source code of the necessary repositories from GitHub and create the appropriate Docker images for sensenet services. For example if you executed the installer with the `-SearchService` switch, it will create the appropriate sensenet application Docker image and the search service image as well, and so on. These temporary folders will be created under the `./temp` folder and the images will be created on the host Docker registry.

```powershell
.\install-sensenet.ps1 -CreateImages
```

### SearchService

Sample sensenet set for NLB environments. Indexing is not handled by the sensenet application but by a separate search service. It will start the following containers:

- IdentityServer
- MS SQL Server
- Search service
- sensenet application
- RabbitMQ service for messaging

### HostDb with DataSource, SqlUser and SqlPsw

You can try this sensenet demo with your local database on the host machine. The `-HostDb` switch will initiate this mode. However you will need a SQL user on your MS SQL Server at least with database creation permission. The script will automatically use the host name as datasource. If your MS SQL Server name is different from your host name, you will have to set it with the `-DataSource` parameter. You can also set the `-SqlUser` and `-SqlPsw` parameters when running the installer otherwise it will ask for these at run time. For example:

```powershell
.\install-sensenet.ps1 -DataSource MyMachineHostName\Sql2019 -HostDb -SqlUser testuserfordockerdemo -SqlPsw Ultr4Secur3P4ssw0rd 
```
### UseVolume and VolumeBasePath

This switch persists database/repository/search data through bind mounts. Without it, data remains inside the containers; certificates are mounted read-only in both modes. For Linux containers on Docker Desktop, Lucene data must live on a Linux filesystem because Windows bind mounts handle file locks differently. The Compose volume scenarios above use Docker named volumes for this reason. With the PowerShell installer, choose a Linux-backed `VolumeBasePath` when needed.

While on linux it is enough to call 

```powershell
.\install-sensenet.ps1 -UseVolume
```

on Windows it should be something like this:

```powershell
.\install-sensenet.ps1 -UseVolume -VolumeBasePath /var/lib/docker/volumes
```

> The example above will bind the WSL paths with the containers.

### OpenInBrowser

With this switch the installer will open the admin ui when the repository is created.

### Uninstall

This switch is responsible for cleanup after an installation. You should use the same switches as with the install to remove the same resources.

### DryRun

This switch can be used to see what processes would be executed but without actually running them.

### Verbose

The output is reduced by default to decrease the amount of install information. With the `-Verbose` switch all the additional technical information will be shown. For example the actual Docker command is shown that can be useful if you need to customize the installation.

---

## Docker Compose Setup (Linux / macOS)

In addition to the PowerShell installer above, there are two **docker-compose** files for running sensenet locally with either **PostgreSQL** or **MSSQL**. These are self-contained and don't require the PowerShell scripts.

### Prerequisites

- Docker & Docker Compose v2+
- A dev certificate at `./volumes/certificates/snapp.pfx` (generated once):

```bash
mkdir -p ./volumes/certificates
dotnet dev-certs https -ep ./volumes/certificates/snapp.pfx -p SuP3rS3CuR3P4sSw0Rd
dotnet dev-certs https --trust   # Linux: may need manual trust
```

---

### PostgreSQL Stack

**File:** `docker-compose.postgres.yml`

| Service | Description | Host Port |
|---------|-------------|-----------|
| `postgres` | PostgreSQL 16 | `localhost:5532` |
| `pgadmin` | pgAdmin 4 web UI | `http://localhost:5433` |
| `snauth` | SnAuth identity / JWT server | `https://localhost:44311` |
| `snapp` | sensenet API (built from local source) | `https://localhost:44362` |

#### Start

```bash
docker compose -f docker-compose.postgres.yml up -d
```

The first start builds the `snapp` image from `../src` and runs the sensenet installer automatically (schema + initial content). This takes 1–3 minutes.

#### Stop

```bash
docker compose -f docker-compose.postgres.yml down
```

#### Get Admin API Key

```bash
docker compose -f docker-compose.postgres.yml run --rm apikey
```

This starts a one-shot container that queries the database and prints the current admin API key:

```
══════════════════════════════════════════════════
  🔑  Admin API Key (PostgreSQL)
══════════════════════════════════════════════════
  NJ1AFRTz8L1FbsJ0qyq1hToTfUiPYFh65CVIcd5kb4L...
══════════════════════════════════════════════════
```

> **Note:** The admin API key is regenerated on every `snapp` restart.

---

### MSSQL Stack

**File:** `docker-compose.mssql.yml`

| Service | Description | Host Port |
|---------|-------------|-----------|
| `mssql` | SQL Server 2022 Express | `localhost:9999` |
| `mssql-init` | Creates the `sensenet-sndb` database (runs once) | — |
| `snauth` | SnAuth identity / JWT server | `https://localhost:44311` |
| `snapp` | sensenet API (built from local source) | `https://localhost:44362` |

#### Start

```bash
docker compose -f docker-compose.mssql.yml up -d
```

#### Stop

```bash
docker compose -f docker-compose.mssql.yml down
```

#### Get Admin API Key

```bash
docker compose -f docker-compose.mssql.yml run --rm apikey
```

---

### Full Reset (DB + Index)

Both compose files include a `db-reset` service under the `reset` Docker Compose profile. Running it drops the database, recreates it empty, and clears the Lucene index so that the next `snapp` start performs a fresh install.

#### Manual reset (PostgreSQL)

```bash
# 1. Stop everything
docker compose -f docker-compose.postgres.yml down

# 2. Run the reset profile (drops + recreates DB, clears index)
docker compose -f docker-compose.postgres.yml --profile reset up db-reset --abort-on-container-exit

# 3. Stop reset containers
docker compose -f docker-compose.postgres.yml --profile reset down

# 4. Start fresh
docker compose -f docker-compose.postgres.yml up -d
```

#### Manual reset (MSSQL)

```bash
docker compose -f docker-compose.mssql.yml down
docker compose -f docker-compose.mssql.yml --profile reset up db-reset --abort-on-container-exit
docker compose -f docker-compose.mssql.yml --profile reset down
docker compose -f docker-compose.mssql.yml up -d
```

---

## Shell Scripts (Linux / macOS)

All bash scripts are in the `deployment/scripts-linux/` directory. You can run
them from anywhere — they resolve paths relative to their own location.

```bash
cd deployment/scripts-linux
```

### Start

| Script | Description |
|--------|-------------|
| `./start-postgres.sh` | Start the PostgreSQL stack |
| `./start-mssql.sh` | Start the MSSQL stack |

```bash
./start-postgres.sh              # start (rebuild if image doesn't exist)
./start-postgres.sh --build      # force rebuild snapp image
./start-postgres.sh --no-build   # skip building, use existing image
```

Both start scripts wait up to 3 minutes for sensenet to respond with HTTP 200,
then print all service URLs and how to get the API key.

### Stop

| Script | Description |
|--------|-------------|
| `./stop-postgres.sh` | Stop the PostgreSQL stack (data preserved) |
| `./stop-mssql.sh` | Stop the MSSQL stack (data preserved) |

```bash
./stop-postgres.sh               # stop containers, keep database data
./stop-postgres.sh --clean       # stop + remove Docker volumes (⚠ deletes DB!)
```

### Reset (full reinstall)

| Script | Description |
|--------|-------------|
| `./reset-postgres.sh` | Stop → drop DB → clear index → rebuild → start |
| `./reset-mssql.sh` | Stop → drop DB → clear index → rebuild → start |

```bash
./reset-postgres.sh              # full reset + rebuild snapp image
./reset-postgres.sh --no-build   # reset without rebuilding the image
```

The reset scripts perform these steps:

1. **Stop** all containers (including the `reset` profile)
2. **Drop & recreate** the database via the `db-reset` container
3. **Clear** the Lucene index (`App_Data/LocalIndex`)
4. **Rebuild** the `snapp` Docker image from source (skip with `--no-build`)
5. **Start** the full stack
6. **Wait** up to 3 minutes for sensenet to respond with HTTP 200 on `/odata.svc/Root`

### API Key

The admin API key is **regenerated on every `snapp` restart**. Use these scripts
to read the current key from the database:

| Script | Description |
|--------|-------------|
| `./apikey-postgres.sh` | Read the admin API key from PostgreSQL |
| `./apikey-mssql.sh` | Read the admin API key from MSSQL |

```bash
./apikey-postgres.sh             # print the key
./apikey-postgres.sh --copy      # print + copy to clipboard
./apikey-postgres.sh --bench     # print + update SnBenchmark appsettings.json
```

The `--bench` flag automatically writes the key into
`tools/SnBenchmark/appsettings.json` so you can run the benchmark tool right
away without manual copy-paste.

> You can also use the docker-compose one-shot container:
> `docker compose -f docker-compose.postgres.yml run --rm apikey`

### Quick Reference

```bash
cd deployment/scripts-linux

# ── PostgreSQL ──────────────────────────────────
./start-postgres.sh          # start
./stop-postgres.sh           # stop
./reset-postgres.sh          # full reset
./apikey-postgres.sh         # get API key
./apikey-postgres.sh --bench # get API key + update benchmark config

# ── MSSQL ───────────────────────────────────────
./start-mssql.sh             # start
./stop-mssql.sh              # stop
./reset-mssql.sh             # full reset
./apikey-mssql.sh            # get API key
./apikey-mssql.sh --bench    # get API key + update benchmark config
```

---

## Default Credentials

| Service | User | Password |
|---------|------|----------|
| sensenet admin UI | `admin` | `admin` |
| PostgreSQL | `postgres` | `SuP3rS3CuR3P4sSw0Rd` |
| pgAdmin | `admin@sensenet.com` | `admin` |
| MSSQL SA | `sa` | `SuP3rS3CuR3P4sSw0Rd` |

---

## File Overview

```
deployment/
├── docker-compose.postgres.yml   PostgreSQL stack (postgres, pgadmin, snauth, snapp)
├── docker-compose.mssql.yml      MSSQL stack (mssql, snauth, snapp)
├── install-sensenet.ps1          Legacy PowerShell installer
├── readme.md                     This file
├── App_Data/                     Mounted into snapp (Lucene index, logs)
├── scripts-linux/                Bash scripts for Linux / macOS
│   ├── start-postgres.sh         Start the PostgreSQL stack
│   ├── stop-postgres.sh          Stop the PostgreSQL stack
│   ├── reset-postgres.sh         Full reset (drop DB + clear index + restart)
│   ├── apikey-postgres.sh        Read admin API key from PostgreSQL
│   ├── start-mssql.sh            Start the MSSQL stack
│   ├── stop-mssql.sh             Stop the MSSQL stack
│   ├── reset-mssql.sh            Full reset (drop DB + clear index + restart)
│   └── apikey-mssql.sh           Read admin API key from MSSQL
├── scripts/                      PowerShell helper scripts
├── volumes/
│   ├── certificates/             Dev TLS certificate (snapp.pfx)
│   └── pgadmin/                  pgAdmin server config
└── ...
```
