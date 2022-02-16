using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TerrariaServer.Application;

public partial class TerrariaServerWorker
{
	private readonly DiscordSocketClient _client;
	private readonly DiscordConfiguration _discordConfig;
	private readonly CommandService _commandService;
	private readonly IServiceProvider _serviceProvider;
	private readonly ISocketCommandContextFactory _commandContextFactory;

	public TerrariaServerWorker(
		DiscordSocketClient client,
		CommandService commandService,
		IOptions<DiscordConfiguration> discordConfig,
		IServiceProvider serviceProvider,
		ISocketCommandContextFactory commandContextFactory)
	{
		_client = client;
		_commandService = commandService;
		_discordConfig = discordConfig.Value;
		_serviceProvider = serviceProvider;
		_commandContextFactory = commandContextFactory;
		RegisterEventHandlers();
	}

	private void RegisterEventHandlers()
	{
		_client.MessageReceived += MessageReceivedHandler;
		_client.Log += LogHandler;
		_commandService.Log += LogHandler;
	}

	private async Task MessageReceivedHandler(SocketMessage message)
	{
		if (message is not SocketUserMessage socketUserMessage || message.Author.IsBot)
			return;
		var context = _commandContextFactory.CreateContext(_client, socketUserMessage);
		if (!_discordConfig.AllowedChannels.Contains(socketUserMessage.Channel.Id))
			return;
		if (!socketUserMessage.HasStringPrefix(_discordConfig.Prefix, out var argumentsPosition))
			return;
		var commandResult = await _commandService.ExecuteAsync(context, argumentsPosition, _serviceProvider);
	}

	private async Task LogHandler(LogMessage log)
	{
		await Console.Out.WriteLineAsync(log.Message);
	}
}

public partial class TerrariaServerWorker : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		await _client.LoginAsync(TokenType.Bot, _discordConfig.BotToken);
		await _client.StartAsync();
	}

	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		await _client.LogoutAsync();
		await _client.StopAsync();
	}
}

public partial class TerrariaServerWorker : IAsyncDisposable
{
	private bool _disposed = false;

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return DisposeAsync(true);
	}

	private async ValueTask DisposeAsync(bool disposing)
	{
		if (_disposed) return;
		if (disposing)
		{
			await StopAsync();
			_client.Dispose();
		}
		_disposed = true;
	}
}

internal static class UserMessageExtensions
{
	internal static bool HasStringPrefix(this IUserMessage message, string prefix, out int index)
	{
		index = 0;
		return message.HasStringPrefix(prefix, ref index);
	}
}