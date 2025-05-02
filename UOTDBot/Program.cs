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

using ManiaAPI.TrackmaniaIO;
using ManiaAPI.NadeoAPI.Extensions.Hosting;
using Serilog.Sinks.SystemConsole.Themes;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.MiniLZO();
GBX.NET.Gbx.ZLib = new GBX.NET.ZLib.ZLib();

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHttpClient<TotdChecker>()
        .AddStandardResilienceHandler();

    services.AddHttpClient<TrackmaniaIO>()
        .AddStandardResilienceHandler();
    services.AddTransient(provider =>
        new TrackmaniaIO(provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(TrackmaniaIO)), "UOTDBot by Poutrel and BigBang1112"));

    services.AddNadeoAPI();

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

    // Add startup
    services.AddHostedService<Startup>();
    services.AddHostedService<Scheduler>();

    // Add services
    services.AddSingleton<IDiscordBot, DiscordBot>();
    services.AddScoped<TotdChecker>();
    services.AddScoped<CarChecker>();
    services.AddScoped<DiscordReporter>();
    services.AddScoped<UotdInitializer>();

    services.AddSingleton(provider => typeof(Program).Assembly.GetName().Version ?? new Version());

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            options.Protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.ToLowerInvariant() switch
            {
                "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                "http/protobuf" or null or "" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                _ => throw new NotSupportedException($"OTLP protocol {context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]} is not supported")
            };
            options.Headers = context.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0], x => x[1]) ?? [];
        })
        .CreateLogger();

    services.AddSerilog();

    services.AddOpenTelemetry()
        .WithMetrics(options =>
        {
            options
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter();

            options.AddMeter("System.Net.Http");
        })
        .WithTracing(options =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                options.SetSampler<AlwaysOnSampler>();
            }

            options
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter();
        });
    services.AddMetrics();
});

await builder.Build().RunAsync();