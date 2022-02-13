using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TerrariaServer.Application.Features.Vanilla;
using TerrariaServer.Application.Mediator;

namespace TerrariaServer.Application.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
		=> services.AddSingleton<World>()
			.AddMediator(Assembly.GetExecutingAssembly())
			.AddHostedService<TerrariaServerWorker>();

	public static IServiceCollection AddDiscord(this IServiceCollection services)
		=> services.AddDiscordSocketClient()
			.AddCommandService();

	private static IServiceCollection AddDiscordSocketClient(this IServiceCollection services)
		=> services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig { MessageCacheSize = 50, GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages }));

	private static IServiceCollection AddCommandService(this IServiceCollection services)
		=> services.AddSingleton(sp =>
		{
			var commandService = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = true, DefaultRunMode = RunMode.Async });
			commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), sp).GetAwaiter().GetResult();
			return commandService;
		});
}
