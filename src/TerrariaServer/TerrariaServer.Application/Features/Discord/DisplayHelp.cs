using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;

namespace TerrariaServer.Application.Features.Discord;

public class DisplayHelpModule : ModuleBase<SocketCommandContext>
{
	private readonly IMediator _mediator;

	internal DisplayHelpModule(IMediator mediator)
		=> _mediator = mediator;

	[Command("help")]
	internal async Task DisplayHelpAsync()
	{
		var request = new DisplayHelpRequest(Context.Message.Id);
		var response = await _mediator.Send(request);
		await ReplyAsync(response.HelpMessage);
	}	
}

internal record DisplayHelpRequest(ulong MessageId) : IRequest<DisplayHelpResponse>;
internal record DisplayHelpResponse(string HelpMessage);

internal class DisplayHelpHandler : IRequestHandler<DisplayHelpRequest, DisplayHelpResponse>
{
	private readonly DiscordConfiguration _discordConfig;
	private string? _helpMessage;

	public DisplayHelpHandler(IOptions<DiscordConfiguration> discordConfig)
	{
		_discordConfig = discordConfig.Value;
	}

	public Task<DisplayHelpResponse> Handle(DisplayHelpRequest request, CancellationToken cancellationToken)
	{
		var helpMessage = _helpMessage ??= GenerateHelpMessage(_discordConfig.Prefix);
		return Task.FromResult(new DisplayHelpResponse(helpMessage));
	}

	private static string GenerateHelpMessage(string commandPrefix)
	{
		var modules = Assembly
			.GetExecutingAssembly()
			.GetTypes(type => type.IsAssignableTo(typeof(ModuleBase<SocketCommandContext>)));
		var methods = modules.SelectMany(module => module.GetMethods(
			method => method.HasAttributes(typeof(CommandAttribute)), BindingFlags.Instance | BindingFlags.NonPublic));
		var stringBuilder = new StringBuilder();
		foreach (var method in methods)
		{
			var command = method.GetCustomAttribute<CommandAttribute>()!.Text;
			stringBuilder.AppendLine($"Command: {command}");
			var parametersStringBuilder = new StringBuilder();
			foreach (var parameter in method.GetParameters())
			{
				parametersStringBuilder.Append($" <{parameter.Name}> ");
			}

			stringBuilder.AppendLine($"\tUsage: {commandPrefix}{command}{parametersStringBuilder}");
		}

		return stringBuilder.ToString();
	}
}

internal static class ReflectionExtensions
{
	internal static List<MethodInfo> GetMethods(this Type type, Func<MethodInfo, bool> selector, BindingFlags bindingFlags)
		=> type.GetMethods(bindingFlags).Where(selector).ToList();

	internal static List<Type> GetTypes(this Assembly assembly, Func<Type, bool> selector)
		=> assembly.GetTypes().Where(selector).ToList();

	internal static bool HasAttributes(this MethodInfo methodInfo, params Type[] attributes)
	{
		var methodAttributes = methodInfo.GetCustomAttributes().Select(x => x.GetType());
		return attributes.All(methodAttributes.Contains);
	}
}