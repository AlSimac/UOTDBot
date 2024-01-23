using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GBX.NET.Engines.Game;
using GBX.NET;
using ManiaAPI.NadeoAPI;
using System.Diagnostics.CodeAnalysis;
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

    private readonly NadeoLiveServices _nls;
    private readonly HttpClient _http;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(NadeoLiveServices nls, HttpClient http, TimeProvider timeProvider, IConfiguration config, ILogger<Scheduler> logger)
    {
        _nls = nls;
        _http = http;
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

        if (timeOfDay >= startTime && timeOfDay <= endTime)
        {
            if (_fired)
            {
                return;
            }

            _logger.LogInformation("Checking for TOTD...");

            var totds = await _nls.GetTrackOfTheDaysAsync(length: 1, cancellationToken: cancellationToken);

            if (totds.MonthList.Length == 0)
            {
                _logger.LogError("No TOTD found (empty month list).");
                return;
            }

            var month = totds.MonthList[0];

            if (month.Days.Length == 0)
            {
                _logger.LogCritical("No TOTD found (empty day list).");
                return;
            }

            if (currentCestDateTime.Day > month.Days.Length)
            {
                _logger.LogCritical("No TOTD found (day out of range).");
                return;
            }

            var day = month.Days[currentCestDateTime.Day - 1];

            if (string.IsNullOrEmpty(day.MapUid)) 
            {                 
                _logger.LogCritical("No TOTD found (empty map UID).");
                return;
            }

            _logger.LogInformation("Found TOTD day {MonthDay} (MapUid: {MapUid})", day.MonthDay, day.MapUid);
            _logger.LogDebug("TOTD details: {DayInfo}", day);
            _logger.LogInformation("Checking map details (MapUid: {MapUid})...", day.MapUid);

            var mapInfo = await _nls.GetMapInfoAsync(day.MapUid, cancellationToken);

            _logger.LogDebug("Map details: {MapInfo}", mapInfo);

            using var mapResponse = await _http.GetAsync(mapInfo.DownloadUrl, cancellationToken);

            mapResponse.EnsureSuccessStatusCode();

            using var mapStream = await mapResponse.Content.ReadAsStreamAsync(cancellationToken);

            var map = LoadGBX(mapStream);

            CheckMap(map);

            _fired = true;
        }
        else
        {
            _fired = false;
        }
    }

    // 02/09/2020 Load the GBX
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public CGameCtnChallenge LoadGBX(Stream stream)
    {
        _logger.LogInformation("< Start Scheduler.LoadGBX");

        // 31/08/2020 Reading GBX
        var map = GameBox.ParseNode<CGameCtnChallenge>(stream);

        _logger.LogInformation("> End Scheduler.LoadGBX");

        return map;
    }

    private void CheckMap(CGameCtnChallenge map)
    {
        List<string> resultList = getAllCars(map);

        // 01/01/2024 Do something only if the list isn't empty
        if (resultList != null)
        {

            // 01/01/2024 maybe add a car emote idk
            foreach (string result in resultList)
            {
                _logger.LogInformation(result);
            }
        }

        else
        {
            _logger.LogInformation("nothing");
        }
    }

    // 01/01/2024 Main method to check the default car and all car change gates
    public List<String> getAllCars(CGameCtnChallenge map)
    {
        _logger.LogInformation("< Start Scheduler.getAllCars");

        _logger.LogInformation("[-- Reading cars played in {0} --]", map.MapName);

        List<String> carList = new List<String>();
        bool alert = false;
        string defaultCar = map.PlayerModel.Id;

        // 01/01/2024 if PlayerModel is empty, it's CarSport
        if (defaultCar == "")
        {
            defaultCar = "CarSport";
        }

        carList.Add(defaultCar);

        // 01/01/2024 if CarSport, check if there are car change Gates
        if (defaultCar == "CarSport" || defaultCar == null)
        {
            // 01/01/2024 loop on blocks
            foreach (CGameCtnBlock block in map.GetBlocks())
            {
                // 01/01/2024 Need to find a better way to get all car change blocks
                if (block.Name.Contains("GameplaySnow"))
                {
                    _logger.LogInformation("Found a car change gate block");
                    carList.Add("CarSnow");
                    alert = true;
                }
            }

            foreach (CGameCtnAnchoredObject item in map.GetAnchoredObjects())
            {

                // 01/01/2024 and also a better way to list the cars
                if (item.ItemModel.Id.Contains("GameplaySnow"))
                {
                    _logger.LogInformation("Found a car change gate item");
                    carList.Add("CarSnow");
                    alert = true;
                }
            }
        }

        // 01/01/2024 if default car is anything else, return true
        else
        {
            _logger.LogInformation("Default car: {DefaultCar}", defaultCar);
            alert = true;
        }

        // 01/01/2024 if there's something else than CarSport, return the list
        if (alert == true)
        {
            carList = carList.Distinct().ToList();
            _logger.LogInformation("> End Scheduler.getAllCars");
            return carList;
        }

        // 01/01/2024 otherwise return null
        else
        {
            _logger.LogInformation("> End Scheduler.getAllCars");
            return null;
        }


    }
}
