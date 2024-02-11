using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot.Modules;

[Group("report", "UOTD report management.")]
public sealed class ReportModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportModule> _logger;
    private readonly Version _version;

    public ReportModule(AppDbContext db, TimeProvider timeProvider, ILogger<ReportModule> logger, Version version)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
        _version = version;
    }

    [SlashCommand("subscribe", "Subscribe to UOTD reports in this or other text channel.")]
    public async Task Subscribe([ChannelTypes(ChannelType.Text)] IChannel? other = default)
    {
        if (!await ValidateManageChannelsPermissionAsync())
        {
            return;
        }

        var channel = other ?? Context.Channel;

        if (channel is not IMessageChannel msgChannel)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Cannot report in {MentionUtils.MentionChannel(channel.Id)} - not a message channel.").Build(),
                    ephemeral: true);
            return;
        }

        var reportChannel = await _db.ReportChannels
            .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id);

        if (reportChannel is null)
        {
            var createdAt = _timeProvider.GetUtcNow();

            reportChannel = new ReportChannel
            {
                ChannelId = channel.Id,
                GuildId = Context.Guild.Id,
                IsEnabled = true,
                AutoThread = false,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Configuration = new()
            };

            await _db.ReportChannels.AddAsync(reportChannel);
        }
        else
        {
            if (reportChannel.ChannelId != channel.Id)
            {
                await SafeSendUnsubscribeMessageAsync(reportChannel.ChannelId);
            }
            else if (reportChannel.IsEnabled)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription($"Already reporting in {MentionUtils.MentionChannel(channel.Id)}.").Build(),
                        ephemeral: true);
                return;
            }

            reportChannel.UpdatedAt = _timeProvider.GetUtcNow();
            reportChannel.ChannelId = channel.Id;
            reportChannel.IsEnabled = true;
        }

        await _db.SaveChangesAsync();

        var initialMsg = default(IUserMessage);

        try
        {
            initialMsg = await msgChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithDescription("**This channel is now subscribed to UOTD reports.**")
                .WithCurrentTimestamp()
                .WithFooter($"UOTD {_version.ToString(3)}")
                .Build());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send an initial report to channel {ChannelId}.", channel.Id);

            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Failed to send an initial report to {MentionUtils.MentionChannel(channel.Id)}: `{ex.Message}`").Build(),
                    ephemeral: true);
        }

        // outside of try catch so that exceptions are better logged
        if (initialMsg is not null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"TOTDs that fit the UOTD rules will be reported in {MentionUtils.MentionChannel(channel.Id)}.\n**Initial report:** {initialMsg.GetJumpUrl()}").Build(),
                    ephemeral: true);
        }
    }

    [SlashCommand("unsubscribe", "Unsubscribe to UOTD reports on this server.")]
    public async Task Unsubscribe()
    {
        if (!await ValidateManageChannelsPermissionAsync())
        {
            return;
        }

        var reportChannel = await _db.ReportChannels
            .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id && c.IsEnabled);

        if (reportChannel is null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("This server is not subscribed to UOTD reports.").Build(),
                    ephemeral: true);
            return;
        }

        reportChannel.IsEnabled = false;
        reportChannel.UpdatedAt = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync();

        await SafeSendUnsubscribeMessageAsync(reportChannel.ChannelId);

        await RespondAsync(embed: new EmbedBuilder()
            .WithDescription("TOTDs that fit the UOTD rules will no longer be reported.").Build(),
                ephemeral: true);
    }

    [SlashCommand("dm", "Subscribe to UOTD reports in DMs.")]
    public async Task Dm()
    {
        var reportUser = await _db.ReportUsers
            .FirstOrDefaultAsync(u => u.UserId == Context.User.Id);

        if (reportUser is null)
        {
            var createdAt = _timeProvider.GetUtcNow();

            reportUser = new ReportUser
            {
                UserId = Context.User.Id,
                IsEnabled = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Configuration = new()
            };

            await _db.ReportUsers.AddAsync(reportUser);
        }
        else
        {
            reportUser.IsEnabled = !reportUser.IsEnabled;
            reportUser.UpdatedAt = _timeProvider.GetUtcNow();
        }

        try
        {
            await Context.User.SendMessageAsync("Hello! You have subscribed to UOTD reports in DMs.\nJust letting you know and testing if I'm not blocked or something (do not block me please, just unsubscribe by typing `/report dm` again).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send a DM to user {UserId}.", Context.User.Id);

            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Failed to send a DM to you: `{ex.Message}`").Build(),
                    ephemeral: true);
            return;
        }

        await _db.SaveChangesAsync();

        await RespondAsync(embed: new EmbedBuilder()
            .WithDescription("You are now subscribed to UOTD reports in DMs.").Build(),
                ephemeral: true);
    }

    [SlashCommand("list", "Gets the list of UOTD reports, including the map information.")]
    public async Task List()
    {
        var reports = await GetReportMessagesAsync();

        if (reports.Count == 0)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("No UOTD reports found.").Build(),
                    ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Last {reports.Count} UOTD reports:");
        sb.AppendLine();

        foreach (var report in reports)
        {
            var jumpLink = $"https://discord.com/channels/{report.Channel!.GuildId}/{report.OriginalChannelId}/{report.MessageId}";
            sb.AppendLine($"{jumpLink} - {TimestampTag.FromDateTimeOffset(report.CreatedAt, TimestampTagStyles.ShortDate)} - {TextFormatter.Deformat(report.Map.Name)}");
        }

        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("UOTD reports")
            .WithDescription(sb.ToString()).Build(),
                ephemeral: true);
    }

    private async Task<List<ReportMessage>> GetReportMessagesAsync(int count = 10)
    {
        if (Context.User is IGuildUser)
        {
            var reportChannel = await _db.ReportChannels
                .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id && c.IsEnabled);

            if (reportChannel is null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("This server is not subscribed to UOTD reports.").Build(),
                        ephemeral: true);
                return [];
            }

            return await _db.ReportMessages
                .Include(r => r.Map)
                .Include(r => r.Channel)
                .Where(r => r.Channel != null && r.Channel.ChannelId == reportChannel.ChannelId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        if (Context.Channel is IDMChannel)
        {
            var reportUser = await _db.ReportUsers
                .FirstOrDefaultAsync(c => c.UserId == Context.User.Id && c.IsEnabled);

            if (reportUser is null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("You are not subscribed to UOTD reports.").Build(),
                        ephemeral: true);
                return [];
            }

            return await _db.ReportMessages
                .Include(r => r.Map)
                .Include(r => r.DM)
                .Where(r => r.DM != null && r.DM.UserId == reportUser.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        return [];
    }

    [SlashCommand("autothread", "Enables or disables creation of threads on UOTD reports.")]
    public async Task Autothread()
    {
        if (!await ValidateManageChannelsPermissionAsync())
        {
            return;
        }

        var reportChannel = await _db.ReportChannels
            .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id && c.IsEnabled);

        if (reportChannel is null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("This server is not subscribed to UOTD reports.").Build(),
                    ephemeral: true);
            return;
        }

        reportChannel.AutoThread = !reportChannel.AutoThread;

        await _db.SaveChangesAsync();

        await RespondAsync(embed: new EmbedBuilder()
            .WithDescription($"Auto-threading has been **{(reportChannel.AutoThread ? "enabled" : "disabled")}**.").Build(),
                ephemeral: true);
    }

    [SlashCommand("test", "Test that the UOTD report would work.")]
    public async Task Test()
    {
        if (!await ValidateManageChannelsPermissionAsync())
        {
            return;
        }

        var reportChannel = await _db.ReportChannels
            .FirstOrDefaultAsync(c => c.GuildId == Context.Guild.Id && c.IsEnabled);

        if (reportChannel is null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("This server is not subscribed to UOTD reports.").Build(),
                    ephemeral: true);
            return;
        }

        var testMsg = default(IUserMessage);

        try
        {
            var channel = Context.Guild.GetTextChannel(reportChannel.ChannelId);

            if (channel is null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("The report channel was not found.").Build(),
                        ephemeral: true);
                return;
            }

            testMsg = await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithDescription("**Test report!** You will get UOTD reported here, woo!")
                .Build());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send a test report to channel {ChannelId}.", reportChannel.ChannelId);

            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Failed to send a test report to {MentionUtils.MentionChannel(reportChannel.ChannelId)}: `{ex.Message}`").Build(),
                    ephemeral: true);
        }

        // outside of try catch so that exceptions are better logged
        if (testMsg is not null)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Test report sent to {MentionUtils.MentionChannel(reportChannel.ChannelId)}.\n**Test report:** {testMsg.GetJumpUrl()}").Build(),
                    ephemeral: true);
        }
    }

    [SlashCommand("emote", "Get or set the emotes for reports.")]
    public async Task Emote([
        Choice("SnowCar", "CarSnow"),
        Choice("RallyCar", "CarRally"),
        Choice("DesertCar", "CarDesert")] string type,
        string? emote = null,
        bool reset = false)
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

        if (reset)
        {
            var isRemoved = config.Emotes.Remove(type);

            config.Emotes = new Dictionary<string, string>(config.Emotes); // ef core leol

            await _db.SaveChangesAsync();

            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription(isRemoved
                ? $"Emote for `{type}` has been reset."
                : $"Emote for `{type}` has already been reset.").Build(),
                    ephemeral: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(emote))
        {
            if (config.Emotes.TryGetValue(type, out var currentEmote) && !string.IsNullOrWhiteSpace(currentEmote))
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription($"Emote for `{type}` is {currentEmote}").Build(),
                        ephemeral: true);
                return;
            }

            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription($"Emote for `{type}` is not set.").Build(),
                    ephemeral: true);

            return;
        }

        string emoteStr;

        if (Discord.Emote.TryParse(emote, out var emoteModel))
        {
            emoteStr = emoteModel.ToString();
        }
        else if (Emoji.TryParse(emote, out var emojiModel))
        {
            emoteStr = emojiModel.ToString();
        }
        else
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("Invalid emote.").Build(),
                    ephemeral: true);
            return;
        }

        config.Emotes = new Dictionary<string, string>(config.Emotes)
        {
            [type] = emoteStr
        };

        await _db.SaveChangesAsync();

        await RespondAsync(embed: new EmbedBuilder()
            .WithDescription($"Emote for `{type}` set to {emoteStr}").Build(),
                ephemeral: true);
    }

    private async Task<bool> ValidateManageChannelsPermissionAsync()
    {
        if (Context.User is not IGuildUser guildUser)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("This command only works on a server.").Build(),
                    ephemeral: true);
            return false;
        }

        if (!guildUser.GuildPermissions.ManageChannels)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithDescription("You need the `Manage Channels` permission to use this command.").Build(),
                    ephemeral: true);
            return false;
        }

        return true;
    }

    private async Task SafeSendUnsubscribeMessageAsync(ulong channelId)
    {
        try
        {
            var channel = Context.Guild.GetTextChannel(channelId);

            if (channel is null)
            {
                _logger.LogWarning("Channel {ChannelId} was not found when unsubscribing.", channelId);
                return;
            }

            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithDescription("This channel is **no longer subscribed** to UOTD reports.")
                .WithCurrentTimestamp()
                .WithFooter($"UOTD {_version.ToString(3)}")
                .Build());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send an unsubscribe message to channel {ChannelId}.", channelId);
        }
    }
}
