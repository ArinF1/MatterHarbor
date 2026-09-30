namespace MatterHarbor.Application.Cases;

public sealed class IdempotencyConflictException()
    : Exception("The idempotency key was already used with a different request.");

public sealed class AssignedUserNotFoundException()
    : Exception("The assignee must be an eligible case worker in this organization.");

public sealed class CaseNotFoundException() : Exception("The case was not found.");

public sealed class CaseAccessDeniedException() : Exception("This organization member cannot perform the requested case action.");
