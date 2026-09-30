# ADR 0007: Optimistic concurrency and idempotency

- Status: Accepted
- Date: 2026-07-22

## Decision

Use an integer EF concurrency token on cases. Require `Idempotency-Key` for creation, status, and assignment. Updates also require an `If-Match` ETag containing the expected version. Store an organization-scoped key, SHA-256 request hash, and original response; serialize same-key transactions with a PostgreSQL advisory lock. For updates, the stored key also includes the actor, operation, and case ID so retries do not cross commands.

## Consequences

Retries return the original case, changed payloads return 409, missing update preconditions return 428, and stale writes do not silently win. Keys currently have no expiry; retention and cleanup need a policy before long-lived use.
