using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace TerrariaServer.Application.Features.Vanilla;

public class StopWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal StopWorldModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("stop")]
	internal async Task StopWorldAsync()
	{
		var stopWorldRequest = new StopWorldRequest(Context.User.Id, Context.Message.Id);
		await _mediator.Send(stopWorldRequest);
	}
}

internal record StopWorldRequest(ulong HostUserId, ulong MessageId) : IRequest;

internal class StopWorldHandler : IRequestHandler<StopWorldRequest>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly World _world;
	private readonly ISocketCommandContextProvider _commandContextProvider;

	public StopWorldHandler(World world, ISocketCommandContextProvider commandContextProvider, IOptions<DiscordConfiguration> discordConfig)
	{
		_world = world;
		_commandContextProvider = commandContextProvider;
		_discordConfig = discordConfig.Value;
	}

	public async Task<Unit> Handle(StopWorldRequest request, CancellationToken cancellationToken)
	{
		var commandContext = _commandContextProvider.ProvideContext(request.MessageId);
		if (_world.WorldStartInfo is null)
		{
			await commandContext.Channel.SendMessageAsync("The world is not started.");
			return Unit.Value;
		}
		if (_world.WorldStartInfo.User != request.HostUserId && request.HostUserId != _discordConfig.AdminUser)
		{
			await commandContext.Channel.SendMessageAsync("The world can only be stopped by the person who started it, or by an admin.");
			return Unit.Value;
		}
		await commandContext.Channel.SendMessageAsync($"Stopping world {_world.WorldStartInfo.WorldName}.");
		await SendCommandToProcessAsync(_world.WorldStartInfo.Process, "exit");
		var worldName = _world.WorldStartInfo.WorldName;
		_world.WorldStartInfo = null;
		await commandContext.Channel.SendMessageAsync($"Stopped world {worldName} successfully.");
		return Unit.Value;
	}

	private static async Task SendCommandToProcessAsync(Process process, string command)
	{
		if (!process.StartInfo.RedirectStandardInput)
			throw new ArgumentException("The standard input must be redirected to allow sending input to the process", nameof(process));
		await process.StandardInput.WriteLineAsync(command);
		var exited = process.WaitForExit(1000 * 15);
		if (!exited)
			throw new TimeoutException("Process did not exit within the alloted timeout.");
	}
}