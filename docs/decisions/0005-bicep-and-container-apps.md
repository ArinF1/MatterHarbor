# ADR 0005: Bicep and Azure Container Apps

- Status: Accepted
- Date: 2026-07-22

## Decision

Define Azure infrastructure with Bicep and target Azure Container Apps for API and worker workloads, PostgreSQL Flexible Server, Service Bus, Blob Storage, Key Vault, managed identity, Log Analytics, and Application Insights.

## Consequences

Bicep is reviewable alongside code and needs no paid third-party tool. The current file is a local design artifact: it has not been deployed, cost-approved, or production-hardened. Private networking, RBAC role assignments, secrets, web hosting, and deployment automation require follow-up.
