using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UOTDBot;

public interface IDiscordBot : IAsyncDisposable, IDisposable
{
    Task StartAsync();
    Task StopAsync();

    Task<IUserMessage?> SendMessageAsync(ulong channelId, string? message = null, Embed? embed = null);
    Task<IThreadChannel?> CreateThreadAsync(ulong channelId, IMessage message, string name);
    Task<IUser?> GetUserAsync(ulong userId);
    IRole? GetRole(ulong channelId, ulong roleId);
}

internal sealed class DiscordBot : IDiscordBot
{
    private readonly IServiceProvider _provider;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DiscordSocketClient> _logger;
    private readonly Version _version;

    public DiscordBot(
        IServiceProvider provider,
        DiscordSocketClient client,
        InteractionService interactionService,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DiscordSocketClient> logger,
        Version version)
    {
        _provider = provider;
        _env = env;
        _client = client;
        _interactionService = interactionService;
        _config = config;
        _logger = logger;
        _version = version;
    }

    public async Task StartAsync()
    {
        var token = _config["Discord:Token"] ?? throw new Exception("Token was not provided.");

        _logger.LogInformation("Starting bot...");
        _logger.LogInformation("Preparing modules...");

        _interactionService.Log += ClientLog;

        using var scope = _provider.CreateScope();
        await _interactionService.AddModulesAsync(typeof(Startup).Assembly, scope.ServiceProvider);

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

        await _client.LoginAsync(TokenType.Bot, token);

        _logger.LogInformation("Starting...");

        await _client.StartAsync();

        _logger.LogInformation("Started!");
    }

    public async Task StopAsync()
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    public async Task<IUserMessage?> SendMessageAsync(ulong channelId, string? message = null, Embed? embed = null)
    {
        var channel = await _client.GetChannelAsync(channelId);

        if (channel is not IMessageChannel msgChannel)
        {
            return null;
        }

        return await msgChannel.SendMessageAsync(message, embed: embed, allowedMentions: new AllowedMentions(AllowedMentionTypes.Roles));
    }

    public async Task<IThreadChannel?> CreateThreadAsync(ulong channelId, IMessage message, string name)
    {
        var channel = await _client.GetChannelAsync(channelId);

        if (channel is not ITextChannel textChannel)
        {
            return null;
        }

        return await textChannel.CreateThreadAsync(name, message: message,
            type: channel is INewsChannel ? ThreadType.NewsThread : ThreadType.PublicThread);
    }

    public async Task<IUser?> GetUserAsync(ulong userId)
    {
        return await _client.GetUserAsync(userId);
    }

    public IRole? GetRole(ulong channelId, ulong roleId)
    {
        var channel = _client.GetChannel(channelId);

        if (channel is IGuildChannel guildChannel)
        {
            return guildChannel.Guild.GetRole(roleId);
        }

        return null;
    }

    private async Task ClientReady()
    {
        var versionStr = _version.ToString(3);

        if (_version.Major == 0)
        {
            versionStr += " (beta)";
        }

        versionStr += " (I can fail sometimes)";

        await _client.SetCustomStatusAsync(versionStr);

        // Does not need to be called every Ready event
        await RegisterCommandsAsync(deleteMissing: true);

        await RemoveInvalidReportsAsync();
    }

    private async Task RemoveInvalidReportsAsync()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invalidReportMessages = await db.ReportMessages
            .Include(x => x.Channel)
            .Include(x => x.Map)
            .Where(x => !x.IsDeleted && x.Map.MapId == Guid.Parse("55ed0a23-3796-4625-8384-8ef6297fc015"))
            .ToListAsync();

        foreach (var msg in invalidReportMessages)
        {
            try
            {
                if (msg.Channel is not null)
                {
                    var c = await _client.GetChannelAsync(msg.Channel.ChannelId) as ITextChannel;

                    if (c is not null)
                    {
                        await c.DeleteMessageAsync(msg.MessageId);
                    }
                }

                if (msg.DM is not null)
                {
                    var u = await _client.GetUserAsync(msg.DM.UserId);

                    if (u is not null)
                    {
                        var c = await u.CreateDMChannelAsync();

                        if (c is not null)
                        {
                            await c.DeleteMessageAsync(msg.MessageId);
                        }
                    }
                }
            }
            catch
            {
                
            }

            msg.IsDeleted = true;
        }

        await db.SaveChangesAsync();
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
            msg.Exception, "{message}", msg.Message ?? msg.Exception?.Message);

        return Task.CompletedTask;
    }

    private async Task RegisterCommandsAsync(bool deleteMissing = true)
    {
        _logger.LogInformation("Registering commands...");

        if (_env.IsDevelopment())
        {
            await _interactionService.RegisterCommandsToGuildAsync(ulong.Parse(_config["Discord:TestGuildId"] ?? throw new Exception("Discord:TestGuildId was not provided.")), deleteMissing);
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
