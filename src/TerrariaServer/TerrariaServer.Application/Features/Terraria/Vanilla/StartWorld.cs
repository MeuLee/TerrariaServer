using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TerrariaServer.Application.Features.Terraria.Shared;
using TerrariaServer.Features.Terraria.Shared;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class StartWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal StartWorldModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("start")]
	internal async Task StartWorldAsync(string worldName, string password)
	{
		var request = new StartWorldRequest(Context.User.Id, worldName, password, Context.Message.Id);
		try
		{
			await ReplyAsync($"Starting world {worldName}.");
			await _mediator.Send(request);
			await ReplyAsync($"World {worldName} is up and running.");
		}
		catch (InvalidPasswordException)
		{
			await ReplyAsync("The password should be composed of numbers and letters only.");
		}
		catch (WorldDoesNotExistException)
		{
			await ReplyAsync($"Could not find world {worldName}.");
		}
		catch (WorldIsAlreadyStartedException)
		{
			await ReplyAsync($"World {worldName} is already started.");
		}
		catch (WorldStartTimeoutException)
		{
			await ReplyAsync($"World {worldName} failed to start within the alloted timeout of {StartWorldConstants.WorldStartTimeout.TotalSeconds} seconds.");
		}
	}
}

internal record StartWorldRequest(ulong HostUserId, string WorldName, string Password, ulong MessageId) : IRequest<Unit>;
internal static class StartWorldConstants
{
	public static readonly TimeSpan WorldStartTimeout = TimeSpan.FromMinutes(1);
}

internal class StartWorldHandler : IRequestHandler<StartWorldRequest>
{
	private const string PasswordRegex = "^[\\d\\w]+$";

	private readonly VanillaConfiguration _vanillaConfig;
	private readonly WorldService _worldService;

	public StartWorldHandler(WorldService worldService, IOptions<VanillaConfiguration> vanillaConfig)
		=> (_vanillaConfig, _worldService) = (vanillaConfig.Value, worldService);

	public async Task<Unit> Handle(StartWorldRequest request, CancellationToken cancellationToken)
	{
		if (_worldService.IsWorldStarted(request.WorldName))
			throw new WorldIsAlreadyStartedException();

		if (!Regex.IsMatch(request.Password, PasswordRegex))
			throw new InvalidPasswordException();

		var worldFilePath = Path.Combine(_vanillaConfig.WorldsFolderPath, $"{request.WorldName.Replace(' ', '_')}.wld");
		if (!File.Exists(worldFilePath))
			throw new WorldDoesNotExistException();

		try
		{
			await Task.Delay(5 * 1000, cancellationToken) // start the world
				.WaitAsync(StartWorldConstants.WorldStartTimeout, cancellationToken);
		}
		catch (TimeoutException)
		{
			throw new WorldStartTimeoutException();
		}
		_worldService.MarkWorldAsStarted(new WorldStartInfo(request.HostUserId, request.WorldName, request.Password));

		return Unit.Value;
	}

	// todo move to its own feature
	
}

internal class InvalidPasswordException : Exception { }
internal class WorldDoesNotExistException : Exception { }
internal class WorldStartTimeoutException : Exception { }
