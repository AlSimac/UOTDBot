using GBX.NET.Engines.Game;
using GBX.NET;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UOTDBot;

internal sealed class UnitedCarChecker
{
    private readonly ILogger<UnitedCarChecker> _logger;

    public UnitedCarChecker(ILogger<UnitedCarChecker> logger)
    {
        _logger = logger;
    }

    public void CheckMap(Stream stream)
    {
        var map = LoadGBX(stream);
        CheckMap(map);
    }

    public void CheckMap(CGameCtnChallenge map)
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

    // 02/09/2020 Load the GBX
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private CGameCtnChallenge LoadGBX(Stream stream)
    {
        _logger.LogInformation("< Start Scheduler.LoadGBX");

        // 31/08/2020 Reading GBX
        var map = GameBox.ParseNode<CGameCtnChallenge>(stream);

        _logger.LogInformation("> End Scheduler.LoadGBX");

        return map;
    }

    // 01/01/2024 Main method to check the default car and all car change gates
    private List<String> getAllCars(CGameCtnChallenge map)
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
