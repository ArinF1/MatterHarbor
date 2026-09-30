using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;
using MatterHarbor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task Roles_assignment_and_retry_contract_are_enforced()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var created = await CreateCaseAsync(client, "alex", $"roles-{Guid.NewGuid():N}");

        using var viewerGet = Request(HttpMethod.Get, $"/api/cases/{created.Id}", "jordan");
        var viewerGetResponse = await client.SendAsync(viewerGet);
        Assert.Equal(HttpStatusCode.OK, viewerGetResponse.StatusCode);
        Assert.Equal("\"v1\"", viewerGetResponse.Headers.ETag?.ToString());

        using var viewerCreate = Request(HttpMethod.Post, "/api/cases", "jordan", new
        {
            title = "Denied",
            description = "Fictional",
            priority = "Normal",
            assignedUserId = (Guid?)null
        });
        viewerCreate.Headers.Add("Idempotency-Key", $"denied-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(viewerCreate), 403, "case-access-denied");

        using var workerCreate = Request(HttpMethod.Post, "/api/cases", "taylor", new
        {
            title = "Worker case",
            description = "Fictional",
            priority = "Normal",
            assignedUserId = DatabaseInitialization.TaylorUserId
        });
        workerCreate.Headers.Add("Idempotency-Key", $"worker-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(workerCreate)).StatusCode);

        using var viewerAssignment = Request(HttpMethod.Put, $"/api/cases/{created.Id}/assignment", "jordan", new
        {
            assignedUserId = DatabaseInitialization.TaylorUserId
        });
        viewerAssignment.Headers.Add("If-Match", "\"v1\"");
        viewerAssignment.Headers.Add("Idempotency-Key", $"denied-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(viewerAssignment), 403, "case-access-denied");

        using var workerChange = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "taylor", new { status = "InProgress" });
        workerChange.Headers.Add("If-Match", "\"v1\"");
        workerChange.Headers.Add("Idempotency-Key", $"denied-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(workerChange), 403, "case-access-denied");

        using var missingPrecondition = Request(HttpMethod.Put, $"/api/cases/{created.Id}/assignment", "alex", new
        {
            assignedUserId = DatabaseInitialization.TaylorUserId
        });
        missingPrecondition.Headers.Add("Idempotency-Key", $"missing-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(missingPrecondition), 428, "precondition-required");

        using var foreignAssignment = Request(HttpMethod.Put, $"/api/cases/{created.Id}/assignment", "alex", new
        {
            assignedUserId = DatabaseInitialization.CaseyUserId
        });
        foreignAssignment.Headers.Add("If-Match", "\"v1\"");
        foreignAssignment.Headers.Add("Idempotency-Key", $"foreign-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(foreignAssignment), 400, "validation-error");

        var assignmentKey = $"assignment-{Guid.NewGuid():N}";
        using var assign = Request(HttpMethod.Put, $"/api/cases/{created.Id}/assignment", "alex", new
        {
            assignedUserId = DatabaseInitialization.TaylorUserId
        });
        assign.Headers.Add("If-Match", "\"v1\"");
        assign.Headers.Add("Idempotency-Key", assignmentKey);
        var assignmentResponse = await client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.OK, assignmentResponse.StatusCode);
        var assigned = await assignmentResponse.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions);
        Assert.Equal(2, assigned?.Version);
        Assert.Equal(DatabaseInitialization.TaylorUserId, assigned?.AssignedUserId);

        using var replay = Request(HttpMethod.Put, $"/api/cases/{created.Id}/assignment", "alex", new
        {
            assignedUserId = DatabaseInitialization.TaylorUserId
        });
        replay.Headers.Add("If-Match", "\"v1\"");
        replay.Headers.Add("Idempotency-Key", assignmentKey);
        var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal("true", replayResponse.Headers.GetValues("Idempotency-Replayed").Single());

        using var stale = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "taylor", new { status = "InProgress" });
        stale.Headers.Add("If-Match", "\"v1\"");
        stale.Headers.Add("Idempotency-Key", $"stale-{Guid.NewGuid():N}");
        await AssertProblemAsync(await client.SendAsync(stale), 409, "concurrency-conflict");

        var statusKey = $"status-{Guid.NewGuid():N}";
        using var status = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "taylor", new { status = "InProgress" });
        status.Headers.Add("If-Match", "\"v2\"");
        status.Headers.Add("Idempotency-Key", statusKey);
        var statusResponse = await client.SendAsync(status);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal("\"v3\"", statusResponse.Headers.ETag?.ToString());

        using var statusReplay = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "taylor", new { status = "InProgress" });
        statusReplay.Headers.Add("If-Match", "\"v2\"");
        statusReplay.Headers.Add("Idempotency-Key", statusKey);
        var statusReplayResponse = await client.SendAsync(statusReplay);
        Assert.Equal(HttpStatusCode.OK, statusReplayResponse.StatusCode);
        Assert.Equal("true", statusReplayResponse.Headers.GetValues("Idempotency-Replayed").Single());

        await using var db = database.CreateContext();
        Assert.Equal(3, await db.AuditEntries.CountAsync(x => x.EntityId == created.Id));
    }

    [Fact]
    public async Task Concurrent_status_updates_commit_one_audit_and_one_version()
    {
        await using var factory = new ApiWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var created = await CreateCaseAsync(client, "alex", $"concurrent-{Guid.NewGuid():N}");
        using var first = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "alex", new { status = "InProgress" });
        first.Headers.Add("If-Match", "\"v1\"");
        first.Headers.Add("Idempotency-Key", $"first-{Guid.NewGuid():N}");
        using var second = Request(HttpMethod.Put, $"/api/cases/{created.Id}/status", "alex", new { status = "InProgress" });
        second.Headers.Add("If-Match", "\"v1\"");
        second.Headers.Add("Idempotency-Key", $"second-{Guid.NewGuid():N}");

        var responses = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var db = database.CreateContext();
        Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.EntityId == created.Id));
        Assert.Equal(2, (await db.Cases.SingleAsync(x => x.Id == created.Id)).Version);
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
