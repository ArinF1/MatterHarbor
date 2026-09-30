# Changelog

All notable changes will be documented here. The format follows Keep a Changelog and the project uses semantic versioning after its first release.

## [Unreleased]

### Changed

- Enforced case status transitions, made Closed terminal with a resolved-case reopen path, and atomically audited each status change.
- Grouped Dependabot minor and patch updates by ecosystem so routine upgrades arrive in fewer pull requests; major upgrades remain separate.
- Updated Testcontainers.PostgreSql to 4.15.0 so its transitive SSH.NET dependency resolves to the patched 2026.0.0 release.
- Updated the frontend lockfile and Vitest to resolve all six advisories reported by the npm audit.
- Applied compatible pending Dependabot updates and upgraded the Node 22 patch baseline for jsdom 30; deferred TypeScript 7 until the lint parser supports it.
- Refreshed the grouped NuGet and web dependencies, xUnit runner 4, and Vitest 5 with regenerated locked dependency graphs.
- Restricted automatic PostgreSQL migration and fictional seed data to Development; production startup no longer owns schema changes.

### Added

- Added a versioned, checksummed EF Core migration bundle that CI applies over fictional v0.1 rows, verifies for data preservation, and smoke-tests through a schema-restricted production-mode API identity.
- Added a controlled migration runbook covering backup preflight, serialized execution, verification, and forward-fix failure handling.

### Fixed

- Made the local E2E script honor the existing web, API, and PostgreSQL host-port overrides.
- Pinned API and worker build images to the SDK selected by `global.json`, avoiding failures when the floating .NET 10 image advances to another feature band.

## [0.1.0] - 2026-07-27

### Added

- Initial modular monolith, separate worker, React client, PostgreSQL persistence, transactional outbox, tenant isolation, idempotency, optimistic concurrency, audit history, observability, tests, CI, containers, Bicep, and project documentation.
- Hosted HTTP integration coverage for authentication, tenant isolation, problem responses, rate limiting, and idempotency replay/conflict behavior.
- Unskipped CI Playwright coverage across the web, API, worker, and PostgreSQL, including proof that the worker drains the created outbox message.
- Accessible list/detail loading, retryable error, and status-update concurrency-conflict states.
- Locked .NET dependency graphs, v0.1 scope, release notes, and reproducible build instructions.

### Fixed

- Made an empty web container API base URL fall back to the documented local API address.
- Reset organization-scoped navigation to the case list when switching development personas.

### Security

- Replaced React Router with Wouter after a newly disclosed React Router vulnerability had no published patched release.
- Updated the locked `brace-expansion` dependency to a release that resolves its denial-of-service advisory.
- Enabled GitHub private vulnerability reporting and added a private security contact link to the issue chooser.

[Unreleased]: https://github.com/ArinF1/MatterHarbor/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/ArinF1/MatterHarbor/releases/tag/v0.1.0
