# ADR 0001: Modular monolith and separate worker

- Status: Accepted
- Date: 2026-07-22

## Decision

Build one ASP.NET Core modular monolith and one .NET Worker process. Share Domain, Application, and Infrastructure libraries. Do not split features into networked services without an independently deployable need.

## Consequences

Case, audit, idempotency, and outbox writes use one local transaction and the project stays easy to clone. The worker can scale and restart independently. Module boundaries require tests and discipline because process boundaries do not enforce them.
