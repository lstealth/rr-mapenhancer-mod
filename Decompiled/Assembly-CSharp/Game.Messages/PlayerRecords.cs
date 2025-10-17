using System.Collections.Generic;
using Game.AccessControl;
using Game.Persistence;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct PlayerRecords : IGameMessage
{
	[Key(0)]
	public Dictionary<string, PlayerRecord> Records;

	public PlayerRecords(Dictionary<string, PlayerRecord> records)
	{
		Records = records;
	}
}
