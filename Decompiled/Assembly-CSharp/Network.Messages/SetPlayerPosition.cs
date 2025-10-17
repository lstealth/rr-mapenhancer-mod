using Game.Messages;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct SetPlayerPosition : INetworkMessage
{
	[Key(0)]
	public CharacterPosition Position { get; set; }

	public SetPlayerPosition(CharacterPosition position)
	{
		Position = position;
	}
}
