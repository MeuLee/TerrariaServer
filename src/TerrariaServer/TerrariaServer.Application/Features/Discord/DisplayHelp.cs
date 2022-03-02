using Discord.Commands;
using Microsoft.Extensions.Options;
using Paramore.Darker;
using System.Reflection;
using System.Text;

namespace TerrariaServer.Application.Features.Discord;

public class DisplayHelpModule : ModuleBase<SocketCommandContext>
{
	private readonly IQueryProcessor _queryProcessor;

	internal DisplayHelpModule(IQueryProcessor queryProcessor)
		=> _queryProcessor = queryProcessor;

	[Command("help")]
	internal async Task DisplayHelpAsync()
	{
		var request = new DisplayHelpQuery();
		var response = _queryProcessor.Execute(request);
		await ReplyAsync(response.HelpMessage);
	}	
}

public record DisplayHelpQuery : IQuery<DisplayHelpResult>;
public record DisplayHelpResult(string HelpMessage);

public class DisplayHelpHandler : QueryHandler<DisplayHelpQuery, DisplayHelpResult>
{
	private readonly DiscordConfiguration _discordConfig;
	private string? _helpMessage;

	public DisplayHelpHandler(IOptions<DiscordConfiguration> discordConfig)
	{
		_discordConfig = discordConfig.Value;
	}

	public override DisplayHelpResult Execute(DisplayHelpQuery query)
	{
		var helpMessage = _helpMessage ??= GenerateHelpMessage(_discordConfig.Prefix);
		return new DisplayHelpResult(helpMessage);
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