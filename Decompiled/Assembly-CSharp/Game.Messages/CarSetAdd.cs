using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct CarSetAdd : IGameMessage
{
	[Key(0)]
	public Snapshot.CarSet Set;

	public CarSetAdd(Snapshot.CarSet set)
	{
		Set = set;
	}

	public override string ToString()
	{
		return $"CarSetAdd: Set={Set}";
	}
}
