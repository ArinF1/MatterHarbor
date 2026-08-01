# Product specification

## Purpose

MatterHarbor helps municipalities, housing companies, and enterprise teams record, route, and follow up cases and incidents. The primary design goals are strong organization isolation, traceable changes, dependable asynchronous processing, accessible workflows, and operability.

## Implemented scope

The v0.1 slice supports seeded development organizations/users, development persona authentication, case creation/list/details/status changes, assignment on creation when the user belongs to the organization, idempotency, optimistic concurrency with accessible conflict recovery, immutable creation audit records, transactional outbox records, worker processing, health endpoints, and telemetry. It is limited to fictional local and CI data and is not production-ready. See README for the exact boundary.

Current v1.0 groundwork removes production startup migration privileges and produces a versioned migration bundle that CI checksums, applies to disposable PostgreSQL, and smoke-tests with the API in Production mode. No shared or production environment is deployed.

## Planned capabilities

The following are requirements, not implemented claims:

- organization administration, user provisioning, and role-based permissions;
- full case assignment and state-transition policies;
- append-only audit history for every material change;
- comments, internal notes, notifications, search, filters, and cursor pagination;
- file uploads using Blob Storage, quarantine, malware scanning, and safe download;
- command-wide idempotency and optimistic concurrency UX;
- personal-data export, deletion/anonymization, legal holds, and retention policies;
- production rate-limit policies and structured problem catalogs;
- operational dashboards, alerting, backup verification, restore runbooks, and disaster recovery;
- performance testing with k6 or NBomber.

## Core rules

- A user belongs to an organization and cannot read or mutate another organization's records.
- Organization identity comes from a verified authenticated claim.
- A case has a generated UUID, organization-scoped readable number, title, description, priority, status, optional assignee, timestamps, and integer version.
- Duplicate retries with the same idempotency key and normalized payload return the original result; changed payloads conflict.
- History is append-only and records actor, organization, action, entity, and timestamp without duplicating sensitive case text.
- Durable external messages originate from the transactional outbox.

## Non-functional goals

Accessibility targets WCAG 2.2 AA. APIs return RFC 9457-style problem details, lists are bounded, and telemetry must not contain sensitive case content. The target cloud is Azure Container Apps with PostgreSQL Flexible Server, Service Bus, Blob Storage, Key Vault, managed identities, and Azure Monitor. No cloud environment is deployed by this repository today.
