using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RequestEditTrainCrew : IGameMessage
{
	[Key(0)]
	public string TrainCrewId { get; set; }

	[Key(1)]
	public string Name { get; set; }

	[Key(2)]
	public string Description { get; set; }

	public RequestEditTrainCrew(string trainCrewId, string name, string description)
	{
		TrainCrewId = trainCrewId;
		Name = name;
		Description = description;
	}
}
