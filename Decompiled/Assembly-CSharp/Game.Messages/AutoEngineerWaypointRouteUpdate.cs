using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct AutoEngineerWaypointRouteUpdate : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	[Key(1)]
	public Snapshot.TrackLocation? Current { get; set; }

	[Key(2)]
	public bool RouteChanged { get; set; }

	public AutoEngineerWaypointRouteUpdate(string locomotiveId, Snapshot.TrackLocation? current, bool routeChanged)
	{
		LocomotiveId = locomotiveId;
		Current = current;
		RouteChanged = routeChanged;
	}
}
