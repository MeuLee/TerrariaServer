namespace TerrariaServer.Application;

public class VanillaConfiguration
{
	public string TerrariaServerPath { get; init; } = default!;
	public string WorldsFolderPath { get; init; } = default!;
	public int Port { get; init; }
}

public class ModdedConfiguration
{
	public string TerrariaServerPath { get; init; } = default!;
	public string WorldsFolderPath { get; init; } = default!;
	public int Port { get; init; }
}

public class DiscordConfiguration
{
	public ulong AdminUser { get; init; }
	public ulong[] AllowedChannels { get; init; } = default!;
	public string BotToken { get; init; } = default!;
	public string Prefix { get; init; } = default!;
}