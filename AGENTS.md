# AGENTS.md

## Repository intent

MatterHarbor is an early-stage portfolio and learning project. Keep documentation explicit about what exists and what is planned. Never claim production readiness and never use real personal data.

## Architecture rules

- `MatterHarbor.Domain` contains domain rules and has no project dependencies.
- `MatterHarbor.Application` contains use cases and ports; it may depend only on Domain.
- `MatterHarbor.Infrastructure` implements persistence, messaging, storage, and other adapters.
- `MatterHarbor.Api` owns HTTP contracts, authentication, authorization, and composition.
- `MatterHarbor.Worker` is the only separate process and safely drains the transactional outbox.
- `MatterHarbor.Web` is the React client. Organization IDs always come from verified server-side claims, never browser input.
- Prefer feature-oriented, explicit code. Do not add a generic repository or a new process without a concrete documented need and an ADR.

## Security invariants

- Every case query must include the authenticated organization ID.
- Development authentication is permitted only when `IHostEnvironment.IsDevelopment()` is true.
- Do not log tokens, secrets, case titles, descriptions, outbox payloads, or request bodies.
- Case, audit, idempotency, and outbox writes for creation remain in one PostgreSQL transaction.
- New list endpoints must be bounded. New writes need concurrency and retry semantics.
- The API may migrate and seed only in Development. Other environments use the reviewed, checksummed migration bundle through a serialized deployment step.

## Verification

Before reporting completion, run the commands in README's checks section. Integration tests use a real PostgreSQL Testcontainer and require a working Docker daemon. Update migrations, architecture docs, threat model, roadmap, changelog, and `docs/tasks.md` when behavior changes.
