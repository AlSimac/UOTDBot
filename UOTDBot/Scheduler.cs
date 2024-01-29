using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UOTDBot.Extensions;
using UOTDBot.Models;

namespace UOTDBot;

public interface IScheduler
{
    Task TickAsync(CancellationToken cancellationToken);
}

internal sealed class Scheduler : BackgroundService, IScheduler
{
    private bool _fired = false;

    private readonly IServiceProvider _provider;
    private readonly IHostEnvironment _env;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(
        IServiceProvider provider,
        IHostEnvironment env,
        TimeProvider timeProvider,
        IConfiguration config,
        ILogger<Scheduler> logger)
    {
        _provider = provider;
        _env = env;
        _timeProvider = timeProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Quick test for TOTD report before 19:00
        if (_env.IsDevelopment() && DateTimeOffset.Now.Day - 1 > 0 && DateTimeOffset.Now.TimeOfDay < TimeSpan.Parse(_config.GetRequiredValue("Scheduler:StartTime")))
        {
            await CheckAndReportTotdAsync(DateTimeOffset.Now.Day - 1, stoppingToken);
            return;
        }

        _logger.LogInformation("Starting scheduler...");

        using var periodicTimer = new PeriodicTimer(TimeSpan.Parse(_config.GetRequiredValue("Scheduler:Interval")));

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await TickAsync(stoppingToken);
        }
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        // ... something every second ...

        var currentCestDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_timeProvider.GetUtcNow(), "Central European Standard Time");

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

        await CheckAndReportTotdAsync(currentCestDateTime.Day, cancellationToken);
    }

    private async Task CheckAndReportTotdAsync(int day, CancellationToken cancellationToken)
    {
        await using var scope = _provider.CreateAsyncScope();

        var map = default(Map);

        try
        {
            map = await scope.ServiceProvider
                .GetRequiredService<TotdChecker>()
                .CheckAsync(day, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while checking for TOTD.");
        }

        _fired = true;

        if (map is not null)
        {
            await scope.ServiceProvider
                .GetRequiredService<DiscordReporter>()
                .ReportAsync(map, cancellationToken);
        }
    }
}
