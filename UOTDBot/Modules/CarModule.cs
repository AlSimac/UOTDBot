using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using UOTDBot.Models;

namespace UOTDBot.Modules;

[Group("car", "Features related to cars.")]
public sealed class CarModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;

    public CarModule(AppDbContext db)
    {
        _db = db;
    }

    [SlashCommand("format", "Get or set the car name format for reports.")]
    public async Task Format([Choice("SnowCar, RallyCar, ...", "standard"), Choice("CarSnow, CarRally, ...", "official")] string format)
    {
        var config = default(ReportConfiguration);

        if (Context.User is IGuildUser guildUser)
        {
            if (!guildUser.GuildPermissions.ManageChannels)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("You need the `Manage Channels` permission to use this command.").Build(),
                        ephemeral: true);
                return;
            }

            var reportChannel = await _db.ReportChannels
                .Include(x => x.Configuration)
                .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id);

            if (reportChannel is null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("This server is not subscribed to UOTD reports.").Build(),
                        ephemeral: true);
                return;
            }

            config = reportChannel.Configuration;
        }
        else if (Context.Channel is IDMChannel)
        {
            var reportUser = await _db.ReportUsers
                .Include(x => x.Configuration)
                .FirstOrDefaultAsync(c => c.UserId == Context.User.Id);

            if (reportUser is null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("You are not subscribed to UOTD reports.").Build(),
                        ephemeral: true);
                return;
            }

            config = reportUser.Configuration;
        }

        if (config is null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("Not subscribed to UOTD reports.").Build(),
                    ephemeral: true);
            return;
        }

        config.Format = format;

        await _db.SaveChangesAsync();

        await RespondAsync(embed: new EmbedBuilder()
            .WithDescription($"Car name format has been set to `{format}`.").Build(),
                ephemeral: true);
    }
}