using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RemoveCars : IGameMessage
{
	[Key(0)]
	public List<string> CarIds;

	public RemoveCars(List<string> carIds)
	{
		CarIds = carIds;
	}
}
