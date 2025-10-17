using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct RequestPurchaseEquipment : IGameMessage
{
	[Key(0)]
	public List<string> PrototypeIds;

	public RequestPurchaseEquipment(List<string> prototypeIds)
	{
		PrototypeIds = prototypeIds;
	}
}
