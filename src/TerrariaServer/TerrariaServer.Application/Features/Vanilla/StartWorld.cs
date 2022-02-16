using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TerrariaServer.Application.Features.Vanilla;

public class StartWorldModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal StartWorldModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("start")]
	internal async Task StartWorldAsync(string worldName, string password)
	{
		var request = new StartWorldRequest(Context.User.Id, worldName, password, Context.Message.Id);
		await _mediator.Send(request);
	}
}

internal record StartWorldRequest(ulong HostUserId, string WorldName, string Password, ulong MessageId) : IRequest;

// todo should be moved elsewhere
internal record WorldStartInfo(ulong User, string WorldName, string Password, Process Process);
internal class World
{
	internal WorldStartInfo? WorldStartInfo { get; set; }
}

internal class StartWorldHandler : IRequestHandler<StartWorldRequest>
{
	private const string PasswordRegex = "^[\\d\\w]+$";

	private readonly VanillaConfiguration _vanillaConfig;
	private readonly World _world;
	private readonly ISocketCommandContextProvider _commandContextProvider;
	private readonly TimeSpan _worldStartTimeout = TimeSpan.FromMinutes(1);
	private bool _worldStarted = false;

	public StartWorldHandler(World world, ISocketCommandContextProvider commandContextProvider, IOptions<VanillaConfiguration> vanillaConfig)
	{
		_world = world;
		_commandContextProvider = commandContextProvider;
		_vanillaConfig = vanillaConfig.Value;
	}

	public async Task<Unit> Handle(StartWorldRequest request, CancellationToken cancellationToken)
	{
		var commandContext = _commandContextProvider.ProvideContext(request.MessageId);
		await commandContext.Channel.SendMessageAsync($"Starting world {request.WorldName}.");
		if (_world.WorldStartInfo is not null)
		{
			var joinMessage = _world.WorldStartInfo.WorldName == request.WorldName ? $" You can join with the password: {_world.WorldStartInfo.Password}" : string.Empty;
			await commandContext.Channel.SendMessageAsync($"World {_world.WorldStartInfo.WorldName} is already started.{joinMessage}");
			return Unit.Value;
		}
		if (!Regex.IsMatch(request.Password, PasswordRegex))
		{
			await commandContext.Channel.SendMessageAsync("The password should be composed of numbers and letters only.");
			return Unit.Value;
		}
		var worldFilePath = Path.Combine(_vanillaConfig.WorldsFolderPath, $"{request.WorldName.Replace(' ', '_')}.wld");
		if (!File.Exists(worldFilePath))
		{
			var otherWorlds = ListWorlds();
			await commandContext.Channel.SendMessageAsync($"Could not find world: {request.WorldName}.");
			if (!otherWorlds.Any())
				return Unit.Value;
			await commandContext.Channel.SendMessageAsync($"Found the following worlds:\n{string.Join('\n', otherWorlds)}");
			return Unit.Value;
		}
		var arguments = $@"-pass {request.Password} -world ""{worldFilePath}"" -port {_vanillaConfig.Port}";
		try
		{
			var process = await StartProcessAsync(_vanillaConfig.TerrariaServerPath, arguments);
			_world.WorldStartInfo = new WorldStartInfo(request.HostUserId, request.WorldName, request.Password, process);
			await commandContext.Channel.SendMessageAsync($"World {request.WorldName} is up and running.");
		}
		catch (TimeoutException)
		{
			await commandContext.Channel.SendMessageAsync($"World {request.WorldName} failed to start within the alloted timeout of {_worldStartTimeout.TotalSeconds} seconds.");
		}

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
		await WaitForWorldStartedAsync().WaitAsync(_worldStartTimeout);
		return process;

		void OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != $"Listening on port {_vanillaConfig.Port}")
				return;
			_worldStarted = true;
		}

		async Task WaitForWorldStartedAsync()
		{
			while (!_worldStarted)
			{
				await Task.Delay(1000);
			}
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