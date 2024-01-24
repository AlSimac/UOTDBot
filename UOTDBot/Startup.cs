using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UOTDBot.Extensions;

namespace UOTDBot;

internal sealed class Startup : IHostedService
{
    private readonly IDiscordBot _bot;
    private readonly NadeoLiveServices _nls;
    private readonly IConfiguration _config;
    private readonly ILogger<Startup> _logger;

    public Startup(IDiscordBot bot, NadeoLiveServices nls, IConfiguration config, ILogger<Startup> logger)
    {
        _bot = bot;
        _nls = nls;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot and authorizing with NadeoLiveServices...");

        await Task.WhenAll(
            _bot.StartAsync(),
            _nls.AuthorizeAsync(
                _config.GetRequiredValue("DedicatedServer:Login"),
                _config.GetRequiredValue("DedicatedServer:Password"),
                AuthorizationMethod.DedicatedServer,
                cancellationToken)
            );

        // ... further startup logic here ...
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.StopAsync();
    }
}