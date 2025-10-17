using Track;

namespace Game;

public struct FlareAdded
{
	public string Key;

	public Location Location;

	public FlareAdded(string key, Location location)
	{
		Key = key;
		Location = location;
	}
}
