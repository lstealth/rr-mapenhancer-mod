using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct SetTimeOfDay : IGameMessage
{
	[Key(0)]
	public float TimeOfDay;

	public SetTimeOfDay(float timeOfDay)
	{
		TimeOfDay = timeOfDay;
	}
}
