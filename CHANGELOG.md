# Changelog

All notable changes will be documented here. The format follows Keep a Changelog and the project uses semantic versioning after its first release.

## [Unreleased]

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
