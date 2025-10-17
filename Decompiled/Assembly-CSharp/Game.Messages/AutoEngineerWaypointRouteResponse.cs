using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct AutoEngineerWaypointRouteResponse : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	[Key(1)]
	public List<Snapshot.TrackLocation> Locations { get; set; }

	[Key(2)]
	public bool HasMore { get; set; }

	public AutoEngineerWaypointRouteResponse(string locomotiveId, List<Snapshot.TrackLocation> locations, bool hasMore)
	{
		LocomotiveId = locomotiveId;
		Locations = locations;
		HasMore = hasMore;
	}
}
