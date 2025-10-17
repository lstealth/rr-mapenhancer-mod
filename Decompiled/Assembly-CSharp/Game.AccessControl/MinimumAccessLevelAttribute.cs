using System;
using Game.Messages;

namespace Game.AccessControl;

[AttributeUsage(AttributeTargets.Struct)]
public class MinimumAccessLevelAttribute : Attribute, IMessageAuthorizationRuleAttribute
{
	public AccessLevel MinimumLevel { get; }

	public MinimumAccessLevelAttribute(AccessLevel minimumLevel)
	{
		MinimumLevel = minimumLevel;
	}

	public bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message)
	{
		return senderAccessLevel >= MinimumLevel;
	}
}
