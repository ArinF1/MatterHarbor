namespace MatterHarbor.Domain.Cases;

public sealed class DomainValidationException(string message) : Exception(message);
