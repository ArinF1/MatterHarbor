# Outbox operations

MatterHarbor is a fictional-data learning project. Do not use real personal data.

The worker polls at most 100 records per batch. A failed publish is retried after exponential backoff, starting at five seconds and capped at one hour. After ten attempts it enters `DeadLetter` and stops being claimed. A lost or expired lease may cause duplicate delivery, so consumers must use the message ID for deduplication. Final status updates require the worker's current lock ID.

The worker deletes processed records after 30 days in batches of 1,000. Dead-letter records are retained until an operator reviews them. Telemetry counters `outbox.published`, `outbox.retried`, `outbox.dead_lettered`, and `outbox.purged` are exported through the worker's OpenTelemetry meter. Alert on dead-letter increments and a sustained retry rate.

## Review and redrive

Use a separate privileged maintenance identity in a controlled session, not the worker's runtime credentials. Never select or log `Payload` while triaging. Inspect the message type, ID, attempt count, error code, and age:

```sql
SELECT "Id", "OrganizationId", "Type", "AttemptCount", "LastErrorCode", "OccurredAt", "DeadLetteredAt"
FROM matterharbor.outbox_messages
WHERE "Status" = 'DeadLetter'
ORDER BY "DeadLetteredAt"
LIMIT 100;
```

Fix the publisher or downstream outage first. Confirm that the destination can deduplicate the message ID, then redrive one reviewed record by ID:

```sql
BEGIN;
UPDATE matterharbor.outbox_messages
SET "Status" = 'Pending', "AttemptCount" = 0, "NextAttemptAt" = NULL,
    "DeadLetteredAt" = NULL, "LastErrorCode" = NULL,
    "LockId" = NULL, "LockedUntil" = NULL
WHERE "Id" = '<reviewed-message-uuid>' AND "Status" = 'DeadLetter';
-- Require exactly one updated row before committing.
COMMIT;
```

Verify that the record reaches `Processed` and that downstream handling is correct. If the update affects zero rows, stop and re-read its status. Keep the dead-letter record for investigation if the failure is not understood.
