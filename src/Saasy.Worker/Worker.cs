namespace Saasy.Worker;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue<int?>("Worker:HeartbeatIntervalSeconds") is { } seconds
            ? TimeSpan.FromSeconds(seconds)
            : DefaultHeartbeatInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker heartbeat at: {time}", DateTimeOffset.UtcNow);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
