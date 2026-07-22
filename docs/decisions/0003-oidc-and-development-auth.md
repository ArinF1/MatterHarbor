# ADR 0003: OIDC/Entra authentication with development-only local auth

- Status: Accepted
- Date: 2026-07-22

## Decision

Validate production bearer tokens from a configurable HTTPS OIDC authority and audience, compatible with Microsoft Entra ID. Require stable user and `org_id` claims. Permit header-based fictional personas only when both configuration and host environment select Development; otherwise startup fails.

## Consequences

Local work needs no tenant. Production provisioning and role mapping remain explicit work. Developers must never infer organization from request payloads or URLs.
