using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct AutoEngineerWaypointRouteRequest : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	public AutoEngineerWaypointRouteRequest(string locomotiveId)
	{
		LocomotiveId = locomotiveId;
	}
}
