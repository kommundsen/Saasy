using System.Diagnostics;

namespace Saasy.Worker;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    internal static readonly ActivitySource ActivitySource = new("Saasy.Worker");

    private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue<int?>("Worker:HeartbeatIntervalSeconds") is { } seconds
            ? TimeSpan.FromSeconds(seconds)
            : DefaultHeartbeatInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var activity = ActivitySource.StartActivity("heartbeat"))
            {
                logger.LogInformation("Worker heartbeat at: {time}", DateTimeOffset.UtcNow);
                activity?.SetTag("worker.heartbeat.timestamp", DateTimeOffset.UtcNow.ToString("O"));
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
