using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct RequestSetSwitch : IGameMessage
{
	[Key(0)]
	public string nodeId { get; set; }

	[Key(1)]
	public bool thrown { get; set; }

	public RequestSetSwitch(string nodeId, bool thrown)
	{
		this.nodeId = nodeId;
		this.thrown = thrown;
	}
}
