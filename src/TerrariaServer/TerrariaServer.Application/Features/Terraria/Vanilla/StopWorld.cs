using Discord.Commands;
using Microsoft.Extensions.Options;
using Paramore.Brighter;
using TerrariaServer.Application.Features.Terraria.Shared;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class StopWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IAmACommandProcessor _commandProcessor;

	internal StopWorldModule(IAmACommandProcessor commandProcessor)
		=> _commandProcessor = commandProcessor;

	[Command("stop")]
	internal async Task StopWorldAsync(string worldName)
	{
		var stopWorldRequest = new StopWorldRequest(worldName, Context.User.Id);
		try
		{
			await ReplyAsync($"Stopping world {worldName}...");
			await _commandProcessor.SendAsync(stopWorldRequest);
			await ReplyAsync($"Stopped world {worldName} successfully.");
		}
		catch (CantStopWorldException)
		{
			await ReplyAsync("The world can only be stopped by the person who started it, or by an admin.");
		}
		catch (WorldIsNotStartedException)
		{
			await ReplyAsync("The world is not started.");
		}
	}
}

public record StopWorldRequest(string WorldName, ulong HostUserId) : IRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
}

public class StopWorldHandler : RequestHandlerAsync<StopWorldRequest>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly WorldService _worldService;

	public StopWorldHandler(WorldService world, IOptions<DiscordConfiguration> discordConfig)
	{
		_worldService = world;
		_discordConfig = discordConfig.Value;
	}

	public override async Task<StopWorldRequest> HandleAsync(StopWorldRequest command, CancellationToken cancellationToken)
	{
		if (!_worldService.IsWorldStarted(command.WorldName))
			throw new WorldIsNotStartedException();

		var world = _worldService.GetWorld(command.WorldName);
		if (world.User != command.HostUserId && command.HostUserId != _discordConfig.AdminUser)
			throw new CantStopWorldException();

		await Task.Delay(5 * 1000, cancellationToken); // stop world
		_worldService.MarkWorldAsStopped(world.WorldName);

		return await base.HandleAsync(command, cancellationToken);
	}
}

internal class CantStopWorldException : Exception { }