using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct FlareRemove : IGameMessage
{
	[Key(0)]
	public string Id;

	public FlareRemove(string id)
	{
		Id = id;
	}
}
