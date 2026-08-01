# Roadmap

MatterHarbor is not production-ready. Priorities are ordered by risk reduction and completion of the existing slice, not by promised dates.

## Now — harden the first slice

- Integrate the CI-tested migration bundle into an approved deployment pipeline with a restricted migration identity and exercised backup/restore. Production startup no longer applies schema changes.
- Add roles and explicit case transition/assignment policies with complete audit coverage.
- Add outbox retry backoff, dead-letter operations, retention, metrics, and Azure Service Bus contract tests.

## Next — usable case collaboration

- Comments and internal notes, notification preferences, assignment history, filters, search, and cursor pagination.
- ETag/If-Match concurrency and idempotency for every mutating command.
- Organization/user administration integrated with OIDC provisioning.
- Backup/restore exercises, dashboards, alerts, SLOs, and performance baselines.

## Later — files and privacy lifecycle

- Blob uploads through quarantine, strict validation, malware scanning, promotion, safe download, and retention.
- Personal-data inventory, export, anonymization/deletion, legal holds, and retention policies.
- Private networking, least-privilege Azure RBAC, deployment identities, release provenance, and disaster recovery.

No milestone implies production readiness without a separate readiness review and independent security assessment.
