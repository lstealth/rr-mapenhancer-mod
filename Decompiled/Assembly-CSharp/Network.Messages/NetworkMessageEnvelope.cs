using System;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct NetworkMessageEnvelope : INetworkMessage
{
	[Key(0)]
	public byte Flags0;

	[Key(1)]
	public byte Flags1;

	[Key(2)]
	public ArraySegment<byte> Data;

	public NetworkMessageEnvelope(byte flags0, byte flags1, ArraySegment<byte> data)
	{
		Flags0 = flags0;
		Flags1 = flags1;
		Data = data;
	}

	public override string ToString()
	{
		return $"NetworkMessageEnvelope({Flags0:x2}, {Flags1:x2}, {Data.Count})";
	}
}
