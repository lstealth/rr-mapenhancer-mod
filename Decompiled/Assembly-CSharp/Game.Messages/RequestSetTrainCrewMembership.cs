using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[RequestSetTrainCrewMembershipRule]
[MessagePackObject(false)]
public struct RequestSetTrainCrewMembership : ICharacterMessage, IGameMessage
{
	[Key(0)]
	public string PlayerId { get; set; }

	[Key(1)]
	public string TrainCrewId { get; set; }

	[Key(2)]
	public bool Join { get; set; }

	public RequestSetTrainCrewMembership(string playerId, string trainCrewId, bool join)
	{
		PlayerId = playerId;
		TrainCrewId = trainCrewId;
		Join = join;
	}
}
