using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GBX.NET.Engines.Game;
using GBX.NET;
using ManiaAPI.NadeoAPI;
using System.Diagnostics.CodeAnalysis;

namespace UOTDBot;

public interface IScheduler
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class Scheduler : BackgroundService, IScheduler
{
    private readonly NadeoLiveServices _nls;
    private readonly HttpClient _http;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(NadeoLiveServices nls, HttpClient http, ILogger<Scheduler> logger)
    {
        _nls = nls;
        _http = http;
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

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // ... something every second ...

        // 04/01/2024 NadeoLiveService initialization
        string downloadFolder = "maps/";
        string filepath = downloadFolder + "cotd.Map.gbx";

        // need to check these parameters
        int length = 1;
        int offset = 0;
        int month = 0;
        int day = 0;

        // 05/01/2024 Fetching TOTD
        TrackOfTheDayCollection TOTDCollection = await _nls.GetTrackOfTheDaysAsync(length, cancellationToken: cancellationToken);
        TrackOfTheDayMonth[] TOTDMonth = TOTDCollection.MonthList;
        TrackOfTheDay[] TOTDList = TOTDMonth[month].Days;
        string mapUid = TOTDList[day].MapUid;

        // 05/01/2024 TODO: Find how to get storage Id from map UID
        //the fileUrl is in MapInfo. Got to find how to get from MapUid to MapInfo
        MapInfo mapInfo = await _nls.GetMapInfoAsync(mapUid, cancellationToken);
        
        using var mapStream = await _http.GetStreamAsync(mapInfo.DownloadUrl, cancellationToken);

        // 05/01/2024 parsing the GBX and checking for cars
        GameBox<CGameCtnChallenge> gbx = LoadGBX(mapStream);
        List<string> resultList = getAllCars(gbx);

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

    // 02/09/2020 Load the GBX
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public GameBox<CGameCtnChallenge> LoadGBX(Stream stream)
    {
        _logger.LogInformation("< Start Scheduler.LoadGBX");

        // 31/08/2020 Reading GBX
        var gbx = GameBox.Parse<CGameCtnChallenge>(stream);

        _logger.LogInformation("> End Scheduler.LoadGBX");

        return gbx;
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
            IList<CGameCtnBlock> blocks = map.Blocks;
            IList<CGameCtnAnchoredObject> items = map.AnchoredObjects;

            // 01/01/2024 loop on blocks
            foreach (CGameCtnBlock block in blocks)
            {
                // 01/01/2024 Need to find a better way to get all car change blocks
                if (block.Name.Contains("GameplaySnow"))
                {
                    _logger.LogInformation("Found a car change gate block");
                    carList.Add("CarSnow");
                    alert = true;
                }
            }

            foreach (CGameCtnAnchoredObject item in items)
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
