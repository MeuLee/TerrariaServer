using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;

namespace TerrariaServer.Application.Shared;

public interface ISocketCommandContextFactory
{
	SocketCommandContext CreateContext(DiscordSocketClient client, SocketUserMessage message);
}

public interface ISocketCommandContextProvider
{
	SocketCommandContext ProvideContext(ulong messageId);
}

internal partial class SocketCommandContextFactory : ISocketCommandContextProvider
{
	private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(1);
	private readonly IMemoryCache _memoryCache;

	public SocketCommandContextFactory(IMemoryCache memoryCache)
		=> _memoryCache = memoryCache;
}

internal partial class SocketCommandContextFactory : ISocketCommandContextFactory
{
	public SocketCommandContext CreateContext(DiscordSocketClient client, SocketUserMessage message)
	{
		var context = new SocketCommandContext(client, message);
		_memoryCache.Set(message.Id, context, _cacheTimeout);
		return context;
	}
}

internal partial class SocketCommandContextFactory : ISocketCommandContextProvider
{
	public SocketCommandContext ProvideContext(ulong messageId)
		=> _memoryCache.Get<SocketCommandContext>(messageId);
}
