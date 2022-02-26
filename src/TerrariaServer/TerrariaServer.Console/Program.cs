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

// rabbitmq for out of process messaging
	// see if brighter can be leveraged to help with strongly typed messages https://paramore.readthedocs.io/en/latest/RabbitMQConfiguration.html
// script to start rabbitmq + discord bot
// create systemd
// tests?
// logging? could log each request with structured logging, in a pipelinebehavior