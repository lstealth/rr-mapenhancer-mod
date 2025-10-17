using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct WaitTime : IGameMessage
{
	[Key(0)]
	public float Hours;
}
