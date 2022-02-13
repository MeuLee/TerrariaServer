using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TerrariaServer.Application.Features.Vanilla;

public class StartWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;
	private readonly VanillaConfiguration _vanillaConfig;

	internal StartWorldModule(IMediator mediator, IOptions<VanillaConfiguration> vanillaConfig)
	{
		_mediator = mediator;
		_vanillaConfig = vanillaConfig.Value;
	}

	[Command("start")]
	internal async Task StartWorldAsync(string worldName, string password)
	{
		try
		{
			var request = new StartWorldRequest(Context.User.Id, worldName, password);
			await Context.Channel.SendMessageAsync($"Starting world {worldName}.");
			await _mediator.Send(request);
			await Context.Channel.SendMessageAsync($"World {worldName} is up and running.");
		}
		catch (InvalidPasswordException)
		{
			await Context.Channel.SendMessageAsync("The password should be composed of numbers and letters only.");
		}
		catch (WorldDoesNotExistException)
		{
			var otherWorlds = ListWorlds();
			await Context.Channel.SendMessageAsync($"Could not find world: {worldName}.");
			if (!otherWorlds.Any()) return;
			await Context.Channel.SendMessageAsync($"Found the following worlds:\n{string.Join('\n', otherWorlds)}");
		}
		catch (WorldIsAlreadyStartedException ex)
		{
			var joinMessage = ex.WorldName == worldName ? $" You can join with the password: {ex.Password}" : string.Empty;
			await Context.Channel.SendMessageAsync($"World {ex.WorldName} is already started.{joinMessage}");
		}
		catch (WorldStartTimedOutException)
		{
			await Context.Channel.SendMessageAsync($"World {worldName} failed to start within the alloted timeout of {TimeSpan.FromMilliseconds(Constants.WorldStartTimeoutMilliseconds).TotalSeconds} seconds.");
		}
		catch (Exception ex)
		{
			await Context.Channel.SendMessageAsync($"Encountered unknown error while trying to start the world.\nError message: {ex.Message}");
		}
	}

	private List<string> ListWorlds()
	{
		if (!Directory.Exists(_vanillaConfig.WorldsFolderPath))
			return new List<string>();
		return Directory.GetFiles(_vanillaConfig.WorldsFolderPath, "*.wld")
			.Select(x => (Path.GetFileNameWithoutExtension(x) ?? string.Empty).Replace('_', ' '))
			.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
	}
}

internal record StartWorldRequest(ulong HostUserId, string WorldName, string Password) : IRequest<Unit>;
internal record WorldStartInfo(ulong User, string WorldName, string Password, Process Process);
internal class World
{
	internal WorldStartInfo? WorldStartInfo { get; set; }
}

internal static class Constants
{
	internal const int WorldStartTimeoutMilliseconds = 60 * 1000;
}

internal class StartWorldHandler : IRequestHandler<StartWorldRequest, Unit>
{
	private const string PasswordRegex = "^[\\d\\w]+$";

	private readonly VanillaConfiguration _vanillaConfig;
	private readonly World _world;

	private bool _worldStarted = false;

	public StartWorldHandler(World world, IOptions<VanillaConfiguration> vanillaConfig)
	{
		_world = world;
		_vanillaConfig = vanillaConfig.Value;
	}

	public async Task<Unit> Handle(StartWorldRequest request, CancellationToken cancellationToken)
	{
		if (_world.WorldStartInfo is not null)
			throw new WorldIsAlreadyStartedException { WorldName = _world.WorldStartInfo.WorldName, Password = _world.WorldStartInfo.Password };
		if (!Regex.IsMatch(request.Password, PasswordRegex))
			throw new InvalidPasswordException();
		var worldFilePath = Path.Combine(_vanillaConfig.WorldsFolderPath, $"{request.WorldName.Replace(' ', '_')}.wld");
		if (!File.Exists(worldFilePath))
			throw new WorldDoesNotExistException();
		var arguments = $@"-pass {request.Password} -world ""{worldFilePath}"" -port {_vanillaConfig.Port}";
		var process = await StartProcessAsync(_vanillaConfig.TerrariaServerPath, arguments);
		_world.WorldStartInfo = new WorldStartInfo(request.HostUserId, request.WorldName, request.Password, process);
		return Unit.Value;
	}

	private async Task<Process> StartProcessAsync(string fileName, string arguments)
	{
		var processStartInfo = new ProcessStartInfo
		{
			FileName = "/bin/bash",
			Arguments = $"{fileName} {arguments}",
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			UseShellExecute = false
		};
		var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
		process.OutputDataReceived += OutputDataReceived;
		process.Start();
		process.BeginOutputReadLine();
		var waitWorldTask = WaitForWorldStartedAsync();
		if (await Task.WhenAny(waitWorldTask, Task.Delay(Constants.WorldStartTimeoutMilliseconds)) == waitWorldTask)
		{
			return process;
		}
		else
		{
			throw new WorldStartTimedOutException();
		}
	}

	private async Task WaitForWorldStartedAsync()
	{
		while (!_worldStarted)
		{
			await Task.Delay(1000);
		}
	}

	private void OutputDataReceived(object sender, DataReceivedEventArgs e)
	{
		if (e.Data != $"Listening on port {_vanillaConfig.Port}") return;
		_worldStarted = true;
	}
}

internal class InvalidPasswordException : Exception { }
internal class WorldDoesNotExistException : Exception { }
internal class WorldIsAlreadyStartedException : Exception
{
	public string WorldName { get; init; } = default!;
	public string Password { get; init; } = default!;
}
internal class WorldStartTimedOutException : Exception { }