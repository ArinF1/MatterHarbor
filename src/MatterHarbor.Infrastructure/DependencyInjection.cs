using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Application.Cases;
using MatterHarbor.Infrastructure.Messaging;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMatterHarborInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MatterHarbor");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:MatterHarbor is required.");
        }

        services.AddDbContext<MatterHarborDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MatterHarborDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor");
            }));
        services.AddScoped<ICaseStore, CaseStore>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<CreateCaseService>();
        services.AddScoped<CaseQueryService>();
        services.AddScoped<ChangeCaseStatusService>();
        services.AddScoped<OutboxProcessor>();

        var transport = configuration["Messaging:Transport"] ?? "Local";
        if (string.Equals(transport, "AzureServiceBus", StringComparison.OrdinalIgnoreCase))
        {
            var fullyQualifiedNamespace = configuration["Messaging:ServiceBus:FullyQualifiedNamespace"];
            var queueName = configuration["Messaging:ServiceBus:QueueName"];
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace) || string.IsNullOrWhiteSpace(queueName))
            {
                throw new InvalidOperationException("Service Bus namespace and queue name are required.");
            }
            services.AddSingleton(new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential()));
            services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(queueName));
            services.AddScoped<IOutboxPublisher, AzureServiceBusOutboxPublisher>();
        }
        else if (string.Equals(transport, "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IOutboxPublisher, LocalNotificationPublisher>();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported messaging transport '{transport}'.");
        }

        return services;
    }
}
