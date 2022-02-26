using Discord.Commands;
using Microsoft.Extensions.Options;
using Paramore.Brighter;
using System.Text.RegularExpressions;
using TerrariaServer.Application.Features.Terraria.Shared;
using TerrariaServer.Application.Messaging;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class StartWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IAmACommandProcessor _commandProcessor;

	internal StartWorldModule(IAmACommandProcessor commandProcessor)
		=> _commandProcessor = commandProcessor;

	[Command("start")]
	internal async Task StartWorldAsync(string worldName, string password)
	{
		var request = new StartWorldRequest(Context.User.Id, worldName, password, Context.Message.Id);
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

internal record StartWorldRequest(ulong HostUserId, string WorldName, string Password, ulong MessageId) : IRequest
{
	public Guid Id { get; set; }
}

internal record StartWorldMessage(string? Description, SystemdType Type, string ExecStart, bool Enable) : IMessage;
enum SystemdType
{
	Simple,
	Forking,
	OneShot,
	DBus,
	Notify,
	Idle
}
internal static class StartWorldConstants
{
	public static readonly TimeSpan WorldStartTimeout = TimeSpan.FromMinutes(1);
}

internal class StartWorldHandler : RequestHandlerAsync<StartWorldRequest>
{
	private const string PasswordRegex = "^[\\d\\w]+$";

	private readonly WorldService _worldService;
	private readonly RabbitClientMessageProducer _messageProducer;
	private readonly VanillaConfiguration _vanillaConfig;

	public StartWorldHandler(WorldService worldService, RabbitClientMessageProducer messageProducer, IOptions<VanillaConfiguration> vanillaConfig)
		=> (_worldService, _messageProducer, _vanillaConfig) = (worldService, messageProducer, vanillaConfig.Value);

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
			_messageProducer.ProduceMessage(new StartWorldMessage("toé tes une description", SystemdType.Simple, command.WorldName, false));
			await Task.Delay(5 * 1000, cancellationToken) // start the world
				.WaitAsync(StartWorldConstants.WorldStartTimeout, cancellationToken);
		}
		catch (TimeoutException)
		{
			throw new WorldStartTimeoutException();
		}
		_worldService.MarkWorldAsStarted(new WorldStartInfo(command.HostUserId, command.WorldName, command.Password)); // not really since the world is started asynchronously

		return await base.HandleAsync(command, cancellationToken);
	}
}

internal class InvalidPasswordException : Exception { }
internal class WorldDoesNotExistException : Exception { }
internal class WorldStartTimeoutException : Exception { }
