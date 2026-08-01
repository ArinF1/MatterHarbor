# Engineering tasks

## Completed in the foundation

- [x] Modular monolith solution and separate worker
- [x] PostgreSQL EF Core model and initial migration
- [x] Development personas and fail-closed OIDC production configuration
- [x] Organization-scoped list/get/create/status APIs
- [x] Idempotency, optimistic concurrency, creation audit, and transactional outbox
- [x] Leased local outbox processing and Azure Service Bus publisher adapter
- [x] React list/create/details flow and meaningful interaction test
- [x] Persona switching resets organization-scoped navigation and preserves tenant isolation
- [x] Health, rate limiting, problem details, security headers, and OpenTelemetry
- [x] Testcontainers integration tests, architecture tests, CI, containers, Bicep, and documentation
- [x] Hosted HTTP tests for authentication, tenant isolation, problem details, rate limiting, and idempotency replay
- [x] Unskipped full-stack Playwright CI with worker outbox verification
- [x] Accessible loading, retryable error, and concurrency-conflict web states
- [x] v0.1 scope, release notes, dependency locks, reproducible build instructions, and private vulnerability reporting

## Completed v1.0 groundwork

- [x] Restrict startup migration and fictional seeding to Development
- [x] Build, checksum, apply over fictional v0.1 data, verify preservation and runtime-role restrictions, and smoke-test the versioned EF migration bundle in CI
- [x] Document controlled single-run migration, backup preflight, verification, and failure handling

## Highest-priority next tasks

1. Implement roles, assignment rules, allowed state transitions, ETag/If-Match, and audit every case mutation.
2. Operationalize outbox retries, dead-letter handling, retention, metrics, and Azure Service Bus contract tests.
3. Add an approved deployment pipeline, restricted migration/runtime database identities, and an exercised backup/restore runbook before any shared environment.

## Deferred product work

- [ ] Comments and internal notes
- [ ] Search, filters, and cursor pagination
- [ ] Notification preferences and delivery channels
- [ ] Quarantined file upload and malware scanning
- [ ] Personal-data export, anonymization/deletion, and retention
- [ ] Dashboards, alerts, SLOs, performance tests, and disaster recovery
