using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Application.Tests;

public sealed class IdempotencyHasherTests
{
    [Fact]
    public void Equivalent_requests_have_the_same_hash()
    {
        var first = new CreateCaseCommand("  Title ", " Description ", CasePriority.High, null);
        var second = new CreateCaseCommand("Title", "Description", CasePriority.High, null);

        Assert.Equal(IdempotencyHasher.Hash(first), IdempotencyHasher.Hash(second));
    }

    [Fact]
    public void Different_requests_have_different_hashes()
    {
        var first = new CreateCaseCommand("Title", "Description", CasePriority.High, null);
        var second = first with { Title = "Another title" };

        Assert.NotEqual(IdempotencyHasher.Hash(first), IdempotencyHasher.Hash(second));
    }
}
