using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct Hello : INetworkMessage
{
	[Key(0)]
	public int MajorVersion;

	[Key(1)]
	public int MinorVersion;
}
