using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TmEssentials;

namespace UOTDBot.Modules;

[Group("map", "UOTD maps.")]
public sealed class MapModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;

    public MapModule(AppDbContext db)
    {
        _db = db;
    }

    [SlashCommand("list", "Get the UOTD map list.")]
    public async Task List()
    {
        var maps = await _db.Maps
            .OrderByDescending(x => x.Totd)
            .Take(10)
            .ToListAsync();

        var sb = new StringBuilder();

        foreach (var map in maps)
        {
            sb.Append($"**{TimestampTag.FormatFromDateTime(map.Totd.ToDateTime(new(19, 0)), TimestampTagStyles.ShortDate)}** - **[{TextFormatter.Deformat(map.Name)}](https://trackmania.io/#/leaderboard/{map.MapUid})** by [{map.AuthorName}](https://trackmania.io/#/player/{map.AuthorGuid})");

            if (map.CupId.HasValue)
            {
                sb.Append($" - 🏆 [#1](https://trackmania.io/#/cotd/{map.CupId})");
            }

            sb.AppendLine();
        }

        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("UOTD maps")
            .WithDescription(sb.ToString())
            .Build(), ephemeral: false);
    }
}