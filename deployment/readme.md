# Local Docker demos: Compose or PowerShell

Each `docker-compose*.yml` file is a complete scenario selected with one `-f` argument. The files share service definitions through `compose.services.yml`; no generator or installer script runs when Compose starts. Docker Compose v2.24 or newer is required. Published ports bind to `127.0.0.1`.

Prepare the certificate once, from this directory:

```powershell
Copy-Item .env.template .env
./scripts/create-devcert.ps1 -CertPsw 'SuP3rS3CuR3P4sSw0Rd'
docker compose -f docker-compose.with-auth.yml up -d --pull always
```

The certificate helper exports/trusts the local .NET developer certificate. If you provide a certificate yourself, place `snapp.pfx` in `temp/certificates`, or set `CERT_FOLDER` and `CERT_PASSWORD` in `.env`. Set `ADMIN_UI_URL` to the origin of the Admin UI you use; its template value is a placeholder. `.env`, certificates and generated signing keys are ignored by Git. The default passwords/API key are public demo values.

For webapps present in the current checkout, `up -d --build` builds the local source instead. An existing local floating image tag may be older than the registry tag; `--pull always` refreshes published images. The LocalAuth scenario always needs its local build, and PostgreSQL source is available on its provider branch.

The following SQL scenarios preserve the PowerShell installer's repository/auth/search port calculation. The auth column selects the older IdentityServer or SNAuth. These are alternative authentication arrangements on the same ports, so stop one before starting another.

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

SQL containers publish port 9999, PostgreSQL containers 5532, and RabbitMQ management 51005. AMQP stays inside the Compose network. Override `SQL_PORT`, `POSTGRES_PORT` or `RABBIT_PORT` when running multiple projects. Each Compose project owns its RabbitMQ container and named volumes; the older PowerShell installer shares its `sensenet-rabbit` container. Stop a Compose demo with the same file (`docker compose -f <file> down`); adding `--volumes` also deletes its persistent data. Use this Compose cleanup for Compose resources, rather than the PowerShell uninstaller.

For host SQL, configure `DATASOURCE`, `SQL_USER` and `SQL_PASSWORD` in `.env`. The login needs database-creation permission. The default Docker host address is `host.docker.internal`; an explicit `host-gateway` mapping also supports Linux hosts. Host scenarios create an empty database idempotently but never start or reset your SQL server.

Additional webapps:

| Scenario | Compose file | PowerShell selection | Repository/auth ports |
| --- | --- | --- | --- |
| In-memory, IdentityServer | `docker-compose.in-memory.yml` | `-InMemPlatform` | 51011 / 51012 |
| In-memory, SNAuth | `docker-compose.in-memory-auth.yml` | `-InMemPlatform -UseAuth` | 51011 / 51012 |
| SQL with document previews, SNAuth | `docker-compose.preview.yml` | `-WebApp Preview -UseAuth` | 51016 / 51017 |
| Search service with previews, SNAuth | `docker-compose.preview-search.yml` | `-WebApp Preview -SearchService -UseAuth` | 51056 / 51057 |
| SQL with internal authentication | `docker-compose.local-auth.yml` | `-WebApp LocalAuth` | 51016 |
| PostgreSQL with SNAuth | `docker-compose.postgres.yml` | `-WebApp PostgreSql` | 51016 / 51017 |

LocalAuth also has `local-auth-volume`, `local-auth-host-db`, and `local-auth-host-db-volume` variants, with repository ports 51036, 51026 and 51046. PostgreSQL has a `postgres-volume` variant on 51036/51037. The `*.Admin` projects and `WebHookHandler` have no Docker image definitions in this repository. `Sql.TokenAuth.NLB` uses an in-process index and is separate from the published `sn-api-nlb` image, which is built from `Sql.SearchService.TokenAuth`.

LocalAuth is build-only in CI. Prepare its RSA key with PowerShell 7.2+, then build locally:

```powershell
./scripts/create-localauth-key.ps1
docker compose -f docker-compose.local-auth.yml up -d --build
# Equivalent installer, building the local checkout:
./install-sensenet.ps1 -WebApp LocalAuth -CreateImages -LocalSn
```

This demo enables `InternalOnly`, bearer transport, the built-in admin user (Id=1), and private/loopback client networks. It explicitly disables mandatory MFA for the demo account. The login page is `/authentication/local/login`; never reuse the generated signing key or these demo settings for a hosted installation. Data persistence does not control key persistence: keep the same ignored key when restarting an existing demo.

PostgreSQL application source is on `postgres-provider`, not `develop`. Its Compose scenario runs the published `sensenetcsp/sn-api-postgre:postgres-provider` image and does not try to build a missing Dockerfile from `develop`. Override `POSTGRES_IMAGE` to use another compatible image. To build PostgreSQL from source, use a checkout containing its webapp; `-WebApp PostgreSql -CreateImages` downloads that branch, while `-LocalSn` requires the webapp already present. PostgreSQL host-database and search-service combinations, and LocalAuth search-service combinations, are rejected because those webapps are unavailable. The branch's existing development Compose files use different ports (44362/44311); these demos retain the 510xx installer convention.

`-WebApp Preview` selects the `sn-api-sql-prv` / `sn-api-nlb-prv` images. `-ImageTag` defaults to `preview` and is used consistently for local build and run. `-SensenetDockerImage` selects an explicit application image. `-CreateImages` also builds the selected external auth server (SNAuth or IdentityServer), and the installer reuses those builds without pulling over them. Repeated installation starts an existing stopped container and keeps a running container. Changed image/environment/volume settings require explicit recreation; repeated installation does not silently replace existing configuration or delete unusual container states.

Validate the deployment changes without starting services:

```powershell
pwsh -NoProfile -File ./tests/Test-Deployment.ps1
```

Opt-in runtime tests create an isolated, randomly named Compose project, a temporary certificate/key without changing the trust store, and fresh databases. They validate OData, admin login, an authenticated admin read, repeated `up`, and restart, then remove only their own containers/volumes. Application ports from the selected scenario must be free:

```powershell
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario WithAuth
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario LocalAuth
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Postgres
pwsh -NoProfile -File ./tests/Test-DeploymentSmoke.ps1 -Scenario Search
```

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
