# Docker image workflows

These workflows replace only the product-image builds whose source is the
`SenseNet/sensenet` GitHub repository. Images owned by other source
repositories must be built by workflows in those repositories.

## Workflow layout

| Workflow | Responsibility |
| --- | --- |
| `_docker-build-image.yml` | Reusable checkout, validation, metadata, Buildx build, Docker Hub login, and optional push |
| `docker-sensenet-images.yml` | Builds the standard image matrix available in the selected revision |
| `docker-sensenet-localauth.yml` | Build-only validation of the dedicated SQL LocalAuth image |

All Dockerfiles in the audited definitions expect the repository's `src`
directory as their Docker build context.

## Image inventory

| Image | Source | Dockerfile | Build condition |
| --- | --- | --- | --- |
| `sensenetcsp/sn-api-inmem` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.InMem.TokenAuth/Dockerfile` | when available in the workflow revision |
| `sensenetcsp/sn-api-sql` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.TokenAuth/Dockerfile` | when available in the workflow revision |
| `sensenetcsp/sn-api-nlb` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth/Dockerfile` | when available in the workflow revision |
| `sensenetcsp/sn-api-sql-prv` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.TokenAuth.Preview/Dockerfile` | when available in the workflow revision |
| `sensenetcsp/sn-api-nlb-prv` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.SearchService.TokenAuth.Preview/Dockerfile` | when available in the workflow revision |
| `sensenetcsp/sn-api-sql-localauth` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.Sql.LocalAuth/Dockerfile` | separate build-only LocalAuth workflow |
| `sensenetcsp/sn-api-postgre` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile` | when available in the workflow revision |

## Standard image publication rules

- Pushes to `develop`, `master`, and `postgres-provider` run the workflow;
  pull requests targeting `develop` or `master` run build-only validation.
- Pull requests build but never log in or push.
- Manual runs do not push unless `push_image` is explicitly enabled.
- Manual runs build the branch selected in GitHub's **Run workflow** dialog.
- The standard matrix builds its available images by default; manual runs may select one.
  LocalAuth is independent and is not included in that matrix.
- Publishing runs retain the legacy TFS build-date tag: `develop.YYYY.MM.DD`
  on `develop`, `YYYY.MM.DD` on `master`/`main`, and
  `<branch>.YYYY.MM.DD` on other branches. They also publish a source-branch
  tag and the immutable `YYYYMMDD-shortSHA` tag.
- Pushes to `develop` also publish `preview`.
- Pushes to `master` also publish `latest`.

Repository secrets expected by publishing jobs:

- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

The reusable builder validates the selected build context and Dockerfile after
checkout. A manual run therefore fails clearly when the selected source
revision does not contain the chosen WebApp or its Dockerfile.

## Dedicated LocalAuth image

`Docker image - SenseNet SQL LocalAuth` builds
`sensenetcsp/sn-api-sql-localauth` using the reusable builder, with its own cache
and concurrency group. It runs on source changes pushed to `develop`/`master`,
on non-draft pull requests targeting those branches, or by manual dispatch.
Shared `src/**` changes trigger validation because the host references repository libraries.

This workflow is currently **build-only for every trigger**: `push_image: false`
and no Docker Hub credentials are passed. The image name is reserved in build metadata;
this change does not create or publish a Docker Hub repository/image.
Branch/date/SHA metadata follows the standard reusable builder.
Publication can be enabled separately after approval for this new Docker Hub destination.
The workflow does not deploy insql. See [local authentication](local-authentication.md)
for host configuration and deployment.

## Deliberate exclusions and review points

- TFS definition 449 is not reproduced. Its exported configuration publishes
  the normal SQL image even though its name says NLB/localindex.
- The colleague-owned `kavics/sn-api-sql` build is outside this migration.
- `sensenetcsp/sn-auth` already has its own proven workflow.
- IdentityServer, SearchService, Taskmanager, Taskagent, and Aspose belong in
  their own GitHub source repositories and are outside these workflows.
- Registry cleanup, runner cleanup, and image keep-alive definitions remain
  operations concerns.
- The archived PostgreSQL definition sets `SnImageVersion=alpha`, but the
  legacy tag/publish steps derive tags from the source branch and do not
  publish an `alpha` moving tag. This workflow preserves that behavior.
