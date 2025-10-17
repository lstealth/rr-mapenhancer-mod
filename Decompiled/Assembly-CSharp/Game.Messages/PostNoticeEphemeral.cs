using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct PostNoticeEphemeral : IGameMessage
{
	[Key(0)]
	public SerializableEntityReference Entity;

	[Key(1)]
	public string Key;

	[Key(2)]
	public string Content;

	public PostNoticeEphemeral(SerializableEntityReference entity, string key, string content)
	{
		Entity = entity;
		Key = key;
		Content = content;
	}
}
