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

// next: implement mediatr pipelinebehavior with try {} catch exception to handle any possible errors
// remove anything to do with filesystem, OS commands etc for now, to allow development without env interference. Still have version control anyway so not a problem
// tests? logging?