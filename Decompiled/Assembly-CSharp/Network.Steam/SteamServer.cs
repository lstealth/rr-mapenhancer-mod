using System;
using System.Collections.Generic;
using Network.Buffers;
using Network.Server;
using Serilog;
using Steamworks;
using UnityEngine;

namespace Network.Steam;

public class SteamServer : MonoBehaviour, IServerManager
{
	private class Client
	{
		public HSteamNetConnection Connection { get; }

		public ClientId ClientId { get; }

		public ulong RemotePlayerSteamId { get; }

		public Client(HSteamNetConnection connection, ClientId clientId, ulong remotePlayerSteamId)
		{
			Connection = connection;
			ClientId = clientId;
			RemotePlayerSteamId = remotePlayerSteamId;
		}
	}

	public IServerDelegate Delegate;

	private Callback<SteamNetConnectionStatusChangedCallback_t> _callbackConnectionStatusChanged;

	private HSteamListenSocket _listenSocket = HSteamListenSocket.Invalid;

	private HSteamNetPollGroup _pollGroup = SteamNetworkingSockets.CreatePollGroup();

	private readonly Dictionary<uint, Client> _clients = new Dictionary<uint, Client>();

	private IntPtr[] _receivedMessagePointers = new IntPtr[32];

	private readonly ReceiveContext _receiveContext = new ReceiveContext();

	private readonly NetworkBufferWriter _bufferWriter = new NetworkBufferWriter();

	private void OnEnable()
	{
		_callbackConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
	}

	private void OnDestroy()
	{
		if (_listenSocket != HSteamListenSocket.Invalid)
		{
			SteamNetworkingSockets.CloseListenSocket(_listenSocket);
			_listenSocket = HSteamListenSocket.Invalid;
		}
		foreach (Client value in _clients.Values)
		{
			SteamNetworkingSockets.CloseConnection(value.Connection, 0, null, bEnableLinger: false);
		}
		_clients.Clear();
		SteamNetworkingSockets.DestroyPollGroup(_pollGroup);
		_pollGroup = HSteamNetPollGroup.Invalid;
		_callbackConnectionStatusChanged.Dispose();
		_callbackConnectionStatusChanged = null;
		_bufferWriter.Dispose();
	}

	private void Update()
	{
		ReceiveMessages();
	}

	public void StartListening()
	{
		Debug.Log("CreateListenSocketP2P");
		_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	public void ReceiveMessages()
	{
		int num = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, _receivedMessagePointers, _receivedMessagePointers.Length);
		for (int i = 0; i < num; i++)
		{
			IntPtr intPtr = _receivedMessagePointers[i];
			try
			{
				HSteamNetConnection connection;
				INetworkMessage message = _receiveContext.NetworkMessageFromPointer(intPtr, out connection);
				Client client = ClientForConnection(connection);
				Delegate.HandleMessage(this, client.ClientId, message);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while handling message:");
				Debug.LogException(exception);
			}
			finally
			{
				SteamNetworkingMessage_t.Release(intPtr);
			}
		}
		if (num == _receivedMessagePointers.Length)
		{
			ReceiveMessages();
		}
	}

	private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
	{
		HSteamNetConnection hConn = param.m_hConn;
		SteamNetConnectionInfo_t info = param.m_info;
		Log.Information("SteamServer.OnConnectionStatusChanged: {connection}: {oldState} -> {newState}", hConn, param.m_eOldState, info.m_eState);
		switch (info.m_eState)
		{
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
		{
			ulong steamID = info.m_identityRemote.GetSteamID64();
			if (!Delegate.ShouldAcceptConnection(steamID))
			{
				Debug.LogWarning($"Closing connection from Steam identity {steamID}");
				SteamNetworkingSockets.CloseConnection(hConn, 0, null, bEnableLinger: false);
			}
			else if (SteamNetworkingSockets.AcceptConnection(hConn) != EResult.k_EResultOK)
			{
				SteamNetworkingSockets.CloseConnection(hConn, 0, null, bEnableLinger: false);
				Debug.LogWarning("Failed to accept connection -- already closed?");
			}
			else if (!SteamNetworkingSockets.SetConnectionPollGroup(hConn, _pollGroup))
			{
				SteamNetworkingSockets.CloseConnection(hConn, 0, null, bEnableLinger: false);
				Debug.LogWarning("Failed to set poll group");
			}
			else
			{
				Client client2 = new Client(hConn, new ClientId(hConn.m_HSteamNetConnection), steamID);
				_clients[client2.Connection.m_HSteamNetConnection] = client2;
			}
			break;
		}
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
		{
			ESteamNetworkingConnectionState eOldState = param.m_eOldState;
			if ((uint)(eOldState - 4) > 1u)
			{
				HandleConnectionClosedStatus(hConn, info.m_eState, param.m_eOldState);
			}
			break;
		}
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
			HandleConnectionClosedStatus(hConn, info.m_eState, param.m_eOldState);
			break;
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
		{
			Client client = ClientForConnection(hConn);
			DispatchClientDidConnect(client);
			break;
		}
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
			break;
		}
	}

	private void HandleConnectionClosedStatus(HSteamNetConnection connection, ESteamNetworkingConnectionState state, ESteamNetworkingConnectionState oldState)
	{
		if (oldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
		{
			Client client = ClientForConnection(connection);
			Log.Information("Client Disconnected: {reason}", state switch
			{
				ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None => "Local Disconnect", 
				ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally => "ProblemDetectedLocally", 
				ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer => "ClosedByPeer", 
				_ => state.ToString(), 
			});
			DispatchClientDidDisconnect(client);
			RemoveClient(client);
		}
		else
		{
			Debug.LogError($"Lost connection with unexpected oldState: {oldState}");
		}
		SteamNetworkingSockets.CloseConnection(connection, 0, null, bEnableLinger: false);
	}

	private void RemoveClient(Client client)
	{
		_clients.Remove(client.Connection.m_HSteamNetConnection);
		Debug.Log($"RemoveClient: {_clients.Count} remain");
	}

	private Client ClientForConnection(HSteamNetConnection hConn)
	{
		return _clients[hConn.m_HSteamNetConnection];
	}

	private Client ClientForId(ClientId clientId)
	{
		foreach (var (_, client2) in _clients)
		{
			if (client2.ClientId.Equals(clientId))
			{
				return client2;
			}
		}
		throw new ArgumentException("No such clientId");
	}

	private void DispatchClientDidConnect(Client client)
	{
		Delegate.ClientDidConnect(this, client.ClientId, client.RemotePlayerSteamId);
	}

	private void DispatchClientDidDisconnect(Client client)
	{
		Delegate.ClientDidDisconnect(this, client.ClientId);
	}

	public void DropClient(ClientId clientId, int steamworksReasonCode, string debugReason)
	{
		SteamNetworkingSockets.CloseConnection(ClientForId(clientId).Connection, steamworksReasonCode, debugReason, bEnableLinger: true);
	}

	public void SendTo(ClientId clientId, INetworkMessage networkMessage)
	{
	}

	internal bool TryGetClientConnection(ClientId clientId, out HSteamNetConnection connection)
	{
		foreach (var (_, client2) in _clients)
		{
			if (client2.ClientId.Equals(clientId))
			{
				connection = client2.Connection;
				return true;
			}
		}
		connection = default(HSteamNetConnection);
		return false;
	}
}
