using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[RequestSetAccessLevelRule]
[MessagePackObject(false)]
public struct RequestSetAccessLevel : IGameMessage
{
	[Key(0)]
	public string RecordKey;

	[Key(1)]
	public AccessLevel AccessLevel;

	public RequestSetAccessLevel(string recordKey, AccessLevel accessLevel)
	{
		RecordKey = recordKey;
		AccessLevel = accessLevel;
	}
}
