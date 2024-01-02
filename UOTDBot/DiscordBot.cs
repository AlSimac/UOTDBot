using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UOTDBot;
public interface IDiscordBot : IAsyncDisposable, IDisposable
{
    Task StartAsync();
    Task StopAsync();
}

internal sealed class DiscordBot : IDiscordBot
{
    private readonly IServiceProvider _provider;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordBot(IServiceProvider provider,
        DiscordSocketClient client,
        InteractionService interactionService,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DiscordSocketClient> logger)
    {
        _provider = provider;
        _env = env;
        _client = client;
        _interactionService = interactionService;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        var token = _config["Token"] ?? throw new Exception("Token was not provided.");

        _logger.LogInformation("Starting bot...");
        _logger.LogInformation("Preparing modules...");

        await _interactionService.AddModulesAsync(typeof(Startup).Assembly, _provider);

        _logger.LogInformation("Subscribing to events...");

        _client.Log += ClientLog;
        _client.InviteCreated += _ => Task.CompletedTask;
        _client.GuildScheduledEventCreated += _ => Task.CompletedTask;
        _client.Ready += ClientReady;
        _client.InteractionCreated += async interaction =>
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _provider);
        };

        _logger.LogInformation("Loggin in...");

        await _client.LoginAsync(TokenType.Bot, _config["Token"]);

        _logger.LogInformation("Starting...");

        await _client.StartAsync();

        _logger.LogInformation("Started!");
    }

    private async Task ClientReady()
    {
        // Does not need to be called every Ready event
        await RegisterCommandsAsync(deleteMissing: true);
    }

    private Task ClientLog(LogMessage msg)
    {
        _logger.Log(msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => throw new NotImplementedException()
        },
            msg.Exception, "{message}", msg.Message);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    private async Task RegisterCommandsAsync(bool deleteMissing = true)
    {
        _logger.LogInformation("Registering commands...");

        if (_env.IsDevelopment())
        {
            await _interactionService.RegisterCommandsToGuildAsync(ulong.Parse(_config["TestGuildId"] ?? throw new Exception("TestGuildId was not provided.")), deleteMissing);
        }
        else
        {
            await _interactionService.RegisterCommandsGloballyAsync(deleteMissing);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
