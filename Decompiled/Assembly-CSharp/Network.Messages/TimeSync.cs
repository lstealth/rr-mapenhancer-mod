using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct TimeSync : INetworkMessage
{
	[Key(0)]
	public long tick;

	public TimeSync(long tick)
	{
		this.tick = tick;
	}
}
