using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TerrariaServer.Application;
using TerrariaServer.Application.Extensions;
using TerrariaServer.Console;

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

// remove logging from brighter
// rabbitmq for out of process messaging
// script to start rabbitmq + discord bot
// create systemd
// tests?
// logging? could log each request with structured logging, in a pipelinebehavior