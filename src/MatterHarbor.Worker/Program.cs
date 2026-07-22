using MatterHarbor.Infrastructure;
using MatterHarbor.Infrastructure.Messaging;
using MatterHarbor.Worker;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddMatterHarborInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MatterHarbor.Worker"));
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MatterHarbor.Worker"))
    .WithTracing(tracing => tracing
        .AddSource(OutboxProcessor.ActivitySourceName)
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql"))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
{
    telemetry.WithTracing(tracing => tracing.AddOtlpExporter(options => options.Endpoint = endpoint));
    telemetry.WithMetrics(metrics => metrics.AddOtlpExporter(options => options.Endpoint = endpoint));
    builder.Logging.AddOpenTelemetry(options => options.AddOtlpExporter(exporter => exporter.Endpoint = endpoint));
}

var host = builder.Build();
await host.RunAsync();
