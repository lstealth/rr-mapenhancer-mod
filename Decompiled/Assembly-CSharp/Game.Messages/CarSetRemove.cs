using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct CarSetRemove : IGameMessage
{
	[Key(0)]
	public uint SetId;

	public CarSetRemove(uint setId)
	{
		SetId = setId;
	}

	public override string ToString()
	{
		return $"CarSetRemove: SetId={SetId}";
	}
}
