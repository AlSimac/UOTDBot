using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UOTDBot.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace UOTDBot;

public interface IScheduler
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class Scheduler : BackgroundService, IScheduler
{
    private bool _fired = false;

    private readonly IServiceProvider _provider;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(
        IServiceProvider provider,
        TimeProvider timeProvider,
        IConfiguration config,
        ILogger<Scheduler> logger)
    {
        _provider = provider;
        _timeProvider = timeProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduler...");

        using var periodicTimer = new PeriodicTimer(TimeSpan.Parse(_config.GetRequiredValue("Scheduler:Interval")));

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // ... something every second ...

        var currentCestDateTime = _timeProvider.GetUtcNow()
            .ToOffset(TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time").BaseUtcOffset);

        var timeOfDay = currentCestDateTime.TimeOfDay;
        
        var startTime = TimeSpan.Parse(_config.GetRequiredValue("Scheduler:StartTime"));
        var endTime = TimeSpan.Parse(_config.GetRequiredValue("Scheduler:EndTime"));

        if (timeOfDay < startTime || timeOfDay > endTime)
        {
            _fired = false;
            return;
        }

        if (_fired)
        {
            return;
        }

        await using var scope = _provider.CreateAsyncScope();

        try
        {
            _fired = await scope.ServiceProvider
                .GetRequiredService<TotdChecker>()
                .CheckAsync(currentCestDateTime.Day, cancellationToken);
        }
        catch (Exception ex)
        {
            _fired = true;
            _logger.LogError(ex, "An error occured while checking for TOTD.");
        }
    }
}
