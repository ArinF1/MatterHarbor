# ADR 0004: Transactional outbox and Azure Service Bus

- Status: Accepted
- Date: 2026-07-22

## Decision

Persist durable integration events in PostgreSQL in the business transaction. A separate worker conditionally leases records and publishes through `IOutboxPublisher`. Local development logs identifiers; cloud configuration uses the Azure Service Bus SDK with `DefaultAzureCredential`.

## Consequences

Database commits cannot lose the intent to publish. Delivery is at least once, so consumers require idempotency. Lease expiry handles worker crashes but retry backoff, dead-letter operations, retention, and real Service Bus contract tests remain planned.
