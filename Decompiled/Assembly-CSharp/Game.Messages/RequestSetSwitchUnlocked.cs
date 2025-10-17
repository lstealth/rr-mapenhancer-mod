using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct RequestSetSwitchUnlocked : IGameMessage
{
	[Key(0)]
	public string NodeId { get; set; }

	[Key(1)]
	public bool Unlocked { get; set; }

	public RequestSetSwitchUnlocked(string nodeId, bool unlocked)
	{
		NodeId = nodeId;
		Unlocked = unlocked;
	}
}
