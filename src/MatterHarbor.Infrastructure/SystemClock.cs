using MatterHarbor.Application.Abstractions;

namespace MatterHarbor.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
