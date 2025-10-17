using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct TurntableUpdateStopIndex : IGameMessage
{
	[Key(0)]
	public string TurntableId;

	[Key(1)]
	public long Tick;

	[Key(2)]
	public float Angle;

	[Key(3)]
	public int? StopIndex;

	public TurntableUpdateStopIndex(string turntableId, long tick, float angle, int? stopIndex)
	{
		TurntableId = turntableId;
		Tick = tick;
		Angle = angle;
		StopIndex = stopIndex;
	}
}
