using System;
using System.Linq;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Steamworks;
using UnityEngine;

namespace Network;

public class SteamTester : MonoBehaviour
{
	private LobbyData[] _lobbies = Array.Empty<LobbyData>();

	private Callback<SteamNetConnectionStatusChangedCallback_t> _callbackConnectionStatusChanged;

	[ContextMenu("Test Request Lobby List")]
	public void TestRequestLobbyList()
	{
		Matchmaking.Client.AddRequestLobbyListStringFilter("ver", Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
		Matchmaking.Client.RequestLobbyList(delegate(LobbyData[] lobbies, bool error)
		{
			Debug.Log(string.Format("Received {0} lobbies: {1}", lobbies.Length, string.Join(", ", lobbies.Select((LobbyData l) => l.Name))));
			_lobbies = lobbies;
		});
	}

	[ContextMenu("Test Create Lobby")]
	public void TestCreateLobby()
	{
		HostGame("Southern Railway", 32);
	}

	private void JoinLobby(CSteamID lobbyId)
	{
		Matchmaking.Client.JoinLobby(LobbyForId(lobbyId), delegate
		{
		});
		Matchmaking.Client.EventLobbyGameCreated.AddListener(delegate
		{
		});
	}

	private LobbyData LobbyForId(CSteamID lobbyId)
	{
		return _lobbies.First((LobbyData lobby) => lobby.SteamId.Equals(lobbyId));
	}

	private void HostGame(string gameName, int maxMembers)
	{
		CreateListenSocket();
		Matchmaking.Client.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxMembers, delegate(EResult result, LobbyData lobby, bool ioError)
		{
			if (ioError)
			{
				Debug.LogError("IO error creating lobby");
			}
			else if (result != EResult.k_EResultOK)
			{
				Debug.LogError($"Error creating lobby: {result}");
			}
			else
			{
				Debug.Log($"Created lobby: {lobby.SteamId}");
				Matchmaking.Client.SetLobbyData(lobby, "ver", Application.version);
				Matchmaking.Client.SetLobbyData(lobby, "name", gameName);
				Matchmaking.Client.SetLobbyGameServer(lobby, 0u, 0, App.Client.Owner.id);
			}
		});
	}

	private void CreateListenSocket()
	{
		Debug.Log("CreateListenSocketP2P");
		SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
		_callbackConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
	}

	private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
	{
		HSteamNetConnection hConn = param.m_hConn;
		SteamNetworkingSockets.GetConnectionInfo(hConn, out var pInfo);
		switch (pInfo.m_eState)
		{
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
			SteamNetworkingSockets.AcceptConnection(hConn);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Linger:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FinWait:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
		case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState__Force32Bit:
			break;
		}
	}

	private void OnP2PSessionRequest(P2PSessionRequest_t param)
	{
	}

	private void TestConnect()
	{
	}
}
