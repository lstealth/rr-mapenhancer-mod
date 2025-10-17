using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct SetSwitch : IGameMessage
{
	[Key(0)]
	public string NodeId { get; set; }

	[Key(1)]
	public bool Thrown { get; set; }

	[Key(2)]
	public long Tick { get; set; }

	[Key(3)]
	public string Requester { get; set; }

	public SetSwitch(string nodeId, bool thrown, long tick, string requester)
	{
		NodeId = nodeId;
		Thrown = thrown;
		Tick = tick;
		Requester = requester;
	}
}
