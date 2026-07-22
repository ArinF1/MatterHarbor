namespace MatterHarbor.Application.Cases;

public sealed class IdempotencyConflictException()
    : Exception("The idempotency key was already used with a different request.");

public sealed class AssignedUserNotFoundException()
    : Exception("The assigned user does not belong to this organization.");

public sealed class CaseNotFoundException() : Exception("The case was not found.");
