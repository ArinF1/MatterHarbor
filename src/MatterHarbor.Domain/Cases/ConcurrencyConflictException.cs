namespace MatterHarbor.Domain.Cases;

public sealed class ConcurrencyConflictException(Exception? innerException = null)
    : Exception("The case was changed by another user. Reload it and try again.", innerException);
