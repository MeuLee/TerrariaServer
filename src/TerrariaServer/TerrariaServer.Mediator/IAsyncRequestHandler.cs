using Discord.WebSocket;

namespace TerrariaServer.Application.Mediator;

public interface IAsyncRequestHandler<TRequest, TResponse> where TRequest: IRequest<TResponse>
{
	Task<TResponse> HandleAsync(TRequest request, ISocketMessageChannel channel, CancellationToken cancellationToken = default);
}

public interface IAsyncRequestHandler<TRequest>
{
	Task HandleAsync(TRequest request, ISocketMessageChannel channel, CancellationToken cancellationToken = default);
}
