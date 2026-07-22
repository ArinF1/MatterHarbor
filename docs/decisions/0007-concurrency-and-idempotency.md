# ADR 0007: Optimistic concurrency and idempotency

- Status: Accepted
- Date: 2026-07-22

## Decision

Use an integer EF concurrency token on cases. Require `Idempotency-Key` for case creation. Store an organization-scoped key, normalized SHA-256 request hash, and original response; serialize same-key transactions with a PostgreSQL advisory lock.

## Consequences

Retries return the original case, changed payloads return 409, and stale writes do not silently win. Keys currently have no expiry and status updates use a body version rather than HTTP ETags; both are follow-up work.
