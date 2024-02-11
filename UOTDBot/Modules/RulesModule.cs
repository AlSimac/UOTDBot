using Discord;
using Discord.Interactions;

namespace UOTDBot.Modules;

public sealed class RulesModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Version _version;

    public RulesModule(Version version)
    {
        _version = version;
    }

    [SlashCommand("rules", "What are the requirements of TOTD to be reported by UOTD?")]
    public async Task Rules()
    {
        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("Rules")
            .WithDescription("For a TOTD to be considered UOTD, the map contains one of these cars:\n- **SnowCar**\n- **RallyCar**\n- **DesertCar**")
            .AddField("Default car", "If the map has a different default car than StadiumCar (CarSport), the TOTD is considered UOTD. *In technical terms:*\n```\nCGameCtnChallenge.PlayerModel.Id\n  is not null\n  and not empty\n  and not CarSport```")
            .AddField("Transformation gates", "If the map has transformation gates that are different than Stadium, the TOTD is considered UOTD. *In technical terms:*\n```\nfor each env in [Snow, Rally, Desert]:\n  for each block in CGameCtnChallenge.Blocks:\n    block.Name contains Gameplay{env}\n  for each item in CGameCtnChallenge.AnchoredObjects:\n    item.ItemModel.Id contains Gameplay{env}```")
            .WithFooter($"UOTD {_version.ToString(3)}")
            .Build());
    }
}
