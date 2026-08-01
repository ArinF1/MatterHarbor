using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MatterHarbor.Api.Authentication;
using MatterHarbor.Api.Contracts;
using MatterHarbor.Api.ErrorHandling;
using MatterHarbor.Api.Health;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;
using MatterHarbor.Infrastructure;
using MatterHarbor.Infrastructure.Persistence;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddMatterHarborInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"]);

var authMode = builder.Configuration["Authentication:Mode"] ?? "Oidc";
if (string.Equals(authMode, "Development", StringComparison.OrdinalIgnoreCase))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Development authentication can only run in the Development environment.");
    }

    builder.Services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            _ => { });
}
else if (string.Equals(authMode, "Oidc", StringComparison.OrdinalIgnoreCase))
{
    var authority = builder.Configuration["Authentication:Oidc:Authority"];
    var audience = builder.Configuration["Authentication:Oidc:Audience"];
    if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
    {
        throw new InvalidOperationException("OIDC authority and audience are required.");
    }
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
}
else
{
    throw new InvalidOperationException($"Unsupported authentication mode '{authMode}'.");
}

builder.Services.AddAuthorization();
var rateLimitPermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120);
var rateLimitWindow = TimeSpan.FromSeconds(
    builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://matterharbor.dev/problems/rate-limit-exceeded",
            title = "Rate limit exceeded",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Too many requests. Wait before trying again."
        }, cancellationToken);
    };
    options.AddPolicy("api", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst("sub")?.Value ??
        httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitPermitLimit,
            Window = rateLimitWindow,
            QueueLimit = 0
        }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

var resourceBuilder = ResourceBuilder.CreateDefault().AddService("MatterHarbor.Api");
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MatterHarbor.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
{
    builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter(options => options.Endpoint = endpoint));
    builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter(options => options.Endpoint = endpoint));
    builder.Logging.AddOpenTelemetry(options => options.AddOtlpExporter(exporter => exporter.Endpoint = endpoint));
}

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        return Task.CompletedTask;
    });
    await next();
});
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/api/dev/personas", () => Results.Ok(new[]
    {
        new { key = "alex", displayName = "Alex Morgan", organization = "Northwind Municipality" },
        new { key = "casey", displayName = "Casey Lee", organization = "Contoso Housing" }
    })).AllowAnonymous();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();

var cases = app.MapGroup("/api/cases")
    .RequireAuthorization()
    .RequireRateLimiting("api");

cases.MapGet("/", async (
    HttpContext context,
    CaseQueryService service,
    int page = 1,
    int pageSize = 25,
    CancellationToken cancellationToken = default) =>
{
    var result = await service.ListAsync(context.User.GetMatterHarborUser(), page, pageSize, cancellationToken);
    return Results.Ok(result);
});

cases.MapGet("/{caseId:guid}", async (
    Guid caseId,
    HttpContext context,
    CaseQueryService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(context.User.GetMatterHarborUser(), caseId, cancellationToken);
    return Results.Ok(result);
});

cases.MapPost("/", async (
    CreateCaseRequest request,
    HttpContext context,
    CreateCaseService service,
    CancellationToken cancellationToken) =>
{
    var key = context.Request.Headers["Idempotency-Key"].ToString();
    var result = await service.ExecuteAsync(
        context.User.GetMatterHarborUser(),
        key,
        new CreateCaseCommand(request.Title, request.Description, request.Priority, request.AssignedUserId),
        cancellationToken);
    context.Response.Headers["Idempotency-Replayed"] = result.IsReplay.ToString().ToLowerInvariant();
    return result.IsReplay
        ? Results.Ok(result.Case)
        : Results.Created($"/api/cases/{result.Case.Id}", result.Case);
});

cases.MapPut("/{caseId:guid}/status", async (
    Guid caseId,
    ChangeCaseStatusRequest request,
    HttpContext context,
    ChangeCaseStatusService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ExecuteAsync(
        context.User.GetMatterHarborUser(),
        caseId,
        new ChangeCaseStatusCommand(request.Status, request.Version),
        cancellationToken);
    return Results.Ok(result);
});

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDevelopmentMatterHarborDatabaseAsync();
}

await app.RunAsync();

#pragma warning disable CA1050 // WebApplicationFactory discovers the generated top-level Program type.
public partial class Program;
#pragma warning restore CA1050
