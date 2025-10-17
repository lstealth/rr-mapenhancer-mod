using System;
using System.Threading.Tasks;
using Network.Client;
using Serilog;
using Steamworks;

namespace Network.Steam;

public class SteamClient : GameClient
{
	private CSteamID _gameServerId;

	private HSteamNetConnection _connection;

	private Callback<SteamNetConnectionStatusChangedCallback_t> _callbackConnectionStatusChanged;

	private TaskCompletionSource<ClientLobbyHelper.VoidResult> _tcsConnect;

	private bool _isConnectedOrConnecting;

	private readonly IntPtr[] _receivedMessagePointers = new IntPtr[32];

	private readonly SendContext _sendContext = new SendContext();

	private readonly ReceiveContext _receiveContext = new ReceiveContext();

	public override bool IsConnectedOrConnecting => _isConnectedOrConnecting;

	private void Awake()
	{
		_callbackConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		Disconnect();
		_callbackConnectionStatusChanged.Dispose();
		_callbackConnectionStatusChanged = null;
		_sendContext.Dispose();
	}

	private void Update()
	{
		int num = SteamNetworkingSockets.ReceiveMessagesOnConnection(_connection, _receivedMessagePointers, _receivedMessagePointers.Length);
		for (int i = 0; i < num; i++)
		{
			IntPtr ptr = _receivedMessagePointers[i];
			try
			{
				HSteamNetConnection connection;
				INetworkMessage message = _receiveContext.NetworkMessageFromPointer(ptr, out connection);
				HandleMessage(message);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while handling message");
			}
		}
	}

	public override void Setup(ConnectionInfo connectionInfo)
	{
		base.Setup(connectionInfo);
		_gameServerId = connectionInfo.GameServerId;
	}

	public override async Task Connect()
	{
		_isConnectedOrConnecting = true;
		_tcsConnect = new TaskCompletionSource<ClientLobbyHelper.VoidResult>();
		SteamNetworkingIdentity identityRemote = default(SteamNetworkingIdentity);
		identityRemote.SetSteamID(_gameServerId);
		_connection = SteamNetworkingSockets.ConnectP2P(ref identityRemote, 0, 0, null);
		try
		{
			await _tcsConnect.Task;
		}
		finally
		{
			_tcsConnect = null;
		}
		SendHello();
	}

	public override void Disconnect()
	{
		SteamNetworkingSockets.CloseConnection(_connection, 1000, "OnDestroy", bEnableLinger: true);
	}

	public override void SendNetworkMessage(INetworkMessage message, Channel channel)
	{
		_sendContext.ClearRecipients();
		_sendContext.AddRecipient(_connection);
		if (_sendContext.Send(message) || channel == Channel.Movement)
		{
			return;
		}
		foreach (var (propertyValue, propertyValue2) in _sendContext.ErroredRecipients())
		{
			Log.Error("Send error for {connection}: {result}", propertyValue, propertyValue2);
		}
		Disconnect();
	}

	private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
	{
		if (param.m_hConn != _connection)
		{
			return;
		}
		Log.Information("SteamClient.OnConnectionStatusChanged: {connection}: {oldState} -> {newState}", param.m_hConn, param.m_eOldState, param.m_info.m_eState);
		switch (param.m_info.m_eState)
		{
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
			if (param.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
			{
				Log.Error("Failed to connect: {reason}", param.m_info.m_szEndDebug);
				_tcsConnect?.SetException(new Exception("Failed to connect: " + param.m_info.m_szEndDebug));
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Log.Error("Lost connection: {reason}", param.m_info.m_szEndDebug);
				_tcsConnect?.SetException(new Exception("Problem detected locally: " + param.m_info.m_szEndDebug));
			}
			else
			{
				_tcsConnect?.SetException(new Exception("Error: " + param.m_info.m_szEndDebug));
			}
			SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, null, bEnableLinger: false);
			_isConnectedOrConnecting = false;
			base.ClientDelegate.ClientDidDisconnect(param.m_info.m_eEndReason);
			break;
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
			_tcsConnect?.SetResult(default(ClientLobbyHelper.VoidResult));
			break;
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
			break;
		}
	}
}
