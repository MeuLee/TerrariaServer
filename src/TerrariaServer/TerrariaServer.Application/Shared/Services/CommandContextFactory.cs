using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;

namespace TerrariaServer.Application.Shared.Services;

public interface ICommandContextFactory
{
	ICommandContext CreateContext(DiscordSocketClient client, SocketUserMessage message);
}

public interface ICommandContextProvider
{
	ICommandContext ProvideContext(ulong messageId);
}

internal partial class CommandContextFactory : ICommandContextProvider
{
	private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(1);
	private readonly IMemoryCache _memoryCache;

	public CommandContextFactory(IMemoryCache memoryCache)
		=> _memoryCache = memoryCache;
}

internal partial class CommandContextFactory : ICommandContextFactory
{
	public ICommandContext CreateContext(DiscordSocketClient client, SocketUserMessage message)
	{
		var context = new SocketCommandContext(client, message);
		_memoryCache.Set(message.Id, context, _cacheTimeout);
		return context;
	}
}

internal partial class CommandContextFactory : ICommandContextProvider
{
	public ICommandContext ProvideContext(ulong messageId)
		=> _memoryCache.Get<SocketCommandContext>(messageId);
}
