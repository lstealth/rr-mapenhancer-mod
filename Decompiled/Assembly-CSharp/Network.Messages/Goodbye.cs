using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct Goodbye : INetworkMessage
{
	[Key(0)]
	public string Reason;
}
