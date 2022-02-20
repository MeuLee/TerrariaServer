using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TerrariaServer.Application;

public class VanillaConfiguration
{
	public string TerrariaServerPath { get; init; } = default!;
	public string WorldsFolderPath { get; init; } = default!;
	public int Port { get; init; }
}

public class ModdedConfiguration
{
	public string TerrariaServerPath { get; init; } = default!;
	public string WorldsFolderPath { get; init; } = default!;
	public int Port { get; init; }
}

public class DiscordConfiguration
{
	public ulong AdminUser { get; init; }
	public ulong[] AllowedChannels { get; init; } = default!;
	public string BotToken { get; init; } = default!;
	public string Prefix { get; init; } = default!;
}

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
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