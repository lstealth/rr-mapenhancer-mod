using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct AutoEngineerWaypointRerouteRequest : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	public AutoEngineerWaypointRerouteRequest(string locomotiveId)
	{
		LocomotiveId = locomotiveId;
	}
}
