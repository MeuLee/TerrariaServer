using Discord.WebSocket;

namespace TerrariaServer.Application.Mediator;

public interface IMediator
{
	Task SendAsync(IRequest request, ISocketMessageChannel channel, CancellationToken cancellationToken = default);
	Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, ISocketMessageChannel channel, CancellationToken cancellationToken = default);
}

internal class Mediator : IMediator
{
	private readonly IServiceProvider _serviceProvider;

	internal Mediator(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task SendAsync(IRequest request, ISocketMessageChannel channel, CancellationToken cancellationToken)
	{
		var requestHandlerType = typeof(IAsyncRequestHandler<IRequest>);
		if (_serviceProvider.GetService(requestHandlerType) is not IAsyncRequestHandler<IRequest> requestHandler)
			throw new NonRegisteredServiceException(requestHandlerType);
		await requestHandler.HandleAsync(request, cancellationToken);
	}

	public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, ISocketMessageChannel channel, CancellationToken cancellationToken)
	{
		var requestHandlerType = typeof(IAsyncRequestHandler<IRequest<TResponse>, TResponse>);
		if (_serviceProvider.GetService(requestHandlerType) is not IAsyncRequestHandler<IRequest<TResponse>, TResponse> requestHandler)
			throw new NonRegisteredServiceException(requestHandlerType);
		return await requestHandler.HandleAsync(request, cancellationToken);
	}
}

public class NonRegisteredServiceException : Exception
{
	internal NonRegisteredServiceException(Type nonRegisteredType)
		: base($"The type {nonRegisteredType} is not registered in the ServiceProvider.")
	{ }
}
