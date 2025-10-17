using Game.Messages;

namespace Game.AccessControl;

public interface IMessageAuthorizationRuleAttribute
{
	bool CheckAuthorization(PlayerId senderPlayerId, AccessLevel senderAccessLevel, IGameMessage message);
}
