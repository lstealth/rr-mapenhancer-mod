using System;
using System.Runtime.InteropServices;
using Steamworks;

namespace Network.Steam;

internal readonly struct RawNetworkMessage
{
	public readonly HSteamNetConnection Connection;

	public readonly SteamNetworkingIdentity Identity;

	public readonly IntPtr DataPtr;

	public readonly int DataLength;

	public readonly SteamNetworkingMicroseconds TimeReceived;

	public readonly long MessageNumber;

	public readonly int Channel;

	public RawNetworkMessage(HSteamNetConnection connection, SteamNetworkingIdentity identity, IntPtr dataPtr, int dataLength, SteamNetworkingMicroseconds timeReceived, long messageNumber, int channel)
	{
		Connection = connection;
		Identity = identity;
		DataPtr = dataPtr;
		DataLength = dataLength;
		TimeReceived = timeReceived;
		MessageNumber = messageNumber;
		Channel = channel;
	}

	public static RawNetworkMessage FromPointer(IntPtr ptr)
	{
		SteamNetworkingMessage_t steamNetworkingMessage_t = Marshal.PtrToStructure<SteamNetworkingMessage_t>(ptr);
		return new RawNetworkMessage(steamNetworkingMessage_t.m_conn, steamNetworkingMessage_t.m_identityPeer, steamNetworkingMessage_t.m_pData, steamNetworkingMessage_t.m_cbSize, steamNetworkingMessage_t.m_usecTimeReceived, steamNetworkingMessage_t.m_nMessageNumber, steamNetworkingMessage_t.m_nChannel);
	}
}
