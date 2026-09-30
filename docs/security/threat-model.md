# Threat model

## Scope and assets

This initial model covers identities, organization boundaries, case records, audit history, idempotency records, outbox messages, telemetry, and planned files. The software is early-stage and has not been independently assessed.

| Threat | Current mitigation | Residual work |
| --- | --- | --- |
| Forged or invalid identity | Production uses HTTPS OIDC metadata with issuer, audience, lifetime, and signature validation; missing configuration fails startup. Case use cases verify stored organization membership and role | OIDC provisioning, key-rotation drills, claims mapping review |
| Development auth exposed in production | Startup throws unless the host environment is Development | Deployment policy test and image-level environment review |
| Cross-organization access | Server derives organization from claims; every list/get/update query predicates organization + identifier; tests cover isolation | Route inventory test as surface grows; database RLS evaluation |
| IDOR through UUID | UUID alone never authorizes access | Continue scoped queries for all new entities |
| Concurrent overwrite | Integer EF concurrency token, required version ETag on update, 409 response, and reload-before-retry conflict UX | Broader multi-user load testing |
| Duplicate command | Required idempotency key on all current case writes, SHA-256 request hash, database key, advisory transaction lock | Expiry/retention policy |
| Audit tampering | Application only appends; DbContext rejects update/delete; creation, status, and assignment audit records commit with their case writes | Restricted DB role, hash chaining/WORM evaluation |
| Lost or duplicate async work | Same-transaction outbox, conditional lease claims and completion, expired-lease recovery, bounded retry backoff, dead-letter state, and operator redrive runbook | Idempotent consumers, Service Bus contract tests, dead-letter recovery exercise, and alerts |
| Sensitive log disclosure | No request bodies, tokens, descriptions, titles, or payloads are logged; stable IDs/error codes only | Automated log redaction tests and production telemetry review |
| Denial of service | Bounded case and assignee lists, conservative fixed-window rate limit, input length limits | Per-route policies, distributed counters, load tests, request size limits |
| Uncontrolled schema change | API startup migrates and seeds only in Development; CI applies a checksummed bundle from the original v0.1 schema with a non-superuser migration role and proves separate API and worker roles cannot create schema objects | Restricted Azure identities, serialized deployment integration, broader compatibility tests, exercised cloud backup/restore |
| Malicious file upload (planned) | No file upload exists | Quarantine container, content validation, malware scan, safe names, access-controlled download |
| Personal-data over-retention | No real data is permitted in this early project | Classification, retention jobs, export, anonymization, legal-hold policy |
| Supply-chain compromise | Lockfiles, central versions, CI vulnerability checks, least-privilege workflow token | Dependabot/Renovate, provenance and signed release process |

## Trust boundaries

The browser, OIDC provider, PostgreSQL, Azure Service Bus, Blob Storage, and telemetry backend are separate trust boundaries. Credentials must come from environment configuration or Key Vault; none belong in source. The current Bicep is a design artifact and has not been deployed or security-tested.
