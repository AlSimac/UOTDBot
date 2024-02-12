using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
            sb.AppendLine($"**{map.Totd:yyyy-MM-dd}** - {map.Name} by {map.AuthorName}");
        }

        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("UOTD maps")
            .WithDescription(sb.ToString())
            .Build(), ephemeral: true);
    }
}