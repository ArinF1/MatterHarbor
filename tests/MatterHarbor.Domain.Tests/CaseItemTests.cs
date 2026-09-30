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

    [Fact]
    public void ChangeStatus_allows_progression_reopening_and_closing()
    {
        var item = CreateCase();
        var now = DateTimeOffset.UtcNow;

        item.ChangeStatus(CaseStatus.InProgress, item.Version, now);
        item.ChangeStatus(CaseStatus.Resolved, item.Version, now);
        item.ChangeStatus(CaseStatus.InProgress, item.Version, now);
        item.ChangeStatus(CaseStatus.Resolved, item.Version, now);
        item.ChangeStatus(CaseStatus.Closed, item.Version, now);

        Assert.Equal(CaseStatus.Closed, item.Status);
    }

    [Theory]
    [InlineData(CaseStatus.New, CaseStatus.Resolved)]
    [InlineData(CaseStatus.New, CaseStatus.Closed)]
    [InlineData(CaseStatus.Closed, CaseStatus.InProgress)]
    [InlineData(CaseStatus.Closed, CaseStatus.Closed)]
    public void ChangeStatus_rejects_invalid_transitions(CaseStatus current, CaseStatus next)
    {
        var item = CreateCase();
        if (current != CaseStatus.New)
        {
            item.ChangeStatus(CaseStatus.InProgress, item.Version, DateTimeOffset.UtcNow);
            item.ChangeStatus(CaseStatus.Resolved, item.Version, DateTimeOffset.UtcNow);
            item.ChangeStatus(CaseStatus.Closed, item.Version, DateTimeOffset.UtcNow);
        }

        Assert.Throws<DomainValidationException>(() =>
            item.ChangeStatus(next, item.Version, DateTimeOffset.UtcNow));
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
