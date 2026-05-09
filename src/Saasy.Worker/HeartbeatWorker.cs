using System.Diagnostics;

namespace Saasy.Worker;

public class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    internal static readonly ActivitySource ActivitySource = new("Saasy.Worker");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = ActivitySource.StartActivity("heartbeat");

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
