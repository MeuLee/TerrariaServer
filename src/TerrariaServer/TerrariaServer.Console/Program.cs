using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TerrariaServer.Application.Extensions;
using TerrariaServer.Console;
using TerrariaServer.Console.Extensions;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices((ctx, services) =>
{
	services
		.AddConfiguration(ctx.Configuration)
		.AddHostedService<TerrariaServerWorker>()
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
// create systemd
// tests?
// logging? could log each request with structured logging, in a pipelinebehavior