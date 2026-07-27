# Threat model

## Scope and assets

This initial model covers identities, organization boundaries, case records, audit history, idempotency records, outbox messages, telemetry, and planned files. The software is early-stage and has not been independently assessed.

| Threat | Current mitigation | Residual work |
| --- | --- | --- |
| Forged or invalid identity | Production uses HTTPS OIDC metadata with issuer, audience, lifetime, and signature validation; missing configuration fails startup | Provisioning, key-rotation drills, claims mapping, role policies |
| Development auth exposed in production | Startup throws unless the host environment is Development | Deployment policy test and image-level environment review |
| Cross-organization access | Server derives organization from claims; every list/get/update query predicates organization + identifier; tests cover isolation | Route inventory test as surface grows; database RLS evaluation |
| IDOR through UUID | UUID alone never authorizes access | Continue scoped queries for all new entities |
| Concurrent overwrite | Integer EF concurrency token, 409 response, and accessible reload-before-retry conflict UX | Add ETag/If-Match HTTP semantics |
| Duplicate command | Required idempotency key, normalized SHA-256 payload hash, database key, advisory transaction lock | Expiry/retention policy and coverage for all writes |
| Audit tampering | Application only appends; DbContext rejects update/delete; same transaction as case | Restricted DB role, hash chaining/WORM evaluation, broader event coverage |
| Lost or duplicate async work | Same-transaction outbox, conditional lease claims, expired-lease recovery | Dead-letter policy, backoff, idempotent consumers, Service Bus contract tests |
| Sensitive log disclosure | No request bodies, tokens, descriptions, titles, or payloads are logged; stable IDs/error codes only | Automated log redaction tests and production telemetry review |
| Denial of service | Bounded lists, conservative fixed-window rate limit, input length limits | Per-route policies, distributed counters, load tests, request size limits |
| Malicious file upload (planned) | No file upload exists | Quarantine container, content validation, malware scan, safe names, access-controlled download |
| Personal-data over-retention | No real data is permitted in this early project | Classification, retention jobs, export, anonymization, legal-hold policy |
| Supply-chain compromise | Lockfiles, central versions, CI vulnerability checks, least-privilege workflow token | Dependabot/Renovate, provenance and signed release process |

## Trust boundaries

The browser, OIDC provider, PostgreSQL, Azure Service Bus, Blob Storage, and telemetry backend are separate trust boundaries. Credentials must come from environment configuration or Key Vault; none belong in source. The current Bicep is a design artifact and has not been deployed or security-tested.
