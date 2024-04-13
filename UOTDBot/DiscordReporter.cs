using ByteSizeLib;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

internal sealed class DiscordReporter
{
    private readonly IDiscordBot _bot;
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordReporter> _logger;
    private readonly Version _version;

    public DiscordReporter(
        IDiscordBot bot,
        AppDbContext db,
        TimeProvider timeProvider,
        IConfiguration config,
        ILogger<DiscordReporter> logger,
        Version version)
    {
        _bot = bot;
        _db = db;
        _timeProvider = timeProvider;
        _config = config;
        _logger = logger;
        _version = version;
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

            var msg = await ReportInChannelAsync(map, channelId, autoThread: false, new());

            if (msg is not null)
            {
                reportCount++;
            }
        }

        var enabledReportChannels = await _db.ReportChannels
            .Include(x => x.Configuration)
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var reportChannel in enabledReportChannels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await ReportInChannelAsync(map, reportChannel, reportChannel.Configuration);

            if (message is not null)
            {
                reportCount++;
            }
        }

        var dmCount = 0;

        var enabledReportDms = await _db.ReportUsers
            .Include(x => x.Configuration)
            .Where(x => x.IsEnabled)
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

    private async Task<ReportMessage?> ReportInChannelAsync(Map map, ReportChannel channel, ReportConfiguration config)
    {
        var message = await ReportInChannelAsync(map, channel.ChannelId, channel.AutoThread, config);

        if (message is null)
        {
            return null;
        }

        var createdAt = _timeProvider.GetUtcNow();

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

    private async Task<IUserMessage?> ReportInChannelAsync(Map map, ulong channelId, bool autoThread, ReportConfiguration config)
    {
        _logger.LogInformation("Sending UOTD report to channel {ChannelId}...", channelId);

        if (map.Features.NonStadiumDistribution < config.Threshold)
        {
            _logger.LogInformation("Skipping report to channel {ChannelId} due to server's threshold not meeting map's car distribution.", channelId);
            return null;
        }

        IUserMessage? message;

        try
        {
            var embed = CreateEmbed(map, config);

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
                await _bot.CreateThreadAsync(channelId, message, $"{TextFormatter.Deformat(map.Name)} by {map.AuthorName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create thread for report in channel {ChannelId}.", channelId);
            }
        }

        return message;
    }

    private async Task<ReportMessage?> ReportInDMAsync(Map map, ReportUser reportUser)
    {
        var userId = reportUser.UserId;

        _logger.LogInformation("Sending UOTD report to DM {UserId}...", userId);

        if (map.Features.NonStadiumDistribution < reportUser.Configuration.Threshold)
        {
            _logger.LogInformation("Skipping report DM for user {UserId} due to user's threshold not meeting map's car distribution.", userId);
            return null;
        }

        var user = await _bot.GetUserAsync(userId);
        
        if (user is null)
        {
            _logger.LogWarning("User not found for report DM (UserId: {UserId}).", userId);
            return null;
        }

        IUserMessage? message;

        try
        {
            var embed = CreateEmbed(map, reportUser.Configuration);

            message = await user.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send UOTD report to user {UserId}.", userId);
            return null;
        }

        var createdAt = _timeProvider.GetUtcNow();

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

    private Embed CreateEmbed(Map map, ReportConfiguration config)
    {
        var sbFeatures = new StringBuilder();

        if (map.Features.DefaultCar != "CarSport")
        {
            var defaultCarModel = _db.Cars.Find(map.Features.DefaultCar);

            if (config.Emotes.TryGetValue(map.Features.DefaultCar, out var emote) && !string.IsNullOrWhiteSpace(emote))
            {
                sbFeatures.Append(emote);
                sbFeatures.Append(' ');
            }

            sbFeatures.Append("**");
            sbFeatures.Append(defaultCarModel?.GetName(config) ?? map.Features.DefaultCar);
            sbFeatures.AppendLine("** (default car)");
        }

        if (map.Features.Gates.Count > 0)
        {
            sbFeatures.AppendLine("Transformation:");

            foreach (var gateCar in map.Features.Gates)
            {
                var gateCarModel = _db.Cars.Find(gateCar);

                if (config.Emotes.TryGetValue(gateCar, out var emote) && !string.IsNullOrWhiteSpace(emote))
                {
                    sbFeatures.Append(emote);
                    sbFeatures.Append(' ');
                }
                else
                {
                    sbFeatures.Append("- ");
                }

                sbFeatures.Append(gateCarModel?.GetName(config) ?? gateCar);

                if (map.Features.CarDistribution?.TryGetValue(gateCar, out var carDistribution) == true)
                {
                    sbFeatures.Append(" (~");
                    sbFeatures.Append(GetLengthString(carDistribution.TimeMilliseconds));
                    sbFeatures.Append(" | ");
                    sbFeatures.Append(carDistribution.Percentage.ToString("P2"));
                    sbFeatures.Append(')');
                }

                sbFeatures.AppendLine();
            }

            if (map.Features.NonStadiumDistribution.HasValue)
            {
                var carSportModel = _db.Cars.Find("CarSport");
                var carSport = carSportModel?.GetName(config) ?? "CarSport";
                sbFeatures.AppendLine($"**{1 - map.Features.NonStadiumDistribution:P2} {carSport} map!**");
            }
            else
            {
                sbFeatures.AppendLine("*Unknown car distribution.*");
            }
        }
        else
        {
            sbFeatures.AppendLine("No transformation");
        }

        var fields = new List<EmbedFieldBuilder>
        {
            new() { Name = "Map", Value = $"[{TextFormatter.Deformat(map.Name)}](https://trackmania.io/#/leaderboard/{map.MapUid})", IsInline = true },
            new() { Name = "Length", Value = $"~{GetLengthString(map.AuthorTime.TotalMilliseconds + 1000)}", IsInline = true },
            new() { Name = "Features", Value = sbFeatures.ToString(), IsInline = true }
        };

        if (!string.IsNullOrEmpty(map.AuthorName))
        {
            fields.Add(new EmbedFieldBuilder { Name = "Author", Value = $"[{map.AuthorName}](https://trackmania.io/#/player/{map.AuthorGuid})", IsInline = true });
        }

        fields.Add(new EmbedFieldBuilder { Name = "Size", Value = ByteSize.FromBytes(map.FileSize), IsInline = true });
        fields.Add(new EmbedFieldBuilder { Name = "Updated", Value = TimestampTag.FromDateTimeOffset(map.UpdatedAt, TimestampTagStyles.Relative), IsInline = true });

        return new EmbedBuilder()
            .WithTitle("New United TOTD!")
            .WithFields(fields)
            .WithThumbnailUrl(map.ThumbnailUrl)
            .WithCurrentTimestamp()
            .WithFooter($"UOTD {_version.ToString(3)} | TOTD")
            .WithUrl($"https://trackmania.io/#/cotd/{map.CupId}")
            .Build();
    }

    private static string GetLengthString(int milliseconds)
    {
        var minutes = milliseconds / 60000;
        var seconds = milliseconds % 60000 / 1000;

        var lengthString = $"{seconds} sec";
        if (minutes > 0) lengthString = $"{minutes} min, {lengthString}";
        return lengthString;
    }
}
