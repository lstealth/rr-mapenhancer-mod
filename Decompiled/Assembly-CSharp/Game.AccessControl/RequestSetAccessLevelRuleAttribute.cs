using System;
using Game.Messages;

namespace Game.AccessControl;

[AttributeUsage(AttributeTargets.Struct)]
public class RequestSetAccessLevelRuleAttribute : Attribute, IMessageAuthorizationRuleAttribute
{
	public bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message)
	{
		if (!(message is RequestSetAccessLevel { AccessLevel: var accessLevel }))
		{
			return false;
		}
		AccessLevel accessLevel2 = ((accessLevel >= AccessLevel.Trainmaster) ? ((accessLevel < AccessLevel.Officer) ? AccessLevel.Officer : AccessLevel.President) : ((accessLevel < AccessLevel.Crew) ? AccessLevel.Trainmaster : AccessLevel.Trainmaster));
		AccessLevel accessLevel3 = accessLevel2;
		return senderAccessLevel >= accessLevel3;
	}
}
