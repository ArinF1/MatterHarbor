-- Fictional compatibility fixture. Never replace this with real personal data.
BEGIN;

INSERT INTO matterharbor.organizations ("Id", "Name")
VALUES ('11111111-1111-1111-1111-111111111111', 'Northwind Municipality');

INSERT INTO matterharbor.organization_users (
    "Id",
    "OrganizationId",
    "ExternalSubject",
    "DisplayName")
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '11111111-1111-1111-1111-111111111111',
    'fixture-alex',
    'Alex Morgan');

INSERT INTO matterharbor.cases (
    "Id",
    "OrganizationId",
    "CaseNumber",
    "Title",
    "Description",
    "Priority",
    "Status",
    "AssignedUserId",
    "CreatedAt",
    "UpdatedAt",
    "Version")
VALUES (
    '33333333-3333-3333-3333-333333333333',
    '11111111-1111-1111-1111-111111111111',
    'MH-0001',
    'Fictional migration fixture',
    'Synthetic data used only to test schema upgrades.',
    'Normal',
    'Open',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '2026-07-01T10:00:00Z',
    '2026-07-01T10:00:00Z',
    1);

INSERT INTO matterharbor.audit_entries (
    "Id",
    "OrganizationId",
    "ActorUserId",
    "EntityId",
    "Action",
    "OccurredAt")
VALUES (
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '33333333-3333-3333-3333-333333333333',
    'CaseCreated',
    '2026-07-01T10:00:00Z');

INSERT INTO matterharbor.idempotency_records (
    "OrganizationId",
    "Key",
    "RequestHash",
    "ResponseJson",
    "CreatedAt")
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'fixture-create-case',
    '0000000000000000000000000000000000000000000000000000000000000000',
    '{"id":"33333333-3333-3333-3333-333333333333","caseNumber":"MH-0001"}',
    '2026-07-01T10:00:00Z');

INSERT INTO matterharbor.outbox_messages (
    "Id",
    "OrganizationId",
    "Type",
    "Payload",
    "OccurredAt",
    "Status",
    "AttemptCount")
VALUES (
    '55555555-5555-5555-5555-555555555555',
    '11111111-1111-1111-1111-111111111111',
    'CaseCreated',
    '{"caseId":"33333333-3333-3333-3333-333333333333"}',
    '2026-07-01T10:00:00Z',
    'Pending',
    0);

COMMIT;
