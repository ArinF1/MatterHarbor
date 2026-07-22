# ADR 0002: PostgreSQL and EF Core

- Status: Accepted
- Date: 2026-07-22

## Decision

Use PostgreSQL 17 and EF Core with Npgsql. Keep use-case-specific persistence ports instead of a generic repository. Version schema changes with migrations and test behavior against real PostgreSQL through Testcontainers.

## Consequences

PostgreSQL provides transactions, constraints, JSONB, advisory locks, and mature Azure hosting. EF Core reduces mapping boilerplate but requires careful query scoping, concurrency configuration, and migration review. Tests need Docker.
