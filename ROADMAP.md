# Roadmap

MatterHarbor is not production-ready. Priorities are ordered by risk reduction and completion of the existing slice, not by promised dates.

## Now — harden the first slice

- Integrate the CI-tested migration bundle into an approved deployment pipeline with a restricted migration identity and exercised backup/restore. Production startup no longer applies schema changes.
- Verify the new stored roles, explicit case transition/assignment policies, ETag retry contracts, and audit coverage at the release candidate commit.
- Exercise dead-letter recovery and Azure Service Bus contract tests against the new outbox retry and retention flow.

## Next — usable case collaboration

- Comments and internal notes, notification preferences, assignment history, filters, search, and cursor pagination.
- Idempotency record retention and user-facing recovery for network retries.
- Organization/user administration integrated with OIDC provisioning.
- Backup/restore exercises, dashboards, alerts, SLOs, and performance baselines.

## Later — files and privacy lifecycle

- Blob uploads through quarantine, strict validation, malware scanning, promotion, safe download, and retention.
- Personal-data inventory, export, anonymization/deletion, legal holds, and retention policies.
- Private networking, least-privilege Azure RBAC, deployment identities, release provenance, and disaster recovery.

No milestone implies production readiness without a separate readiness review and independent security assessment.
