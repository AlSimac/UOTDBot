using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GBX.NET.Engines.Game;
using GBX.NET;
using GBX.NET.LZO;
using ManiaAPI.NadeoAPI;

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

        // 04/01/2024 NadeoLiveService initialization and fectching the TOTD. I think...
        NadeoLiveServices nls = new NadeoLiveServices(true);
        int length = 1;
        int offset = 0;
        TrackOfTheDayCollection TOTDCollection = nls.GetTrackOfTheDaysAsync(length, offset, false, default);
        TOTDCollection.Deconstruct(TOTDMonth, NextRequestTimestamp, RelativeNextRequest);
        TOTDMonth.Deconstruct(Year, Month, LastDay, TOTDList, Media);
        String mapUid = TOTDList[0].mapUid;

        // TODO: wth do you do with that now

        // 02/01/2024 before NadeoAPI is set up
        String filepath = "C:\\Users\\STRpo\\Documents\\coding\\MapParsing\\maps\\cotd.Map.Gbx";

        // 02/09/2020 loading one file
        GameBox<CGameCtnChallenge> gbx = LoadGBX(filepath);
        List<String> resultList = getAllCars(gbx);

        // 01/01/2024 Do something only if the list isn't empty
        if (resultList != null)
        {

            // 01/01/2024 maybe add a car emote idk
            foreach (String result in resultList)
            {
                Console.WriteLine(result);
            }
        }

        else
        {
            Console.WriteLine("nothing");
        }

        

        return Task.CompletedTask;
    }

    // 02/09/2020 Load the GBX
    public GameBox<CGameCtnChallenge> LoadGBX(string mapPath)
    {
        Console.WriteLine("< Start ReaderProgram.LoadGBX");
        //string mapPath = "C:\\Users\\STRpo\\Documents\\Ultimate Nadeo Maps\\UpdatedFiles\\TrackMania\\1.2.3\\PuzzleD3.Challenge.gbx";

        // 31/08/2020 Reading GBX
        var gbx = GameBox.Parse<CGameCtnChallenge>(mapPath);

        Console.WriteLine("> End ReaderProgram.LoadGBX");
        return gbx;
    }

    // 01/01/2024 Main method to check the default car and all car change gates
    public List<String> getAllCars(CGameCtnChallenge map)
    {
        Console.WriteLine("< Start ReaderProgram.getAllCars");

        Console.WriteLine("[-- Writing block info for {0} --]", map.MapName);

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
                    Console.WriteLine("Found a car change gate block");
                    carList.Add("CarSnow");
                    alert = true;
                }
            }

            foreach (CGameCtnAnchoredObject item in items)
            {

                // 01/01/2024 and also a better way to list the cars
                if (item.ItemModel.Id.Contains("GameplaySnow"))
                {
                    Console.WriteLine("Found a car change gate item");
                    carList.Add("CarSnow");
                    alert = true;
                }
            }
        }

        // 01/01/2024 if default car is anything else, return true
        else
        {
            Console.WriteLine("Default car: " + defaultCar);
            alert = true;
        }

        // 01/01/2024 if there's something else than CarSport, return the list
        if (alert == true)
        {
            carList = carList.Distinct().ToList();
            Console.WriteLine("> End ReaderProgram.getAllCars");
            return carList;
        }

        // 01/01/2024 otherwise return null
        else
        {
            Console.WriteLine("> End ReaderProgram.getAllCars");
            return null;
        }


    }
}
