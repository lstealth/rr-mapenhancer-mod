using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct TurntableUpdateAngle : IGameMessage
{
	[Key(0)]
	public string TurntableId;

	[Key(1)]
	public long Tick;

	[Key(2)]
	public float Angle;

	public TurntableUpdateAngle(string turntableId, long tick, float angle)
	{
		TurntableId = turntableId;
		Tick = tick;
		Angle = angle;
	}
}
