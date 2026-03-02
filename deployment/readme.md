# Install sensenet on local Docker with Powershell

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

The script is preconfigured so you don't have to know every switch if you just want to try it out. With the preconfigured settings the installer will create an IdentityServer, an MS SQL Server and a sensenet application container. Neither will have mounted volumes so the necessary files will be either copied into the containers or created inside them.

To install sensenet, execute the following script:

```powershell
.\install-sensenet.ps1 
```

After the script completes, your repository will be accessible on the URL displayed by the script. In case of the default installation this is `https://localhost:51016` but it may be different in case of in-memory or search service scenarios. 

You can log in by visiting [https://admin.sensenet.com](https://admin.test.sensenet.com) and providing the repository url. The default credentials are:

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

This is a bit advanced switch as Docker for Windows with linux containers may have a problem with the default setup. By default containers will be created **without volume bind**, so it should work on every system. But it is recommended to use volumes with an actual sensenet project in containers. This switch shows how to bind those volumes for sensenet services. However with default settings it may only work on a linux host. If you use Docker for Windows you may have to set the folder path to bind and this path should be on linux. It is because Windows and linux file systems handle file locks differently and the Lucene engine from the linux containers will not be able to lock the necessary files on a Windows file system.

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