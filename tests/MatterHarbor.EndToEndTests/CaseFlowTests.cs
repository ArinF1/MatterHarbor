using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.EndToEndTests;

public sealed class CaseFlowTests
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task User_can_create_and_open_a_case()
    {
        var baseUrl = Environment.GetEnvironmentVariable("MATTERHARBOR_E2E_BASE_URL")
            ?? throw new InvalidOperationException("MATTERHARBOR_E2E_BASE_URL is required.");
        var connectionString = Environment.GetEnvironmentVariable("MATTERHARBOR_E2E_CONNECTION_STRING")
            ?? throw new InvalidOperationException("MATTERHARBOR_E2E_CONNECTION_STRING is required.");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync(baseUrl);
        await page.GetByLabel("Development persona").SelectOptionAsync("alex");
        await page.GetByRole(AriaRole.Link, new() { Name = "Create case" }).ClickAsync();
        await page.GetByLabel("Title").FillAsync("Broken streetlight");
        await page.GetByLabel("Description").FillAsync("Lamp outside the library is dark.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create case" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Broken streetlight" }))
            .ToBeVisibleAsync();

        var options = new DbContextOptionsBuilder<MatterHarborDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor"))
            .Options;
        await using var context = new MatterHarborDbContext(options);
        var workerProcessedMessage = false;
        for (var attempt = 0; attempt < 20 && !workerProcessedMessage; attempt++)
        {
            workerProcessedMessage = await context.OutboxMessages
                .AsNoTracking()
                .AnyAsync(message => message.Status == OutboxStatus.Processed);
            if (!workerProcessedMessage)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        Assert.True(workerProcessedMessage, "The worker did not process the case-created outbox message.");
    }
}
