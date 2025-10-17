using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RequestDeleteTrainCrew : IGameMessage
{
	[Key(0)]
	public string TrainCrewId { get; set; }

	public RequestDeleteTrainCrew(string trainCrewId)
	{
		TrainCrewId = trainCrewId;
	}
}
