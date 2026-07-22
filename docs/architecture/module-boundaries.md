# Module boundaries

| Project | Responsibility | Allowed project dependencies |
| --- | --- | --- |
| Domain | Entities, enums, invariants, domain errors, audit entity | None |
| Application | Case commands/queries, idempotency hashing, ports | Domain |
| Infrastructure | EF Core/PostgreSQL, OIDC-adjacent adapters, outbox, Service Bus | Application, Domain |
| API | HTTP contracts, auth, tenant claim extraction, errors, composition | Application, Infrastructure |
| Worker | Outbox polling host | Infrastructure |
| Web | Browser UX and typed REST contracts | HTTP only |

Architecture tests verify the two most important negative rules: Domain has no MatterHarbor dependencies, and Application cannot reference Infrastructure or API. Future modules should expose narrow application contracts. Cross-module database access must not bypass organization predicates or transaction ownership.
