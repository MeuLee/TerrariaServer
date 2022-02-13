using Discord.Commands;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;
using TerrariaServer.Application.Extensions;

namespace TerrariaServer.Application.Features;

public class DisplayHelpModule : ModuleBase<SocketCommandContext>
{
	private readonly DiscordConfiguration _discordConfig;
	private readonly string _displayHelpMessage;

	internal DisplayHelpModule(IOptions<DiscordConfiguration> discordConfig)
	{
		_discordConfig = discordConfig.Value;
		_displayHelpMessage = GetDisplayHelpMessage();
	}

	[Command("help")]
	internal async Task DisplayHelpAsync()
	{
		await Context.Channel.SendMessageAsync(_displayHelpMessage);
	}

	private string GetDisplayHelpMessage()
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

			stringBuilder.AppendLine($"\tUsage: {_discordConfig.Prefix}{command}{parametersStringBuilder}");
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