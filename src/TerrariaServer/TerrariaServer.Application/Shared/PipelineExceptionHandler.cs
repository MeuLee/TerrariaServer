using MediatR;

namespace TerrariaServer.Application.Shared;

internal class PipelineExceptionHandler<TRequest, TResponse> : IPipelineBehavior<TRequest, Unit>
    where TRequest : IRequestWithMessageId
{
	public async Task<Unit> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<Unit> next)
	{
        var response = await next();
        return response;
    }
}