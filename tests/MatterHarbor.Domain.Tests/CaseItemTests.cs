using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Domain.Tests;

public sealed class CaseItemTests
{
    [Fact]
    public void Create_rejects_invalid_priority()
    {
        Assert.Throws<DomainValidationException>(() => CaseItem.Create(
            Guid.NewGuid(),
            "OC-1",
            "Broken streetlight",
            "Lamp is dark",
            (CasePriority)999,
            null,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChangeStatus_rejects_invalid_status()
    {
        var item = CreateCase();

        Assert.Throws<DomainValidationException>(() =>
            item.ChangeStatus((CaseStatus)999, item.Version, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChangeStatus_rejects_stale_version()
    {
        var item = CreateCase();

        Assert.Throws<ConcurrencyConflictException>(() =>
            item.ChangeStatus(CaseStatus.InProgress, item.Version + 1, DateTimeOffset.UtcNow));
    }

    private static CaseItem CreateCase() => CaseItem.Create(
        Guid.NewGuid(),
        "OC-1",
        "Broken streetlight",
        "Lamp is dark",
        CasePriority.Normal,
        null,
        DateTimeOffset.UtcNow);
}
