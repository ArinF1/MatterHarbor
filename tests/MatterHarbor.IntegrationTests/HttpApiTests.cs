using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.IntegrationTests;

public sealed class HttpApiTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Missing_development_identity_is_rejected()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cases");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Foreign_organization_cannot_list_or_get_a_case()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var created = await CreateCaseAsync(client, "alex", $"tenant-{Guid.NewGuid():N}");

        using var listRequest = Request(HttpMethod.Get, "/api/cases", "casey");
        var listResponse = await client.SendAsync(listRequest);
        var list = await listResponse.Content.ReadFromJsonAsync<CaseResponse[]>(JsonOptions);
        using var getRequest = Request(HttpMethod.Get, $"/api/cases/{created.Id}", "casey");
        var getResponse = await client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.DoesNotContain(list ?? [], item => item.Id == created.Id);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        await AssertProblemAsync(getResponse, 404, "case-not-found");
    }

    [Fact]
    public async Task Invalid_create_returns_problem_details()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var request = Request(
            HttpMethod.Post,
            "/api/cases",
            "alex",
            new { title = " ", description = "Description", priority = "Normal", assignedUserId = (Guid?)null });
        request.Headers.Add("Idempotency-Key", $"validation-{Guid.NewGuid():N}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await AssertProblemAsync(response, 400, "validation-error");
    }

    [Fact]
    public async Task Idempotent_replay_returns_original_response_and_replay_header()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var key = $"replay-{Guid.NewGuid():N}";
        var input = new
        {
            title = "Repeated request",
            description = "The same request may be safely retried.",
            priority = "High",
            assignedUserId = (Guid?)null
        };

        using var firstRequest = Request(HttpMethod.Post, "/api/cases", "alex", input);
        firstRequest.Headers.Add("Idempotency-Key", key);
        var firstResponse = await client.SendAsync(firstRequest);
        var first = await firstResponse.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions);
        using var replayRequest = Request(HttpMethod.Post, "/api/cases", "alex", input);
        replayRequest.Headers.Add("Idempotency-Key", key);
        var replayResponse = await client.SendAsync(replayRequest);
        var replay = await replayResponse.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal("false", firstResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.NotNull(firstResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal("true", replayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(first?.Id, replay?.Id);
    }

    [Fact]
    public async Task Reusing_idempotency_key_with_changed_payload_returns_conflict_problem()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var key = $"changed-{Guid.NewGuid():N}";

        await CreateCaseAsync(client, "alex", key);
        using var changedRequest = Request(
            HttpMethod.Post,
            "/api/cases",
            "alex",
            new
            {
                title = "Changed title",
                description = "The payload no longer matches.",
                priority = "High",
                assignedUserId = (Guid?)null
            });
        changedRequest.Headers.Add("Idempotency-Key", key);
        var response = await client.SendAsync(changedRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemAsync(response, 409, "idempotency-conflict");
    }

    [Fact]
    public async Task Api_rate_limit_returns_problem_details()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString, rateLimitPermitLimit: 2);
        using var client = factory.CreateClient();

        for (var requestNumber = 0; requestNumber < 2; requestNumber++)
        {
            using var allowedRequest = Request(HttpMethod.Get, "/api/cases", "alex");
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowedRequest)).StatusCode);
        }

        using var rejectedRequest = Request(HttpMethod.Get, "/api/cases", "alex");
        var response = await client.SendAsync(rejectedRequest);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        await AssertProblemAsync(response, 429, "rate-limit-exceeded");
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string persona, string key)
    {
        using var request = Request(
            HttpMethod.Post,
            "/api/cases",
            persona,
            new
            {
                title = "Organization-scoped case",
                description = "Fictional test data only.",
                priority = "Normal",
                assignedUserId = (Guid?)null
            });
        request.Headers.Add("Idempotency-Key", key);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions)
            ?? throw new InvalidOperationException("The API returned no case.");
    }

    private static HttpRequestMessage Request(
        HttpMethod method,
        string path,
        string persona,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-MatterHarbor-User", persona);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int expectedStatus,
        string expectedTypeSuffix)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.EndsWith(expectedTypeSuffix, problem.GetProperty("type").GetString(), StringComparison.Ordinal);
    }
}
