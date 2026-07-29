# Controlled database migrations

MatterHarbor is not production-ready. Do not use it with real personal data.

The API applies migrations and fictional seed data only in `Development`. Every other environment must use the versioned migration bundle built and smoke-tested by CI. The bundle is a deployment artifact, not a separate long-running process.

## Artifact

Each CI run publishes `matterharbor-migrations-<source commit>` containing:

- the self-contained Linux x64 migration executable;
- `SHA256SUMS`, covering the executable and build metadata;
- `SOURCE_COMMIT`, identifying the exact source revision;
- `BUILD_INFO.json` and `resolved-locks/`, recording the SDK, EF tool, target, and resolved package graph.

Build the same artifact locally with:

```powershell
./scripts/build-migration-bundle.ps1 -SourceVersion <source-commit>
```

## Required preflight

The deployment owner must:

1. Approve the exact application image and migration artifact from the same source commit.
2. Confirm a recent backup and a tested restore path for the target database.
3. Stop if another migration execution or incompatible deployment is in progress.
4. Verify that `BUILD_INFO.json` identifies a self-contained `linux-x64` artifact from the approved source commit and that the one-off runner is Linux x64. `global.json` selects the build SDK; it does not pin a deployment runtime.
5. Supply a migration-only database identity from the deployment secret store. Never put its connection string in source, an artifact, or logs.
6. Verify network access from the one-off deployment runner and record the current migration head:

```sql
SELECT "MigrationId"
FROM matterharbor."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;
```

The runtime API and worker identities must not own schema-altering permissions.

## Apply once

Verify the downloaded artifact before execution:

```bash
sha256sum --check SHA256SUMS
test "$(cat SOURCE_COMMIT)" = "<approved-source-commit>"
chmod 0755 matterharbor-migrate
./matterharbor-migrate --connection "$MATTERHARBOR_MIGRATION_CONNECTION_STRING" --no-color
```

Run one bundle instance for the target database. A deployment pipeline must serialize this step and must not start the new application revision until it succeeds.

## Verify

After the bundle exits successfully:

1. Confirm the expected migration is the newest row in `matterharbor."__EFMigrationsHistory"`.
2. Start the API and worker revision from the same source commit.
3. Require `/health/ready` to pass before shifting traffic.
4. Exercise authenticated case read/write smoke tests using fictional data.
5. Check API, worker, PostgreSQL, and outbox telemetry for errors without logging case content or credentials.

## Failure handling

Stop the rollout on any bundle or readiness failure. Do not run an automatic down migration. Preserve the non-sensitive bundle output, keep traffic on the last compatible application revision, and have the deployment owner choose either a reviewed forward-fix migration or a database restore. A restore is a go/no-go decision because it can discard writes made after the backup.

Schema compatibility, deployment rollback, restricted database roles, and backup restoration must be exercised in an approved environment before this repository can claim production readiness.
