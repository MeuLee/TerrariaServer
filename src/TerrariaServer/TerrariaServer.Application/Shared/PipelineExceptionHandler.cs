using MediatR;

namespace TerrariaServer.Application.Shared;

internal class PipelineExceptionHandler<TRequest, TResponse> : IPipelineBehavior<TRequest, Unit>
    where TRequest : IRequestWithMessageId
{
    private readonly ISocketCommandContextProvider _commandContextProvider;

	public PipelineExceptionHandler(ISocketCommandContextProvider commandContextProvider)
	{
		_commandContextProvider = commandContextProvider;
	}

	public async Task<Unit> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<Unit> next)
	{
        try
        {
            return await next();
        }
        catch (Exception ex)
		{
            var commandContext = _commandContextProvider.ProvideContext(request.MessageId);
            await commandContext.Channel.SendMessageAsync($"Encountered unknown error while executing command: {request.GetCommandName()}.\nException message: {ex.Message}");
        }
        return Unit.Value;
    }
}