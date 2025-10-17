using System.Collections.Generic;
using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct Transaction : IGameMessage
{
	[Key(0)]
	public List<IGameMessage> Messages;

	public Transaction(List<IGameMessage> messages)
	{
		Messages = messages;
	}
}
