﻿using GBX.NET.Engines.Game;
using GBX.NET;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UOTDBot.Models;
using System.Collections.Immutable;
using TmEssentials;
using ManiaAPI.TrackmaniaIO;
using GBX.NET.Engines.MwFoundations;

namespace UOTDBot;

internal sealed class CarChecker
{
    private static readonly ImmutableArray<string> environments = ["Snow", "Rally", "Desert", "Stadium"];
    
    private readonly TrackmaniaIO _tmio;
    private readonly ILogger<CarChecker> _logger;

    public CarChecker(TrackmaniaIO tmio, ILogger<CarChecker> logger)
    {
        _tmio = tmio;
        _logger = logger;
    }

    // 02/09/2020 Load the GBX
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private T LoadGBX<T>(Stream stream) where T : CMwNod
    {
        _logger.LogInformation("Loading Gbx...");

        // 31/08/2020 Reading GBX
        var map = GameBox.ParseNode<T>(stream);

        _logger.LogInformation("Gbx loaded.");

        return map;
    }

    public MapFeatures CheckMap(Stream stream, out bool isUotd, out CGameCtnGhost? raceValidateGhost)
    {
        var map = LoadGBX<CGameCtnChallenge>(stream);
        raceValidateGhost = map.ChallengeParameters?.RaceValidateGhost;
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

                _logger.LogInformation("Found a car change gate block {Block}", block);
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
                if (!item.ItemModel.Id.Contains($"Gameplay{env}") && !item.ItemModel.Id.Contains($"{env}GateGameplay"))
                {
                    continue;
                }

                _logger.LogInformation("Found a car change gate item {Item}", item);
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

    public async Task<float?> DownloadAndCheckWrGhostAsync(string mapUid, string defaultCar, CGameCtnGhost? backupGhost, CancellationToken cancellationToken)
    {
        var recordList = await _tmio.GetLeaderboardAsync(mapUid, length: 1, cancellationToken: cancellationToken);

        var wr = recordList.Tops.FirstOrDefault();

        if (wr is null)
        {
            _logger.LogWarning("No WR found for the map, hoping that it's UOTD, fingers crossed (MapUid: {MapUid}).", mapUid);
            return null;
        }

        using var wrGhostResponse = await _tmio.Client.GetAsync($"https://trackmania.io{wr.Url}", cancellationToken);

        var ghost = backupGhost;

        if (wrGhostResponse.IsSuccessStatusCode)
        {
            using var ghostStream = await wrGhostResponse.Content.ReadAsStreamAsync(cancellationToken);
            ghost = LoadGBX<CGameCtnGhost>(ghostStream);
        }

        if (ghost is null)
        {
            _logger.LogWarning("Failed to get WR ghost for the map, hoping that it's UOTD, fingers crossed (MapUid: {MapUid}, StatusCode: {StatusCode}).", mapUid, wrGhostResponse.StatusCode);
            return null;
        }

        if (ghost.RecordData is null)
        {
            _logger.LogWarning("No record data found in the WR ghost, hoping that it's UOTD, fingers crossed (MapUid: {MapUid}).", mapUid);
            return null;
        }

        var arenaPlayerIndex = -1;
        for (int i = 0; i < ghost.RecordData.EntRecordDescs.Length; i++)
        {
            if (ghost.RecordData.EntRecordDescs[i].ClassId == 0x032CB000)
            {
                arenaPlayerIndex = i;
                break;
            }
        }

        if (arenaPlayerIndex == -1)
        {
            _logger.LogWarning("No CGameArenaPlayer found in the WR ghost (EntRecordDesc not found), hoping that it's UOTD, fingers crossed (MapUid: {MapUid}).", mapUid);
            return null;
        }

        var arenaPlayer = ghost.RecordData.EntList.FirstOrDefault(x => x.Type == arenaPlayerIndex);

        if (arenaPlayer is null)
        {
            _logger.LogWarning("No CGameArenaPlayer found in the WR ghost (EntList not found), hoping that it's UOTD, fingers crossed (MapUid: {MapUid}).", mapUid);
            return null;
        }

        var currentCar = defaultCar;
        var tempTimestamp = TimeInt32.Zero;
        var carLengths = new Dictionary<string, TimeInt32>();

        foreach (var delta in arenaPlayer.Samples2.Where(x => x.Type == 81))
        {
            var eventType = delta.Data[4];

            var carSwitch = eventType switch
            {
                20 => "CarSport",
                21 => "CarSnow",
                22 => "CarRally",
                23 => "CarDesert",
                _ => null
            };

            if (carSwitch is null || carSwitch == currentCar)
            {
                continue;
            }

            var carLength = delta.Time - tempTimestamp;

            if (!carLengths.TryAdd(currentCar, carLength))
            {
                carLengths[currentCar] += carLength;
            }

            currentCar = carSwitch;
            tempTimestamp = delta.Time;
        }

        // no car change and default car is not stadium
        if (carLengths.Count == 0 && defaultCar != "CarSport")
        {
            return null;
        }

        carLengths.TryAdd(currentCar, ghost.RaceTime ?? ghost.RecordData.End - tempTimestamp);

        _ = carLengths.TryGetValue("CarSport", out var carSportLength);
        var nonCarSportLength = carLengths.Where(x => x.Key != "CarSport")
            .Sum(x => x.Value.TotalMilliseconds);

        foreach (var (car, time) in carLengths)
        {
            _logger.LogInformation("Car {Car} was played for {Time}.", car, time);
        }

        if (carSportLength.TotalMilliseconds == 0)
        {
            return 1;
        }

        return nonCarSportLength / (float)(carSportLength.TotalMilliseconds + nonCarSportLength);
    }
}
