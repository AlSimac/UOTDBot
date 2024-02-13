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
        if (_env.IsDevelopment() && _timeProvider.GetLocalNow().Day - 1 > 0 && _timeProvider.GetLocalNow().TimeOfDay < TimeSpan.Parse(_config.GetRequiredValue("Scheduler:StartTime")))
        {
            await CheckAndReportTotdAsync(_timeProvider.GetLocalNow().Day - 1, stoppingToken);
        }

        _logger.LogInformation("Starting scheduler...");

        using var periodicTimer = new PeriodicTimer(TimeSpan.Parse(_config.GetRequiredValue("Scheduler:Interval")));

        _logger.LogInformation("Scheduler started, ticking.");

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

        _fired = true;

        _logger.LogInformation("Scheduler fired.");

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
                .CheckAsync(day, monthsBack: 0, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while checking for TOTD.");
        }

        if (map is not null)
        {
            await scope.ServiceProvider
                .GetRequiredService<DiscordReporter>()
                .ReportInChannelsAsync(map, cancellationToken);
        }
    }
}
