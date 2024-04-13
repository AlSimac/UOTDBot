using Microsoft.Extensions.Logging;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Configuration;

namespace UOTDBot;

internal sealed class TotdChecker
{
    private readonly NadeoLiveServices _nls;
    private readonly NadeoClubServices _ncs;
    private readonly TrackmaniaIO _tmio;
    private readonly HttpClient _http;
    private readonly CarChecker _carChecker;
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<TotdChecker> _logger;

    public TotdChecker(
        NadeoLiveServices nls,
        NadeoClubServices ncs,
        TrackmaniaIO tmio,
        HttpClient http,
        CarChecker carChecker,
        AppDbContext db,
        TimeProvider timeProvider,
        IConfiguration config,
        ILogger<TotdChecker> logger)
    {
        _nls = nls;
        _ncs = ncs;
        _tmio = tmio;
        _http = http;
        _carChecker = carChecker;
        _db = db;
        _timeProvider = timeProvider;
        _config = config;
        _logger = logger;
    }

    public async Task<TrackOfTheDayCollection> GetMonthTotdsAsync(int monthsBack, CancellationToken cancellationToken)
    {
        return await _nls.GetTrackOfTheDaysAsync(length: 1, offset: monthsBack, cancellationToken: cancellationToken);
    }

    public async Task<Models.Map?> CheckAsync(int day, int monthsBack, CancellationToken cancellationToken)
    {
        var totds = await GetMonthTotdsAsync(monthsBack, cancellationToken);
        return await CheckAsync(totds, day, cancellationToken);
    }

    public async Task<Models.Map?> CheckAsync(TrackOfTheDayCollection totds, int day, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for TOTD day {Day}...", day);

        if (totds.MonthList.Length == 0)
        {
            _logger.LogError("No TOTD found (empty month list).");
            return null;
        }

        var month = totds.MonthList[0];
        _logger.LogInformation("TOTD month: {Month}", month.Month);

        if (month.Days.Length == 0)
        {
            _logger.LogCritical("No TOTD found (empty day list).");
            return null;
        }

        if (day > month.Days.Length)
        {
            _logger.LogCritical("No TOTD found (day out of range).");
            return null;
        }

        var dayInfo = month.Days[day - 1];
        var mapUid = dayInfo.MapUid;

        if (string.IsNullOrEmpty(mapUid))
        {
            _logger.LogError("No TOTD found (empty map UID). Is Scheduler:StartTime too early?");
            return null;
        }

        _logger.LogInformation("Found TOTD day {MonthDay} (MapUid: {MapUid})", dayInfo.MonthDay, mapUid);
        _logger.LogDebug("TOTD details: {DayInfo}", day);

        var mapModel = await _db.Maps.FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);

        if (mapModel is not null)
        {
            _logger.LogInformation("Map already exists in database (MapUid: {MapUid}). Nothing to report.", mapUid);
            return null;
        }

        _logger.LogInformation("Checking map details (MapUid: {MapUid})...", mapUid);

        var mapInfo = await _nls.GetMapInfoAsync(mapUid, cancellationToken);
        
        _logger.LogInformation("Map details: {MapInfo}", mapInfo);

        using var mapResponse = await _http.GetAsync(mapInfo.DownloadUrl, cancellationToken);

        mapResponse.EnsureSuccessStatusCode();

        using var mapStream = await mapResponse.Content.ReadAsStreamAsync(cancellationToken);

        var features = _carChecker.CheckMap(mapStream, out var isUotd, out var raceValidateGhost);

        if (isUotd)
        {
            _logger.LogInformation("UOTD elements found. Checking the WR if it has a UOTD car...");

            var carDistrib = features.CarDistribution = await _carChecker.DownloadAndCheckWrGhostAsync(
                mapUid, features.DefaultCar, backupGhost: raceValidateGhost, cancellationToken);

            if (carDistrib is not null)
            {
                _ = carDistrib.TryGetValue("CarSport", out var carSportLength);
                var nonCarSportLength = carDistrib.Where(x => x.Key != "CarSport").Sum(x => x.Value);
                
                if (nonCarSportLength == 0)
                {
                    _logger.LogInformation("Map has only CarSport, definitely not an UOTD (MapUid: {MapUid}).", mapUid);
                    isUotd = false;
                }
                
                var nonStadDistrib = features.NonStadiumDistribution = nonCarSportLength / (float)(carSportLength + nonCarSportLength);
            
                if (nonStadDistrib > 0.01f)
                {
                    _logger.LogInformation("Map has more than 99% of CarSport, definitely not an UOTD (MapUid: {MapUid}).", mapUid);
                    isUotd = false;
                }
            }
        }

        _ = bool.TryParse(_config["AllowAllTOTD"], out var allowAllTotd);

        if (!allowAllTotd && !isUotd)
        {
            _logger.LogInformation("Map is not an UOTD (MapUid: {MapUid}).", mapUid);
            return null;
        }

        // TM.IO get map not tested enough, make sure to not crash if it fails
        var authorName = default(string);
        
        try
        {
            var mapOnTmIo = await _tmio.GetMapInfoAsync(mapInfo.Uid, cancellationToken);
            authorName = mapOnTmIo.AuthorPlayer.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get map info from trackmania.io (MapUid: {MapUid}).", mapUid);
        }

        var cupId = default(int?);

        // TODO: this check does not consider reruns, needs rework
        if (_timeProvider.GetUtcNow().Day == dayInfo.MonthDay)
        {
            try
            { 
                var cotd = await _ncs.GetCurrentCupOfTheDayAsync(cancellationToken);
                _logger.LogInformation("NadeoClubServices: {Cotd}", cotd);
                cupId = cotd.Competition.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current cup of the day.");
            }
        }

        mapModel = new Models.Map
        {
            MapId = mapInfo.MapId,
            MapUid = mapInfo.Uid,
            Name = mapInfo.Name,
            ThumbnailUrl = mapInfo.ThumbnailUrl,
            DownloadUrl = mapInfo.DownloadUrl,
            AuthorTime = mapInfo.AuthorTime,
            FileSize = (int)mapResponse.Content.Headers.ContentLength.GetValueOrDefault(),
            UploadedAt = mapInfo.UploadTimestamp,
            UpdatedAt = mapInfo.UpdateTimestamp,
            Features = features,
            Totd = new DateOnly(month.Year, month.Month, dayInfo.MonthDay),
            AuthorGuid = mapInfo.Author,
            AuthorName = authorName,
            CupId = cupId
        };

        await _db.Maps.AddAsync(mapModel, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return mapModel;
    }
}
