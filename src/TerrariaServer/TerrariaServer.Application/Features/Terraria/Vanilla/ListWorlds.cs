using Discord.Commands;
using Microsoft.Extensions.Options;
using Paramore.Darker;

namespace TerrariaServer.Application.Features.Terraria.Vanilla;

public class ListWorldsModule : ModuleBase<SocketCommandContext>
{
	private readonly IQueryProcessor _queryProcessor;

	internal ListWorldsModule(IQueryProcessor queryProcessor)
		=> _queryProcessor = queryProcessor;

	[Command("list worlds")]
	internal async Task ListWorldsAsync()
	{
		var request = new ListWorldsRequest();
		var response = _queryProcessor.Execute(request);
		var displayMessage = $"Found the following vanilla worlds:\n\t{string.Join("\n\t", response.Worlds)}";
		await ReplyAsync(displayMessage);
	}
}

internal record ListWorldsRequest : IQuery<ListWorldsResponse>;
internal record ListWorldsResponse(List<string> Worlds);

internal class ListWorldsHandler : QueryHandler<ListWorldsRequest, ListWorldsResponse>
{
	private readonly VanillaConfiguration _vanillaConfig;

	public ListWorldsHandler(IOptions<VanillaConfiguration> vanillaConfig)
		=> _vanillaConfig = vanillaConfig.Value;

	public override ListWorldsResponse Execute(ListWorldsRequest query)
	{
		var worlds = ListWorlds();
		return new ListWorldsResponse(worlds);
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
