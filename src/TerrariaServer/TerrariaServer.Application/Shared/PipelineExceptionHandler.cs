using MediatR;

namespace TerrariaServer.Application.Shared;

internal class PipelineExceptionHandler<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var response = await next();
        return response;
    }
}