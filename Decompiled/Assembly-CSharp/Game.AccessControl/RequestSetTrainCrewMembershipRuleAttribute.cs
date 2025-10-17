using System;
using Game.Messages;
using Game.State;

namespace Game.AccessControl;

[AttributeUsage(AttributeTargets.Struct)]
public class RequestSetTrainCrewMembershipRuleAttribute : Attribute, IMessageAuthorizationRuleAttribute
{
	public bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message)
	{
		if (!(message is RequestSetTrainCrewMembership requestSetTrainCrewMembership))
		{
			return false;
		}
		if (StateManager.Shared.Storage.TrainCrewMembershipManagedByTrainmaster)
		{
			return senderAccessLevel >= AccessLevel.Trainmaster;
		}
		if (requestSetTrainCrewMembership.PlayerId == senderPlayerId.String)
		{
			return senderAccessLevel >= AccessLevel.Crew;
		}
		return senderAccessLevel >= AccessLevel.Trainmaster;
	}
}
