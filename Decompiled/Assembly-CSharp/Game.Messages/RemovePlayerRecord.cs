using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.President)]
[MessagePackObject(false)]
public struct RemovePlayerRecord : IGameMessage
{
	[Key(0)]
	public string RecordKey;

	public RemovePlayerRecord(string recordKey)
	{
		RecordKey = recordKey;
	}
}
