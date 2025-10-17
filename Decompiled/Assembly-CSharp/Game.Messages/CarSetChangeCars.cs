using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct CarSetChangeCars : IGameMessage
{
	[Key(0)]
	public Snapshot.CarSet Set;

	public CarSetChangeCars(Snapshot.CarSet set)
	{
		Set = set;
	}

	public override string ToString()
	{
		return $"CarSetChangeCars: Set={Set}";
	}
}
