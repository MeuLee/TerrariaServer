using MediatR;

namespace TerrariaServer.Application.Shared
{
	internal interface IRequestWithMessageId : IRequest
	{
		ulong MessageId { get; }
	}

	internal static class RequestWithMessageIdExtensions
	{
		internal static string GetCommandName(this IRequestWithMessageId request) => request.GetType().Name.Replace("Request", "");
	}
}
