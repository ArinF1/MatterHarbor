# Changelog

All notable changes will be documented here. The format follows Keep a Changelog and the project uses semantic versioning after its first release.

## [Unreleased]

### Added

- Initial modular monolith, separate worker, React client, PostgreSQL persistence, transactional outbox, tenant isolation, idempotency, optimistic concurrency, audit history, observability, tests, CI, containers, Bicep, and project documentation.

### Fixed

- Made an empty web container API base URL fall back to the documented local API address.

### Security

- Replaced React Router with Wouter after a newly disclosed React Router vulnerability had no published patched release.
- Updated the locked `brace-expansion` dependency to a release that resolves its denial-of-service advisory.

[Unreleased]: https://github.com/ArinF1/MatterHarbor/commits/main/
