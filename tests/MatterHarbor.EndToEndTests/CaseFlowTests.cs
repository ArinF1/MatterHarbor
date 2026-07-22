using Microsoft.Playwright;

namespace MatterHarbor.EndToEndTests;

public sealed class CaseFlowTests
{
    [Fact(Skip = "Set MATTERHARBOR_E2E_BASE_URL and install Playwright browsers to run the live browser test.")]
    public async Task User_can_create_and_open_a_case()
    {
        var baseUrl = Environment.GetEnvironmentVariable("MATTERHARBOR_E2E_BASE_URL")
            ?? throw new InvalidOperationException("MATTERHARBOR_E2E_BASE_URL is required.");
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
    }
}
