using System;
using Game.AccessControl;
using Game.Messages;
using MessagePack;

namespace Game.Persistence;

[MessagePackObject(false)]
public struct PlayerRecord
{
	[Key("name")]
	public string Name;

	[Key("position")]
	public CharacterPosition Position;

	[Key("updated")]
	public DateTime Updated;

	[Key("steamId")]
	public ulong SteamId;

	[Key("accessLevel")]
	public AccessLevel AccessLevel;

	[Key("accessLevelChanged")]
	public DateTime AccessLevelChanged;

	[Key("lastConnected")]
	public DateTime LastConnected;
}
