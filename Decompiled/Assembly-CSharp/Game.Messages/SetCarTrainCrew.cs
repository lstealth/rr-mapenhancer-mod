using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct SetCarTrainCrew : IGameMessage
{
	[Key(0)]
	public string CarId;

	[Key(1)]
	public string TrainCrewId;

	public SetCarTrainCrew(string carId, string trainCrewId)
	{
		CarId = carId;
		TrainCrewId = trainCrewId;
	}
}
