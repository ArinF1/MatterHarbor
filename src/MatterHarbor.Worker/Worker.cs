using MatterHarbor.Infrastructure.Messaging;

namespace MatterHarbor.Worker;

public sealed partial class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            try
            {
                var count = await processor.ProcessBatchAsync(20, stoppingToken);
                if (count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogPollingFailure(logger, exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    [LoggerMessage(3001, LogLevel.Information, "MatterHarbor outbox worker started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(3002, LogLevel.Error, "Outbox polling failed with {ErrorCode}")]
    private static partial void LogPollingFailure(ILogger logger, string errorCode);
}
