using System.Collections.Generic;
using Game.Messages;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct PlayerList : INetworkMessage
{
	[Key(0)]
	public Dictionary<string, Snapshot.Player> Players;

	public PlayerList(Dictionary<string, Snapshot.Player> players)
	{
		Players = players;
	}
}
