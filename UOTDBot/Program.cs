using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using UOTDBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Polly;
using Polly.Contrib.WaitAndRetry;
using ManiaAPI.TrackmaniaIO;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.MiniLZO();
GBX.NET.Gbx.ZLib = new GBX.NET.ZLib.ZLib();

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHttpClient<TotdChecker>()
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 5)
        ));

    services.AddHttpClient<NadeoLiveServices>()
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 5)
        ));

    services.AddHttpClient<NadeoClubServices>()
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 5)
        ));

    services.AddHttpClient<TrackmaniaIO>()
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 5)
        ));

    services.AddSingleton(TimeProvider.System);

    services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection"));
    });

    // Configure Discord bot
    services.AddSingleton(new DiscordSocketConfig()
    {
        LogLevel = LogSeverity.Verbose
    });

    // Add Discord bot client and Interaction Framework
    services.AddSingleton<DiscordSocketClient>();
    services.AddSingleton<InteractionService>(provider => new(provider.GetRequiredService<DiscordSocketClient>(), new()
    {
        LogLevel = LogSeverity.Verbose,
        //LocalizationManager = new JsonLocalizationManager("Data", "commands")
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
    services.AddScoped<TotdChecker>();
    services.AddScoped<CarChecker>();
    services.AddScoped<DiscordReporter>();
    services.AddScoped<UotdInitializer>();

    // 01/01/2024 Add ManiaAPI.NadeoAPI
    services.AddSingleton<NadeoLiveServices>(
        provider => new(provider.GetRequiredService<HttpClient>()));
    services.AddSingleton<NadeoClubServices>(
        provider => new(provider.GetRequiredService<HttpClient>()));
    services.AddSingleton<TrackmaniaIO>(
        provider => new(provider.GetRequiredService<HttpClient>(), "UOTDBot by Poutrel and BigBang1112"));

    services.AddSingleton<Version>(provider => typeof(Program).Assembly.GetName().Version ?? new Version());

});

// Use Serilog
builder.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

await builder.Build().RunAsync();