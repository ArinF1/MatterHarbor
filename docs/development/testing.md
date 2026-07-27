# Testing

## Test layers

- Domain tests cover invariants without infrastructure.
- Application tests cover deterministic command behavior such as idempotency hashing.
- Architecture tests enforce forbidden project dependencies.
- Integration tests host the real API pipeline against Testcontainers PostgreSQL and cover authentication, tenant isolation, RFC-style problem responses, rate limiting, idempotency, transactions, audit/outbox persistence, concurrency, and outbox claiming.
- Vitest + Testing Library exercises the React case-creation flow and headers.
- Playwright runs without a skip against disposable API, worker, web, and PostgreSQL containers and confirms the worker processes the created outbox record.

Run:

```bash
dotnet test MatterHarbor.sln --filter "Category!=EndToEnd"
npm --prefix src/MatterHarbor.Web run test
pwsh ./scripts/test-e2e.ps1
```

Integration tests use Testcontainers by default and require Docker. A controlled CI or developer environment may set `MATTERHARBOR_TEST_CONNECTION_STRING` to an isolated disposable PostgreSQL database; never point it at shared or production data. The browser script installs Chromium, starts the disposable stack, supplies the two required test environment variables, and always tears the stack down.

The local transport verifies worker claim behavior and handler integration only. It does not prove Azure Service Bus locks, retries, identity, or network behavior; those need Azure SDK contract tests against an approved test environment.
