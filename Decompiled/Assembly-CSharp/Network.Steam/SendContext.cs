using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using MessagePack;
using Network.Buffers;
using Network.Messages;
using Serilog;
using Steamworks;

namespace Network.Steam;

public class SendContext : IDisposable
{
	public struct Recipient
	{
		public HSteamNetConnection Connection;

		public EResult Result;

		public Recipient(HSteamNetConnection connection)
		{
			Connection = connection;
			Result = EResult.k_EResultNone;
		}
	}

	private readonly NetworkBufferWriter _bufferWriter = new NetworkBufferWriter();

	private readonly List<Recipient> _recipients = new List<Recipient>();

	private WriteBufferStream _gzipMemoryStream;

	public void Dispose()
	{
		_bufferWriter.Dispose();
		_gzipMemoryStream?.Dispose();
	}

	public void ClearRecipients()
	{
		_recipients.Clear();
	}

	public void AddRecipient(HSteamNetConnection connection)
	{
		_recipients.Add(new Recipient(connection));
	}

	public bool Send(INetworkMessage networkMessage)
	{
		if (_recipients.Count == 0)
		{
			return true;
		}
		Channel channel = Multiplayer.ChannelForMessage(networkMessage);
		_bufferWriter.Clear();
		MessagePackSerializer.Serialize(_bufferWriter, networkMessage);
		if (_bufferWriter.ArrayLength > 1024)
		{
			int arrayLength = _bufferWriter.ArrayLength;
			if (_gzipMemoryStream == null)
			{
				_gzipMemoryStream = new WriteBufferStream(new byte[131072]);
			}
			_gzipMemoryStream.Flush();
			using (GZipStream gZipStream = new GZipStream(_gzipMemoryStream, CompressionMode.Compress))
			{
				gZipStream.Write(_bufferWriter.Array, 0, _bufferWriter.ArrayLength);
			}
			NetworkMessageEnvelope networkMessageEnvelope = new NetworkMessageEnvelope(1, 0, _gzipMemoryStream.ArraySegment);
			_bufferWriter.Clear();
			MessagePackSerializer.Serialize((IBufferWriter<byte>)_bufferWriter, (INetworkMessage)networkMessageEnvelope, (MessagePackSerializerOptions)null, default(CancellationToken));
			Log.Debug("Compressed {name}: {before} to {after}", networkMessage.GetType().Name, arrayLength, _bufferWriter.ArrayLength);
		}
		try
		{
			SendToRecipients(_recipients, channel, _bufferWriter.Array, _bufferWriter.ArrayLength);
		}
		finally
		{
			_bufferWriter.Reset();
		}
		foreach (Recipient recipient in _recipients)
		{
			if (recipient.Result != EResult.k_EResultOK)
			{
				return false;
			}
		}
		return true;
	}

	private static void SendToRecipients(List<Recipient> recipients, Channel channel, byte[] data, int dataLength)
	{
		int nSendFlags = channel switch
		{
			Channel.Message => 8, 
			Channel.Movement => 0, 
			Channel.Data => 8, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		IntPtr pData = gCHandle.AddrOfPinnedObject();
		for (int i = 0; i < recipients.Count; i++)
		{
			Recipient value = recipients[i];
			try
			{
				long pOutMessageNumber;
				EResult result = SteamNetworkingSockets.SendMessageToConnection(value.Connection, pData, (uint)dataLength, nSendFlags, out pOutMessageNumber);
				value.Result = result;
			}
			catch (Exception)
			{
				value.Result = EResult.k_EResultUnexpectedError;
			}
			finally
			{
				recipients[i] = value;
			}
		}
		gCHandle.Free();
	}

	public static void ThrowIfError(EResult result)
	{
		switch (result)
		{
		case EResult.k_EResultInvalidParam:
			throw new Exception("SendTo: k_EResultInvalidParam - invalid connection handle, or the individual message is too big");
		case EResult.k_EResultInvalidState:
			throw new Exception("SendTo: k_EResultInvalidState - connection is in an invalid state");
		case EResult.k_EResultNoConnection:
			throw new Exception("SendTo: k_EResultNoConnection - connection has ended");
		case EResult.k_EResultIgnored:
			throw new Exception("SendTo: k_EResultIgnored - You used k_nSteamNetworkingSend_NoDelay, and the message was dropped because we were not ready to send it.");
		case EResult.k_EResultLimitExceeded:
			throw new Exception("SendTo: k_EResultLimitExceeded - there was already too much data queued to be sent. (See k_ESteamNetworkingConfig_SendBufferSize)");
		}
	}

	public IEnumerable<(HSteamNetConnection connection, EResult result)> ErroredRecipients()
	{
		foreach (Recipient recipient in _recipients)
		{
			if (recipient.Result != EResult.k_EResultOK)
			{
				yield return (connection: recipient.Connection, result: recipient.Result);
			}
		}
	}
}
