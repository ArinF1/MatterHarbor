# Testing

## Test layers

- Domain tests cover invariants without infrastructure.
- Application tests cover deterministic command behavior such as idempotency hashing.
- Architecture tests enforce forbidden project dependencies.
- Integration tests use Testcontainers with real PostgreSQL for transactions, idempotency, audit/outbox persistence, tenant isolation, concurrency, and outbox claiming. They are not mock substitutes.
- Vitest + Testing Library exercises the React case-creation flow and headers.
- Playwright defines a live browser flow but is skipped in the normal suite until browsers are installed and `MATTERHARBOR_E2E_BASE_URL` points to running services.

Run:

```bash
dotnet test MatterHarbor.sln
npm --prefix src/MatterHarbor.Web run test
```

Integration tests use Testcontainers by default and require Docker. A controlled CI or developer environment may set `MATTERHARBOR_TEST_CONNECTION_STRING` to an isolated disposable PostgreSQL database; never point it at shared or production data. For the opt-in browser test, first run `pwsh tests/MatterHarbor.EndToEndTests/bin/Debug/net10.0/playwright.ps1 install chromium`, start the stack, set `MATTERHARBOR_E2E_BASE_URL=http://localhost:5173`, remove the explicit skip after confirming the environment, and run that project. CI currently runs the skipped declaration and all other tests; full live E2E orchestration is a roadmap item.

The local transport verifies worker claim behavior and handler integration only. It does not prove Azure Service Bus locks, retries, identity, or network behavior; those need Azure SDK contract tests against an approved test environment.
