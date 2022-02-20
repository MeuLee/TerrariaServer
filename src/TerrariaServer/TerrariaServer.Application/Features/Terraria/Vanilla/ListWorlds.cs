using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class ListWorldsModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal ListWorldsModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("list worlds")]
	internal async Task ListWorldsAsync()
	{
		var request = new ListWorldsRequest();
		var response = await _mediator.Send(request);
		var displayMessage = $"Found the following vanilla worlds:\n\t{string.Join("\n\t", response.Worlds)}";
		await ReplyAsync(displayMessage);
	}
}

internal record ListWorldsRequest : IRequest<ListWorldsResponse>;
internal record ListWorldsResponse(List<string> Worlds);

internal class ListWorldsHandler : IRequestHandler<ListWorldsRequest, ListWorldsResponse>
{
	private readonly VanillaConfiguration _vanillaConfig;

	public ListWorldsHandler(IOptions<VanillaConfiguration> vanillaConfig)
		=> _vanillaConfig = vanillaConfig.Value;

	public Task<ListWorldsResponse> Handle(ListWorldsRequest request, CancellationToken cancellationToken)
	{
		var worlds = ListWorlds();
		return Task.FromResult(new ListWorldsResponse(worlds));
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
