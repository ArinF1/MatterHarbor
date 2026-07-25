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

## Highest-priority next tasks

1. Add full HTTP integration tests for authentication, tenant isolation, validation/problem details, rate limiting, and replay status/headers.
2. Make Playwright run end to end in CI with the API, worker, web app, and PostgreSQL orchestration.
3. Implement roles, assignment rules, allowed state transitions, ETag/If-Match, and audit every case mutation.
4. Operationalize outbox retries, dead-letter handling, retention, metrics, and Azure Service Bus contract tests.
5. Build controlled migration/deployment and backup/restore runbooks before any shared environment.

## Deferred product work

- [ ] Comments and internal notes
- [ ] Search, filters, and cursor pagination
- [ ] Notification preferences and delivery channels
- [ ] Quarantined file upload and malware scanning
- [ ] Personal-data export, anonymization/deletion, and retention
- [ ] Dashboards, alerts, SLOs, performance tests, and disaster recovery
