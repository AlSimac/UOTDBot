using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

internal sealed class DiscordReporter
{
    private readonly IDiscordBot _bot;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordReporter> _logger;

    public DiscordReporter(IDiscordBot bot, IConfiguration config, ILogger<DiscordReporter> logger)
    {
        _bot = bot;
        _config = config;
        _logger = logger;
    }

    public async Task ReportAsync(Map map, CancellationToken cancellationToken)
    {
        foreach (var reportChannelSection in _config.GetSection("Discord:ForcedReportChannels").GetChildren())
        {
            if (ulong.TryParse(reportChannelSection.Value, out var val))
            {
                _logger.LogInformation("Sending UOTD report to channel {ChannelId}...", val);

                try
                {
                    var length = map.AuthorTime + 1000;
                    var minutes = length / 60000;
                    var seconds = length % 60000 / 1000;

                    var lengthString = $"{seconds} sec";
                    if (minutes > 0) lengthString = $"{minutes} min, {lengthString}";

                    var embed = new EmbedBuilder()
                        .WithTitle("New United TOTD!")
                        .WithFields(
                            new EmbedFieldBuilder { Name = "Map", Value = $"[{TextFormatter.Deformat(map.Name)}](https://trackmania.io/#/leaderboard/{map.MapUid})", IsInline = true },
                            new EmbedFieldBuilder { Name = "Length", Value = $"~{lengthString}", IsInline = true },
                            new EmbedFieldBuilder { Name = "Features", Value = $"SnowCar\nRallyCar\nDesertCar", IsInline = true })
                        .WithThumbnailUrl(map.ThumbnailUrl)
                        .WithCurrentTimestamp()
                        .Build();

                    await _bot.SendMessageAsync(val, embed: embed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send UOTD report to channel {ChannelId}.", val);
                }
            }
        }
    }
}
