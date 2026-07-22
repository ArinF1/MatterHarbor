# ADR 0006: Audit history design

- Status: Accepted
- Date: 2026-07-22

## Decision

Store append-only audit entries containing organization, actor, entity, action, and timestamp in the business transaction. Do not copy case description or other unnecessary personal content into the audit record. Reject tracked audit updates/deletes in the application DbContext.

## Consequences

Creation history survives normal case evolution and avoids sensitive duplication. Database-role restrictions, complete mutation coverage, retention/legal hold, tamper evidence, and privileged operational access remain to be designed.
