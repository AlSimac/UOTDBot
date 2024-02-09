using ByteSizeLib;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

internal sealed class DiscordReporter
{
    private readonly IDiscordBot _bot;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordReporter> _logger;

    public DiscordReporter(IDiscordBot bot, AppDbContext db, IConfiguration config, ILogger<DiscordReporter> logger)
    {
        _bot = bot;
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task ReportInChannelsAsync(Map map, CancellationToken cancellationToken)
    {
        var reportCount = 0;

        foreach (var reportChannelSection in _config.GetSection("Discord:ForcedReportChannels").GetChildren())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ulong.TryParse(reportChannelSection.Value, out var channelId))
            {
                _logger.LogWarning("Invalid forced channel ID in configuration: {ChannelId}", reportChannelSection.Value);
                continue;
            }

            var msg = await ReportInChannelAsync(map, channelId, autoThread: false);

            if (msg is not null)
            {
                reportCount++;
            }
        }

        var enabledReportChannels = await _db.ReportChannels
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var reportChannel in enabledReportChannels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await ReportInChannelAsync(map, reportChannel);

            if (message is not null)
            {
                reportCount++;
            }
        }

        var dmCount = 0;

        var enabledReportDms = await _db.ReportUsers
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var reportUser in enabledReportDms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await ReportInDMAsync(map, reportUser);

            if (message is not null)
            {
                dmCount++;
            }
        }

        _logger.LogInformation("Reported UOTD to {ChannelCount} channels and {DmCount} DMs.", reportCount, dmCount);
    }

    private async Task<ReportMessage?> ReportInChannelAsync(Map map, ReportChannel channel)
    {
        var message = await ReportInChannelAsync(map, channel.ChannelId, channel.AutoThread);

        if (message is null)
        {
            return null;
        }

        var createdAt = DateTimeOffset.UtcNow;

        var reportMessage = new ReportMessage
        {
            MessageId = message.Id,
            OriginalChannelId = channel.ChannelId,
            Map = map,
            Channel = channel,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        await _db.ReportMessages.AddAsync(reportMessage);
        await _db.SaveChangesAsync();

        return reportMessage;
    }

    private async Task<IUserMessage?> ReportInChannelAsync(Map map, ulong channelId, bool autoThread)
    {
        _logger.LogInformation("Sending UOTD report to channel {ChannelId}...", channelId);

        IUserMessage? message;

        try
        {
            var embed = CreateEmbed(map);

            message = await _bot.SendMessageAsync(channelId, embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send UOTD report to channel {ChannelId}.", channelId);
            return null;
        }

        if (autoThread && message is not null)
        {
            try
            {
                await _bot.CreateThreadAsync(channelId, message, "lol");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create thread for report in channel {ChannelId}.", channelId);
            }
        }

        return message;
    }

    private async Task<ReportMessage?> ReportInDMAsync(Map map, ReportUser reportUser)
    {
        var userId = reportUser.UserId;

        _logger.LogInformation("Sending UOTD report to DM {UserId}...", userId);

        var user = await _bot.GetUserAsync(userId);
        
        if (user is null)
        {
            _logger.LogWarning("User not found for report DM (UserId: {UserId}).", userId);
            return null;
        }

        IUserMessage? message;

        try
        {
            var embed = CreateEmbed(map);

            message = await user.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send UOTD report to user {UserId}.", userId);
            return null;
        }

        var createdAt = DateTimeOffset.UtcNow;

        var reportMessage = new ReportMessage
        {
            MessageId = message.Id,
            Map = map,
            DM = reportUser,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        await _db.ReportMessages.AddAsync(reportMessage);
        await _db.SaveChangesAsync();

        return reportMessage;
    }

    private static Embed CreateEmbed(Map map)
    {
        var length = map.AuthorTime.TotalMilliseconds + 1000;
        var minutes = length / 60000;
        var seconds = length % 60000 / 1000;

        var lengthString = $"{seconds} sec";
        if (minutes > 0) lengthString = $"{minutes} min, {lengthString}";

        return new EmbedBuilder()
            .WithTitle("New United TOTD!")
            .WithFields(
                new EmbedFieldBuilder { Name = "Map", Value = $"[{TextFormatter.Deformat(map.Name)}](https://trackmania.io/#/leaderboard/{map.MapUid})", IsInline = true },
                new EmbedFieldBuilder { Name = "Length", Value = $"~{lengthString}", IsInline = true },
                new EmbedFieldBuilder { Name = "Features", Value = "SnowCar", IsInline = true },
                new EmbedFieldBuilder { Name = "Size", Value = ByteSize.FromBytes(map.FileSize), IsInline = true },
                new EmbedFieldBuilder { Name = "Uploaded", Value = TimestampTag.FromDateTimeOffset(map.UploadedAt, TimestampTagStyles.Relative), IsInline = true },
                new EmbedFieldBuilder { Name = "Updated", Value = TimestampTag.FromDateTimeOffset(map.UpdatedAt, TimestampTagStyles.Relative), IsInline = true })
            .WithThumbnailUrl(map.ThumbnailUrl)
            .WithCurrentTimestamp()
            .Build();
    }
}
