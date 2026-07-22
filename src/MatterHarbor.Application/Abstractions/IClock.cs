namespace MatterHarbor.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
