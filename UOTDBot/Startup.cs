using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UOTDBot;
internal sealed class Startup : IHostedService
{
    private readonly IDiscordBot _bot;
    private readonly ILogger<Startup> _logger;

    public Startup(IDiscordBot bot, ILogger<Startup> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bot.StartAsync();

        // ... further startup logic here ...
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.StopAsync();
    }
}