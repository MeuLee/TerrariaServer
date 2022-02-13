using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TerrariaServer.Application.Mediator;

namespace TerrariaServer.Application.Features.Vanilla;

public class StopWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal StopWorldModule(IMediator mediator)
	{
		_mediator = mediator;
	}

	[Command("stop")]
	internal async Task StopWorldAsync()
	{
		try
		{
			var stopWorldRequest = new StopWorldRequest(Context.User.Id);
			var worldName = await _mediator.SendAsync(stopWorldRequest, Context.Channel);
			await Context.Channel.SendMessageAsync($"Stopping world {worldName}.");
		}
		catch (WorldIsNotStartedException)
		{
			await Context.Channel.SendMessageAsync("The world is not started.");
		}
		catch (DidNotStartWorldException)
		{
			await Context.Channel.SendMessageAsync("The world can only be stopped by the person who started it, or by an admin.");
		}
		catch (Exception ex)
		{
			await Context.Channel.SendMessageAsync($"Encountered unknown error while trying to stop the world.\nError message: {ex.Message}");
		}
	}
}

internal record StopWorldRequest(ulong HostUserId) : IRequest<string>;

internal class StopWorldHandler : IAsyncRequestHandler<StopWorldRequest, string>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly World _world;

	public StopWorldHandler(World world, IOptions<DiscordConfiguration> discordConfig)
	{
		_world = world;
		_discordConfig = discordConfig.Value;
	}

	public async Task<string> HandleAsync(StopWorldRequest request, ISocketMessageChannel channel, CancellationToken cancellationToken)
	{
		if (_world.WorldStartInfo is null)
			throw new WorldIsNotStartedException();
		if (_world.WorldStartInfo.User != request.HostUserId && request.HostUserId != _discordConfig.AdminUser)
			throw new DidNotStartWorldException();
		await SendCommandToProcessAsync(_world.WorldStartInfo.Process, "exit");
		var worldName = _world.WorldStartInfo.WorldName;
		_world.WorldStartInfo = null;
		return worldName;
	}

	private static async Task SendCommandToProcessAsync(Process process, string command)
	{
		if (!process.StartInfo.RedirectStandardInput)
			throw new ArgumentException("The standard input must be redirected to allow sending input to the process", nameof(process));
		await process.StandardInput.WriteLineAsync(command);
		var exited = process.WaitForExit(1000 * 15);
		if (!exited)
			throw new Exception("Process did not exit within the alloted timeout.");
	}
}

internal class DidNotStartWorldException : Exception { }
internal class WorldIsNotStartedException : Exception { }