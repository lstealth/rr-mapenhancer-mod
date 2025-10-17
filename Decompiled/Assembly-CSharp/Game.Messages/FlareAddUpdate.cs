using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct FlareAddUpdate : IGameMessage
{
	[Key(0)]
	public Snapshot.TrackLocation Location;

	public FlareAddUpdate(Snapshot.TrackLocation location)
	{
		Location = location;
	}
}
