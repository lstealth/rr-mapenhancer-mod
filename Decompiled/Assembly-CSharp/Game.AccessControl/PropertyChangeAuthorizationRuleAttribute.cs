using System;
using Game.Messages;
using Game.State;

namespace Game.AccessControl;

[AttributeUsage(AttributeTargets.Struct)]
public class PropertyChangeAuthorizationRuleAttribute : Attribute, IMessageAuthorizationRuleAttribute
{
	public bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message)
	{
		if (!(message is PropertyChange propertyChange))
		{
			return false;
		}
		return StateManager.Shared.CheckAuthorizationForPropertyChange(propertyChange.ObjectId, propertyChange.Key, senderPlayerId, senderAccessLevel);
	}
}
