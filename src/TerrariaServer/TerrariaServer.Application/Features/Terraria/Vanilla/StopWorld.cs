using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using TerrariaServer.Application.Features.Terraria.Shared;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class StopWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal StopWorldModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("stop")]
	internal async Task StopWorldAsync(string worldName)
	{
		var stopWorldRequest = new StopWorldRequest(worldName, Context.User.Id, Context.Message.Id);
		try
		{
			await ReplyAsync($"Stopping world {worldName}...");
			await _mediator.Send(stopWorldRequest);
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

internal record StopWorldRequest(string WorldName, ulong HostUserId, ulong MessageId) : IRequest;

internal class StopWorldHandler : IRequestHandler<StopWorldRequest>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly WorldService _worldService;

	public StopWorldHandler(WorldService world, IOptions<DiscordConfiguration> discordConfig)
	{
		_worldService = world;
		_discordConfig = discordConfig.Value;
	}

	public async Task<Unit> Handle(StopWorldRequest request, CancellationToken cancellationToken)
	{
		if (!_worldService.IsWorldStarted(request.WorldName))
			throw new WorldIsNotStartedException();

		var world = _worldService.GetWorld(request.WorldName);
		if (world.User != request.HostUserId && request.HostUserId != _discordConfig.AdminUser)
			throw new CantStopWorldException();

		await Task.Delay(5 * 1000, cancellationToken); // stop world
		_worldService.MarkWorldAsStopped(world.WorldName);
		return Unit.Value;
	}
}

internal class CantStopWorldException : Exception { }