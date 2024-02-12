using Microsoft.EntityFrameworkCore;

namespace UOTDBot;

internal sealed class UotdInitializer
{
    private readonly TotdChecker _totdChecker;
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UotdInitializer(
        TotdChecker totdChecker,
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _totdChecker = totdChecker;
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (await _db.Maps.AnyAsync(cancellationToken))
        {
            return;
        }

        var firstSnowCarTotdDateTime = new DateTimeOffset(2023, 11, 21, 18, 0, 0, new());

        var timeSinceSnowCarRelease = _timeProvider.GetUtcNow() - firstSnowCarTotdDateTime;
        var months = (int)Math.Ceiling(timeSinceSnowCarRelease.TotalDays / 30);

        var totds = await _totdChecker.GetMonthTotdsAsync(months, cancellationToken);

        var currentDate = firstSnowCarTotdDateTime;
        var previousDate = currentDate;

        while (currentDate < _timeProvider.GetUtcNow())
        {
            if (currentDate.Month != previousDate.Month)
            {
                totds = await _totdChecker.GetMonthTotdsAsync(--months, cancellationToken);
            }

            var totd = await _totdChecker.CheckAsync(totds, currentDate.Day, cancellationToken);

            previousDate = currentDate;
            currentDate = currentDate.AddDays(1);
        }
    }
}
