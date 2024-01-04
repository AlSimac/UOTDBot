using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ManiaAPI.NadeoAPI;

namespace UOTDBot;
internal static class App
{
    public static void Services(IServiceCollection services)
    {
        // Configure Discord bot
        services.AddSingleton(new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Verbose
        });

        // Add Discord bot client and Interaction Framework
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<InteractionService>(provider => new(provider.GetRequiredService<DiscordSocketClient>(), new()
        {
            LogLevel = LogSeverity.Verbose
        }));

        // Add Serilog
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        // Add startup
        services.AddHostedService<Startup>();
        services.AddHostedService<Scheduler>();

        // Add services
        services.AddSingleton<IDiscordBot, DiscordBot>();

        // 01/01/2024 Add ManiaAPI.NadeoAPI
        services.AddSingleton<NadeoServices>();
    }

    public static IHostBuilder UseApp(this IHostBuilder app)
    {
        // Use Serilog
        app.UseSerilog();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return app;
    }
}