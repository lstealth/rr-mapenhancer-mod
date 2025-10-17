using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct FireEvent : IGameMessage
{
	[Key(0)]
	public int EventCode { get; set; }

	public FireEvent(int eventCode)
	{
		EventCode = eventCode;
	}
}
