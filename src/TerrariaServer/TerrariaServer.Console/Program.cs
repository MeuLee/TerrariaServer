using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TerrariaServer.Application.Extensions;
using TerrariaServer.Console.Extensions;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices((ctx, services) =>
{
	services
		.AddConfiguration(ctx.Configuration)
		.AddDiscord()
		.AddApplication();
});
hostBuilder.ConfigureAppConfiguration(builder =>
{
	builder.AddJsonFile("appsettings.override.json", optional: true);
});

using var host = hostBuilder.Build();
await host.RunAsync();
