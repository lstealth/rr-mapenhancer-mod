using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct SwitchListSetCarIds : IGameMessage
{
	[Key(0)]
	public string TrainCrewId;

	[Key(1)]
	public List<string> CarIds;

	public SwitchListSetCarIds(string trainCrewId, List<string> carIds)
	{
		TrainCrewId = trainCrewId;
		CarIds = carIds;
	}
}
