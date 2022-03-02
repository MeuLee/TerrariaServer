using Discord.Commands;
using Microsoft.Extensions.Options;
using Paramore.Brighter;
using System.Text.RegularExpressions;
using TerrariaServer.Application.Features.Terraria.Shared;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class StartWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IAmACommandProcessor _commandProcessor;

	internal StartWorldModule(IAmACommandProcessor commandProcessor)
		=> _commandProcessor = commandProcessor;

	[Command("start")]
	internal async Task StartWorldAsync(string worldName, string password)
	{
		var request = new StartWorldRequest(Context.User.Id, worldName, password);
		try
		{
			await ReplyAsync($"Starting world {worldName}.");
			await _commandProcessor.SendAsync(request);
			await ReplyAsync($"World {worldName} is up and running."); // not really since the world is started asynchronously
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

public record StartWorldRequest(ulong HostUserId, string WorldName, string Password) : IRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
}

internal static class StartWorldConstants
{
	public static readonly TimeSpan WorldStartTimeout = TimeSpan.FromMinutes(1);
}

public record WorldStartedMessage(string WorldName, string Password) : IRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
}

public class StartWorldHandler : RequestHandlerAsync<StartWorldRequest>
{
	private const string PasswordRegex = @"^[\d\w]+$";

	private readonly WorldService _worldService;
	private readonly VanillaConfiguration _vanillaConfig;

	public StartWorldHandler(WorldService worldService, IOptions<VanillaConfiguration> vanillaConfig)
		=> (_worldService, _vanillaConfig) = (worldService, vanillaConfig.Value);

	public override async Task<StartWorldRequest> HandleAsync(StartWorldRequest command, CancellationToken cancellationToken)
	{
		if (_worldService.IsWorldStarted(command.WorldName))
			throw new WorldIsAlreadyStartedException();

		if (!Regex.IsMatch(command.Password, PasswordRegex))
			throw new InvalidPasswordException();

		var worldFilePath = Path.Combine(_vanillaConfig.WorldsFolderPath, $"{command.WorldName.Replace(' ', '_')}.wld");
		if (!File.Exists(worldFilePath))
			throw new WorldDoesNotExistException();

		try
		{
			// create the systemd service and start it
			// wait until the world is started, or StartWorldCalled handler would say that the world started
		}
		catch (TimeoutException)
		{
			throw new WorldStartTimeoutException();
		}
		_worldService.MarkWorldAsStarted(new WorldStartInfo(command.HostUserId, command.WorldName, command.Password));

		return await base.HandleAsync(command, cancellationToken);
	}
}

internal class InvalidPasswordException : Exception { }
internal class WorldDoesNotExistException : Exception { }
internal class WorldStartTimeoutException : Exception { }
