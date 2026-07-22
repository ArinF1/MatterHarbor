using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Domain_has_no_project_dependencies()
    {
        var references = typeof(CaseItem).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, x => x.Name?.StartsWith("MatterHarbor.", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var references = typeof(CreateCaseService).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, x => x.Name is "MatterHarbor.Infrastructure" or "MatterHarbor.Api");
    }
}
