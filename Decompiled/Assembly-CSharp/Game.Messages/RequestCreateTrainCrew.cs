using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RequestCreateTrainCrew : IGameMessage
{
	[Key(0)]
	public Snapshot.TrainCrew TrainCrew { get; set; }

	public RequestCreateTrainCrew(Snapshot.TrainCrew trainCrew)
	{
		TrainCrew = trainCrew;
	}
}
