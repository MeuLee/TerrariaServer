using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TerrariaServer.Application;

namespace TerrariaServer.Console.Extensions;

internal static class ServiceCollectionExtensions
{
	internal static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<DiscordConfiguration>()
			.Configure(options => configuration.GetSection("discord").Bind(options));
		services.AddOptions<VanillaConfiguration>()
			.Configure(options => configuration.GetSection("vanilla").Bind(options));
		services.AddOptions<ModdedConfiguration>()
			.Configure(options => configuration.GetSection("modded").Bind(options));
		return services;
	}
}