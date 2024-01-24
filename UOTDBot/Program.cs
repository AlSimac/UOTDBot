﻿using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using UOTDBot;
using GBX.NET.LZO;
using Microsoft.EntityFrameworkCore;

GBX.NET.Lzo.SetLzo(typeof(MiniLZO));

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHttpClient();

    services.AddSingleton(TimeProvider.System);

    services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite();
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
    services.AddSingleton<TotdChecker>();

    // 01/01/2024 Add ManiaAPI.NadeoAPI
    services.AddSingleton<NadeoLiveServices>(
        provider => new(provider.GetRequiredService<HttpClient>()));
});

// Use Serilog
builder.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

await builder.Build().RunAsync();