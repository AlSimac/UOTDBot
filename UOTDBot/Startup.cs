using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UOTDBot.Extensions;

namespace UOTDBot;

internal sealed class Startup : IHostedService
{
    private readonly IServiceProvider _provider;
    private readonly IDiscordBot _bot;
    private readonly NadeoLiveServices _nls;
    private readonly NadeoClubServices _ncs;
    private readonly IConfiguration _config;
    private readonly ILogger<Startup> _logger;

    public Startup(
        IServiceProvider provider,
        IDiscordBot bot,
        NadeoLiveServices nls,
        NadeoClubServices ncs,
        IConfiguration config,
        ILogger<Startup> logger)
    {
        _provider = provider;
        _bot = bot;
        _nls = nls;
        _ncs = ncs;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing database...");

        using (var scope = _provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;
            if (db.IsRelational()) db.Migrate();
        }

        _logger.LogInformation("Starting bot and authorizing with NadeoLiveServices + NadeoClubServices...");

        var dedicatedServerLogin = _config.GetRequiredValue("DedicatedServer:Login");
        var dedicatedServerPassword = _config.GetRequiredValue("DedicatedServer:Password");

        await Task.WhenAll(
            _bot.StartAsync(),
            _nls.AuthorizeAsync(
                dedicatedServerLogin,
                dedicatedServerPassword,
                AuthorizationMethod.DedicatedServer,
                cancellationToken),
            _ncs.AuthorizeAsync(
                dedicatedServerLogin,
                dedicatedServerPassword,
                AuthorizationMethod.DedicatedServer,
                cancellationToken)
            );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.StopAsync();
    }
}