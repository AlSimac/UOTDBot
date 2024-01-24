using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UOTDBot.Extensions;

namespace UOTDBot;

public interface IScheduler
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class Scheduler : BackgroundService, IScheduler
{
    private bool _fired = false;

    private readonly TotdChecker _totdChecker;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(TotdChecker totdChecker, TimeProvider timeProvider, IConfiguration config, ILogger<Scheduler> logger)
    {
        _totdChecker = totdChecker;
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

        try
        {
            _fired = await _totdChecker.CheckAsync(currentCestDateTime.Day, cancellationToken);
        }
        catch (Exception ex)
        {
            _fired = true;
            _logger.LogCritical(ex, "Exception while checking TOTD.");
        }
    }
}
