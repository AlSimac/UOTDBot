using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UOTDBot;

var services = new ServiceCollection();

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => App.Services(services))
    .UseApp()
    .Build()
    .RunAsync();