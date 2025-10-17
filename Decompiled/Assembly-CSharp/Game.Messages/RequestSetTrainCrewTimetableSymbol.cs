using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RequestSetTrainCrewTimetableSymbol : IGameMessage
{
	[Key(0)]
	public string TrainCrewId { get; set; }

	[Key(1)]
	public string TimetableSymbol { get; set; }

	public RequestSetTrainCrewTimetableSymbol(string trainCrewId, string timetableSymbol)
	{
		TrainCrewId = trainCrewId;
		TimetableSymbol = timetableSymbol;
	}
}
