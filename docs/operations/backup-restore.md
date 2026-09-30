# Fictional-data backup and restore exercise

MatterHarbor remains a portfolio and learning project. Do not put real personal data in this exercise or its artifacts. The CI migration job rehearses a PostgreSQL 17 backup and restore using the fictional v0.1 fixture; it does not establish an Azure recovery objective.

## CI exercise

1. Apply the checksummed migration bundle to the original v0.1 schema and insert the fictional compatibility fixture before applying later migrations.
2. Confirm the fixture case still exists with status `New`, and its member has the backfilled `Administrator` role.
3. Run `pg_dump --format=custom --no-owner --no-acl` into the disposable PostgreSQL container. The dump stays in that container and is not uploaded as a CI artifact.
4. Restore into a newly created database with `pg_restore --exit-on-error --no-owner --no-acl`.
5. Compare the source and restored EF migration heads. Read the restored fictional case number and status and its member role.

CI fails if any command or comparison fails. The workflow output is the evidence for each commit; a passing job verifies this disposable-container procedure only.

## Azure exercise required for v1.0.0

In the approved disposable Azure environment, use a migration-only identity to apply the same checksummed bundle. Take a backup before the rollout, restore it into a separate server or database, and verify the migration head and authenticated case reads using only fictional records. Record the source commit, image digests, backup timestamp and location, restore target, start and finish times, and query/health results in the release record. Keep credentials and dump contents out of logs and the repository. Leave the v1.0.0 backup and deployment gates open until this exercise succeeds.

If the restore fails, stop the rollout, preserve non-sensitive diagnostics, and correct the procedure before retrying. Do not overwrite the source database as part of an exercise.
