using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game;
using Network.Buffers;
using Network.Server;
using Serilog;

namespace Network.Client;

public class LocalGameClient : GameClient, IServerManager
{
	private HostManager HostManager;

	private bool _connected;

	private readonly List<(INetworkMessage, Channel)> _pendingSend = new List<(INetworkMessage, Channel)>();

	private readonly List<INetworkMessage> _pendingReceive = new List<INetworkMessage>();

	private readonly NetworkBufferWriter _buffer = new NetworkBufferWriter();

	public override bool IsConnectedOrConnecting => _connected;

	private static ClientId ClientId => new ClientId(0uL);

	public void Setup(ConnectionInfo connectionInfo, HostManager host)
	{
		base.Setup(connectionInfo);
		HostManager = host;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		Disconnect();
		_buffer.Dispose();
	}

	private void Update()
	{
		for (int i = 0; i < _pendingReceive.Count; i++)
		{
			INetworkMessage networkMessage = _pendingReceive[i];
			try
			{
				HandleMessage(networkMessage);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while 'receiving' message {message}:", networkMessage);
			}
		}
		_pendingReceive.Clear();
		int count = _pendingSend.Count;
		for (int j = 0; j < count; j++)
		{
			INetworkMessage item = _pendingSend[j].Item1;
			try
			{
				HostManager.HandleMessage(this, ClientId, item);
			}
			catch (Exception exception2)
			{
				Log.Error(exception2, "Exception while 'sending' message {message}:", item);
			}
		}
		_pendingSend.Clear();
	}

	public override void Disconnect()
	{
		if (_connected)
		{
			HostManager.ClientDidDisconnect(this, ClientId);
			_connected = false;
		}
	}

	public override async Task Connect()
	{
		_connected = true;
		HostManager.ClientDidConnect(this, ClientId, Multiplayer.MySteamId);
		SendHello();
	}

	public override void SendNetworkMessage(INetworkMessage message, Channel channel)
	{
		_pendingSend.Add((CleanMessage(message), channel));
	}

	public void DropClient(ClientId clientId, int steamworksReasonCode, string debugReason)
	{
		Disconnect();
	}

	public void SendTo(ClientId clientId, INetworkMessage networkMessage)
	{
		_pendingReceive.Add(CleanMessage(networkMessage));
	}

	private INetworkMessage CleanMessage(INetworkMessage input)
	{
		return input;
	}
}
