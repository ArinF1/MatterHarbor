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
- [x] Enforce the case status lifecycle, make Closed terminal, allow resolved cases to reopen, and audit each status change atomically
- [x] v0.1 scope, release notes, dependency locks, reproducible build instructions, and private vulnerability reporting
- [x] Group routine Dependabot minor and patch updates by ecosystem while keeping major upgrades separate
- [x] Apply compatible pending Dependabot updates and hold TypeScript 7 until the lint parser supports it
- [x] Upgrade the Node 22 baseline and jsdom 30 together
- [x] Update Testcontainers to resolve its transitive SSH.NET security advisories
- [x] Resolve frontend dependency advisories reported by npm audit

## Completed v1.0 groundwork

- [x] Restrict startup migration and fictional seeding to Development
- [x] Build, checksum, apply over fictional v0.1 data, verify preservation and runtime-role restrictions, and smoke-test the versioned EF migration bundle in CI
- [x] Document controlled single-run migration, backup preflight, verification, and failure handling
- [x] Add bounded outbox retry, dead-letter records, processed-record retention, telemetry, an operator redrive runbook, and a Service Bus sender-level contract test
- [x] Add stored organization roles, scoped member checks, administrator assignment, case-worker status policy, ETag/If-Match, update idempotency, and assignment audit
- [ ] Run the new pg_dump/pg_restore CI exercise over the fictional v0.1 fixture and verify the restored migration head, case, and member role

## Highest-priority next tasks

The [v1.0.0 release gates](releases/v1.0.0-readiness.md) are required before tagging the next release.

1. Exercise Azure Service Bus adapter contract and dead-letter recovery in a disposable environment.
2. Add an approved deployment pipeline, restricted migration/runtime database identities, and an exercised backup/restore runbook before any shared environment.
3. Verify all release gates at the release candidate commit and record the results.

## Deferred product work

- [ ] Comments and internal notes
- [ ] Search, filters, and cursor pagination
- [ ] Notification preferences and delivery channels
- [ ] Quarantined file upload and malware scanning
- [ ] Personal-data export, anonymization/deletion, and retention
- [ ] Dashboards, alerts, SLOs, performance tests, and disaster recovery

## Deferred repository maintenance

- [ ] Re-enable TypeScript major updates after TypeScript ESLint supports TypeScript 7
