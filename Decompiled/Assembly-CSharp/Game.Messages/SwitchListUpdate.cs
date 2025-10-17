using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct SwitchListUpdate : IGameMessage
{
	[Key(0)]
	public string TrainCrewId;

	[Key(1)]
	public SwitchList SwitchList;

	public SwitchListUpdate(string trainCrewId, SwitchList switchList)
	{
		TrainCrewId = trainCrewId;
		SwitchList = switchList;
	}
}
