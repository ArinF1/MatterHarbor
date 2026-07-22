# ADR 0008: Observability with OpenTelemetry

- Status: Accepted
- Date: 2026-07-22

## Decision

Instrument ASP.NET Core, outgoing HTTP, runtime metrics, Npgsql activity sources, and worker outbox activities with OpenTelemetry. Export OTLP when a configured endpoint is present. Log stable IDs and error codes, never case text, tokens, headers, or message payloads.

## Consequences

Local Jaeger can display traces and Azure Monitor can receive OTLP-compatible telemetry later. Dashboards, alerts, sampling, cost controls, semantic conventions, and redaction verification remain planned.
