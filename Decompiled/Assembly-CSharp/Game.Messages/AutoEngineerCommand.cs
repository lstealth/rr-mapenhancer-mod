using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct AutoEngineerCommand : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	[Key(1)]
	public AutoEngineerMode Mode { get; set; }

	[Key(2)]
	public bool Forward { get; set; }

	[Key(3)]
	public int MaxSpeedMph { get; set; }

	[Key(4)]
	public float? Distance { get; set; }

	[Key(5)]
	public string WaypointLocationString { get; set; }

	[Key(6)]
	public string WaypointCoupleToCarId { get; set; }

	public AutoEngineerCommand(string locomotiveId, AutoEngineerMode mode, bool forward, int mph, float? distance, string waypointLocationString, string waypointCoupleToCarId)
	{
		LocomotiveId = locomotiveId;
		Mode = mode;
		Forward = forward;
		MaxSpeedMph = mph;
		Distance = distance;
		WaypointLocationString = waypointLocationString;
		WaypointCoupleToCarId = waypointCoupleToCarId;
	}
}
