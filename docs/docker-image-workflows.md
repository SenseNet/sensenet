# Docker image workflows

These workflows replace only the product-image builds whose source is the
`SenseNet/sensenet` GitHub repository. Images owned by other source
repositories must be built by workflows in those repositories.

## Workflow layout

| Workflow | Responsibility |
| --- | --- |
| `_docker-build-image.yml` | Reusable checkout, validation, metadata, Buildx build, Docker Hub login, and optional push |
| `docker-sensenet-images.yml` | Builds every image whose source and Dockerfile are available in the selected revision |

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
| `sensenetcsp/sn-api-postgre` | `SenseNet/sensenet` | `src/WebApps/SnWebApplication.Api.PostgreSql.TokenAuth/Dockerfile` | when available in the workflow revision |

## Publication rules

- Pushes to `develop`, `master`, and `postgres-provider` run the workflow;
  pull requests targeting `develop` or `master` run build-only validation.
- Pull requests build but never log in or push.
- Manual runs do not push unless `push_image` is explicitly enabled.
- Manual runs build the branch selected in GitHub's **Run workflow** dialog.
- By default, a run builds every listed image whose Dockerfile exists in the
  selected revision. Manual runs may select one available image instead.
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
