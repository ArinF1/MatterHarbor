# ADR 0004: Transactional outbox and Azure Service Bus

- Status: Accepted
- Date: 2026-07-22

## Decision

Persist durable integration events in PostgreSQL in the business transaction. A separate worker conditionally leases records and publishes through `IOutboxPublisher`. Local development logs identifiers; cloud configuration uses the Azure Service Bus SDK with `DefaultAzureCredential`.

## Consequences

Database commits cannot lose the intent to publish. Delivery is at least once, so consumers require idempotency. Lease expiry handles worker crashes. The worker now applies bounded retry backoff, dead-letters after ten failures, emits counters, and purges processed records after 30 days. Operators can review and redrive a dead-letter record using the [outbox runbook](../operations/outbox.md). A sender-level SDK test checks message identity and JSON metadata; a live Azure Service Bus exercise remains planned.
