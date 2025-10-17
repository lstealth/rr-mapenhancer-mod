using System;
using System.IO.Compression;
using System.Runtime.InteropServices;
using MessagePack;
using Network.Buffers;
using Network.Messages;
using Steamworks;

namespace Network.Steam;

public class ReceiveContext
{
	private readonly ArraySegmentReadStream _gzipSourceStream = new ArraySegmentReadStream();

	private readonly WriteBufferStream _gzipDestStream = new WriteBufferStream(new byte[131072]);

	public INetworkMessage NetworkMessageFromPointer(IntPtr ptr, out HSteamNetConnection connection)
	{
		RawNetworkMessage rawNetworkMessage = RawNetworkMessage.FromPointer(ptr);
		connection = rawNetworkMessage.Connection;
		byte[] array = new byte[rawNetworkMessage.DataLength];
		Marshal.Copy(rawNetworkMessage.DataPtr, array, 0, rawNetworkMessage.DataLength);
		INetworkMessage networkMessage = MessagePackSerializer.Deserialize<INetworkMessage>(array);
		if (networkMessage is NetworkMessageEnvelope networkMessageEnvelope)
		{
			if (networkMessageEnvelope.Flags0 != 1 || networkMessageEnvelope.Flags1 != 0)
			{
				throw new Exception($"Unexpected flags on envelope: {networkMessageEnvelope}");
			}
			_gzipSourceStream.SetArraySegment(networkMessageEnvelope.Data);
			using GZipStream gZipStream = new GZipStream(_gzipSourceStream, CompressionMode.Decompress);
			_gzipDestStream.Flush();
			gZipStream.CopyTo(_gzipDestStream);
			networkMessage = MessagePackSerializer.Deserialize<INetworkMessage>(_gzipDestStream.ArraySegment);
		}
		return networkMessage;
	}
}
