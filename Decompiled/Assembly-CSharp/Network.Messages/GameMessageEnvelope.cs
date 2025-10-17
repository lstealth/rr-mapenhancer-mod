using Game.Messages;
using JetBrains.Annotations;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct GameMessageEnvelope : INetworkMessage
{
	[Key(0)]
	public string sender;

	[Key(1)]
	public IGameMessage gameMessage;

	public GameMessageEnvelope([NotNull] string sender, [NotNull] IGameMessage gameMessage)
	{
		this.sender = sender;
		this.gameMessage = gameMessage;
	}

	public override string ToString()
	{
		return $"GameMessage({sender}, {gameMessage})";
	}
}
