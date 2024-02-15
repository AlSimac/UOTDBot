using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace UOTDBot.Modules;

public sealed class StatsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;
    private readonly Version _version;

    public StatsModule(AppDbContext db, Version version)
    {
        _db = db;
        _version = version;
    }

    [SlashCommand("stats", "Show stats.")]
    public async Task Stats()
    {
        var totalGuilds = await _db.ReportChannels.CountAsync(x => x.IsEnabled);
        var totalUsers = await _db.ReportUsers.CountAsync(x => x.IsEnabled);

        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("Stats")
            .WithDescription($"Reports in **{totalUsers}** DMs and **{totalGuilds}** servers.")
            .WithFooter($"UOTD {_version.ToString(3)}")
            .Build(),
                ephemeral: true);
    }
}
