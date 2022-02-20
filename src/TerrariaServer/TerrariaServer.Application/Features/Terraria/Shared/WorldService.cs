using TerrariaServer.Application.Features.Terraria.Shared;

namespace TerrariaServer.Features.Terraria.Shared;

internal record WorldStartInfo(ulong User, string WorldName, string Password);
internal class WorldService
{
	// concurrent?
	private readonly Dictionary<string, WorldStartInfo> _worlds = new();
	internal bool IsWorldStarted(string worldName) => _worlds.ContainsKey(worldName);
	internal void MarkWorldAsStarted(WorldStartInfo worldStartInfo)
	{
		if (_worlds.ContainsKey(worldStartInfo.WorldName)) throw new WorldIsAlreadyStartedException();
		_worlds[worldStartInfo.WorldName] = worldStartInfo;
	}
	internal void MarkWorldAsStopped(string worldName)
	{
		if (!_worlds.ContainsKey(worldName)) throw new WorldIsNotStartedException();
		_worlds.Remove(worldName);
	}
	internal WorldStartInfo GetWorld(string worldName)
	{
		if (!_worlds.ContainsKey(worldName)) throw new WorldIsNotStartedException();
		return _worlds[worldName];
	}
}
