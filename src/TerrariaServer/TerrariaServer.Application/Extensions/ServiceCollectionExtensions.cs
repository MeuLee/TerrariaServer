using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TerrariaServer.Application.Features.Vanilla;
using TerrariaServer.Application.Shared;

namespace TerrariaServer.Application.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
		=> services.AddSingleton<World>()
			.AddMediatR(Assembly.GetExecutingAssembly())
			.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineExceptionHandler<,>))
			.AddHostedService<TerrariaServerWorker>();

	public static IServiceCollection AddDiscord(this IServiceCollection services)
		=> services.AddDiscordSocketClient()
			.AddCommandService()
			.AddCommandContextWithCache();

	private static IServiceCollection AddDiscordSocketClient(this IServiceCollection services)
		=> services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig { MessageCacheSize = 50, GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages }));

	private static IServiceCollection AddCommandService(this IServiceCollection services)
		=> services.AddSingleton(sp =>
		{
			var commandService = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = true, DefaultRunMode = RunMode.Async });
			commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), sp).GetAwaiter().GetResult();
			return commandService;
		});

	private static IServiceCollection AddCommandContextWithCache(this IServiceCollection services)
		=> services.AddMemoryCache()
			.AddSingleton<SocketCommandContextFactory>()
			.AddSingleton<ISocketCommandContextFactory>(services => services.GetRequiredService<SocketCommandContextFactory>())
			.AddSingleton<ISocketCommandContextProvider>(services => services.GetRequiredService<SocketCommandContextFactory>());
}
