using GBX.NET.Engines.Game;
using GBX.NET;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UOTDBot.Models;
using System.Collections.Immutable;
using TmEssentials;

namespace UOTDBot;

internal sealed class CarChecker
{
    private static readonly ImmutableArray<string> environments = ["Snow", "Rally", "Desert", "Stadium"];

    private readonly ILogger<CarChecker> _logger;

    public CarChecker(ILogger<CarChecker> logger)
    {
        _logger = logger;
    }

    // 02/09/2020 Load the GBX
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private CGameCtnChallenge LoadGBX(Stream stream)
    {
        _logger.LogInformation("Loading Gbx...");

        // 31/08/2020 Reading GBX
        var map = GameBox.ParseNode<CGameCtnChallenge>(stream);

        _logger.LogInformation("Gbx loaded.");

        return map;
    }

    public MapFeatures CheckMap(Stream stream, out bool isUotd)
    {
        var map = LoadGBX(stream);
        return CheckMap(map, out isUotd);
    }

    // 01/01/2024 Main method to check the default car and all car change gates
    private MapFeatures CheckMap(CGameCtnChallenge map, out bool isUotd)
    {
        _logger.LogInformation("Reading cars played in {MapName}...", TextFormatter.Deformat(map.MapName));

        var defaultCar = map.PlayerModel?.Id;
        var gates = new HashSet<string>();

        // 01/01/2024 if PlayerModel is empty, it's CarSport
        if (string.IsNullOrEmpty(defaultCar))
        {
            defaultCar = "CarSport";
        }

        isUotd = defaultCar != "CarSport";

        foreach (var env in environments)
        {
            // 01/01/2024 loop on blocks
            foreach (var block in map.GetBlocks())
            {
                // 01/01/2024 Need to find a better way to get all car change blocks
                // 10/02/2024 I think this is the best way until Nadeo adds more features
                if (!block.Name.Contains($"Gameplay{env}"))
                {
                    continue;
                }

                _logger.LogInformation("Found a car change gate block");
                gates.Add(env == "Stadium" ? "CarSport" : $"Car{env}");

                if (!isUotd && env != "Stadium")
                {
                    isUotd = true;
                }
            }

            foreach (var item in map.GetAnchoredObjects())
            {
                // 01/01/2024 and also a better way to list the cars
                // 10/02/2024 I think this is the best way until Nadeo adds more features
                if (!item.ItemModel.Id.Contains($"Gameplay{env}"))
                {
                    continue;
                }

                _logger.LogInformation("Found a car change gate item");
                gates.Add(env == "Stadium" ? "CarSport" : $"Car{env}");

                if (!isUotd && env != "Stadium")
                {
                    isUotd = true;
                }
            }
        }

        var features = new MapFeatures { DefaultCar = defaultCar };
        features.Gates.AddRange(gates);
        return features;
    }
}
