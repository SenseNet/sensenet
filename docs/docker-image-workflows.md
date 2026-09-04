# Docker image workflows

These workflows replace only the product-image builds whose source is the
`SenseNet/sensenet` GitHub repository. Images owned by other source
repositories must be built by workflows in those repositories.

## Workflow layout

| Workflow | Responsibility |
| --- | --- |
| `_docker-build-image.yml` | Reusable checkout, validation, metadata, Buildx build, Docker Hub login, and optional push |
| `docker-sensenet-images.yml` | Five images whose source and Dockerfile are in this repository |
| `docker-postgres-image.yml` | Branch-bound PostgreSQL API image |

All Dockerfiles in the audited definitions expect the repository's `src`
directory as their Docker build context.

## Image inventory

| Image | Source | Dockerfile | Trigger in this draft |
| --- | --- | --- | --- |
| `sensenetcsp/sn-api-inmem` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.InMem.TokenAuth/Dockerfile` | push/PR on `develop` or `master`; manual |
| `sensenetcsp/sn-api-sql` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.TokenAuth/Dockerfile` | push/PR on `develop` or `master`; manual |
| `sensenetcsp/sn-api-nlb` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth/Dockerfile` | push/PR on `develop` or `master`; manual |
| `sensenetcsp/sn-api-sql-prv` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.TokenAuth.Preview/Dockerfile` | push/PR on `develop` or `master`; manual |
| `sensenetcsp/sn-api-nlb-prv` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth.Preview/Dockerfile` | push/PR on `develop` or `master`; manual |
| `sensenetcsp/sn-api-postgre` | `SenseNet/sensenet@postgres-provider` | `src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile` | push on `postgres-provider`; matching PR; manual |

## Publication rules

- Pull requests build but never log in or push.
- Manual runs do not push unless `push_image` is explicitly enabled.
- Manual runs can use the branch selected in GitHub's **Run workflow** dialog,
  or override it with any source branch, tag, or commit through `source_ref`.
  This also lets the default-branch workflow build an older branch that does
  not contain the workflow files itself.
- Source push/dispatch runs publish a source-branch tag and
  `YYYYMMDD-shortSHA`.
- `develop` source events also publish `preview`.
- `master` source events also publish `latest`.
- PostgreSQL publishes only its branch tag and source version tag. It does not
  overwrite `preview` or `latest`.

Repository secrets expected by publishing jobs:

- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

The reusable builder validates the selected build context and Dockerfile after
checkout. A manual run therefore fails clearly when the selected source
revision does not contain the chosen WebApp or its Dockerfile.

## Deliberate exclusions and review points

- TFS definition 449 is not reproduced. Its exported configuration publishes
  the normal SQL image even though its name says NLB/localindex.
- The colleague-owned `kavics/sn-api-sql` build is outside this migration.
- `sensenetcsp/sn-auth` already has its own proven workflow.
- IdentityServer, SearchService, Taskmanager, Taskagent, and Aspose belong in
  their own GitHub source repositories and are outside these workflows.
- Registry cleanup, runner cleanup, and image keep-alive definitions remain
  operations concerns.
- Confirm whether legacy consumers require the old TFS date/revision tag in
  addition to the new source-derived version tag.
- Confirm whether PostgreSQL should receive an `alpha` moving tag.
