using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct SwitchListToggleCarIds : IGameMessage
{
	[Key(0)]
	public string TrainCrewId;

	[Key(1)]
	public List<string> CarIds;

	[Key(2)]
	public bool On;

	public SwitchListToggleCarIds(string trainCrewId, List<string> carIds, bool on)
	{
		TrainCrewId = trainCrewId;
		CarIds = carIds;
		On = on;
	}
}
