using System;
using Game.Messages;
using Game.State;

namespace Game.AccessControl;

[AttributeUsage(AttributeTargets.Struct)]
public class HostOnlyAuthorizationRuleAttribute : Attribute, IMessageAuthorizationRuleAttribute
{
	public bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message)
	{
		if (StateManager.IsHost)
		{
			return PlayersManager.PlayerId == senderPlayerId;
		}
		return false;
	}
}
