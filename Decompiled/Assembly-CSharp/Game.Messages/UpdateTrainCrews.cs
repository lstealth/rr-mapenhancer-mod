using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct UpdateTrainCrews : IGameMessage
{
	[Key(0)]
	public Dictionary<string, Snapshot.TrainCrew> TrainCrews { get; set; }

	public UpdateTrainCrews(Dictionary<string, Snapshot.TrainCrew> trainCrews)
	{
		TrainCrews = trainCrews;
	}
}
