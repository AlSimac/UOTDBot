using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UOTDBot;
public interface IScheduler
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class Scheduler : BackgroundService, IScheduler
{
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(ILogger<Scheduler> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduler...");

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
        }
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        // ... something every second ...

        return Task.CompletedTask;
    }
}
