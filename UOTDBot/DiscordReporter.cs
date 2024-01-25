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
                    await _bot.SendMessageAsync(val, TextFormatter.Deformat(map.Name));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send UOTD report to channel {ChannelId}.", val);
                }
            }
        }
    }
}
