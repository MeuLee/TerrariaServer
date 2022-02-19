using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using TerrariaServer.Application.Shared;
using TerrariaServer.Application.Shared.Services;
using TerrariaServer.Features.Terraria.Shared;

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
		await _mediator.Send(stopWorldRequest);
	}
}

internal record StopWorldRequest(string WorldName, ulong HostUserId, ulong MessageId) : IRequestWithMessageId;

internal class StopWorldHandler : IRequestHandler<StopWorldRequest>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly WorldService _worldService;
	private readonly ICommandContextProvider _commandContextProvider;

	public StopWorldHandler(WorldService world, ICommandContextProvider commandContextProvider, IOptions<DiscordConfiguration> discordConfig)
	{
		_worldService = world;
		_commandContextProvider = commandContextProvider;
		_discordConfig = discordConfig.Value;
	}

	public async Task<Unit> Handle(StopWorldRequest request, CancellationToken cancellationToken)
	{
		var commandContext = _commandContextProvider.ProvideContext(request.MessageId);
		if (!_worldService.IsWorldStarted(request.WorldName))
		{
			await commandContext.Channel.SendMessageAsync("The world is not started.");
			return Unit.Value;
		}
		var world = _worldService.GetWorld(request.WorldName);
		if (world.User != request.HostUserId && request.HostUserId != _discordConfig.AdminUser)
		{
			await commandContext.Channel.SendMessageAsync("The world can only be stopped by the person who started it, or by an admin.");
			return Unit.Value;
		}
		await commandContext.Channel.SendMessageAsync($"Stopping world {world.WorldName}.");
		await Task.Delay(5 * 1000, cancellationToken); // stop world
		_worldService.MarkWorldAsStopped(world.WorldName);
		await commandContext.Channel.SendMessageAsync($"Stopped world {world.WorldName} successfully.");
		return Unit.Value;
	}
}