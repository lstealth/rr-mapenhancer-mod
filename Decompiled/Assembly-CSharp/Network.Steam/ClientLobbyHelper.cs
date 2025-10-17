using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Steamworks;
using UnityEngine;

namespace Network.Steam;

public class ClientLobbyHelper : IDisposable
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct VoidResult
	{
	}

	private LobbyData[] _lobbies = Array.Empty<LobbyData>();

	private LobbyData _joinedLobby;

	private TaskCompletionSource<VoidResult> _taskCompletionSourceGameCreated = new TaskCompletionSource<VoidResult>();

	public ClientLobbyHelper()
	{
		Matchmaking.Client.EventLobbyGameCreated.AddListener(HandleLobbyGameCreated);
		Matchmaking.Client.EventLobbyAskedToLeave.AddListener(HandleLobbyAskedToLeave);
	}

	public void Dispose()
	{
		Matchmaking.Client.EventLobbyGameCreated.RemoveListener(HandleLobbyGameCreated);
		Matchmaking.Client.EventLobbyAskedToLeave.RemoveListener(HandleLobbyAskedToLeave);
		_joinedLobby.Leave();
	}

	public Task<LobbyData[]> FetchLobbies(string reportingMark)
	{
		TaskCompletionSource<LobbyData[]> tcs = new TaskCompletionSource<LobbyData[]>();
		Matchmaking.Client.AddRequestLobbyListStringFilter("ver", Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
		Matchmaking.Client.AddRequestLobbyListStringFilter("status", "open", ELobbyComparison.k_ELobbyComparisonEqual);
		Matchmaking.Client.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
		if (!string.IsNullOrEmpty(reportingMark))
		{
			Matchmaking.Client.AddRequestLobbyListStringFilter("rpmk", ServerLobbyHelper.SanitizeReportingMark(reportingMark), ELobbyComparison.k_ELobbyComparisonEqual);
		}
		Matchmaking.Client.RequestLobbyList(delegate(LobbyData[] lobbies, bool error)
		{
			if (error)
			{
				_lobbies = Array.Empty<LobbyData>();
				Debug.LogError("Error fetching lobbies");
				tcs.SetException(new Exception("Error fetching lobbies"));
			}
			else
			{
				Debug.Log(string.Format("Received {0} lobbies: {1}", lobbies.Length, string.Join(", ", lobbies.Select((LobbyData l) => l.Name))));
				_lobbies = lobbies;
				tcs.SetResult(lobbies);
			}
		});
		return tcs.Task;
	}

	public Task<VoidResult> JoinLobby(LobbyData lobby)
	{
		_taskCompletionSourceGameCreated?.TrySetCanceled();
		_taskCompletionSourceGameCreated = new TaskCompletionSource<VoidResult>();
		_joinedLobby.Leave();
		Matchmaking.Client.RequestLobbyData(lobby);
		TaskCompletionSource<VoidResult> tcs = new TaskCompletionSource<VoidResult>();
		Matchmaking.Client.JoinLobby(lobby, delegate(LobbyEnter lobbyEnter, bool error)
		{
			if (error)
			{
				tcs.SetException(new Exception("Error joining lobby"));
			}
			else
			{
				_joinedLobby = lobby;
				tcs.SetResult(default(VoidResult));
				if (lobby.HasServer)
				{
					_taskCompletionSourceGameCreated.SetResult(default(VoidResult));
				}
			}
		});
		return tcs.Task;
	}

	public async Task<CSteamID> GetGameServerId(CSteamID joinSetupLobbyId)
	{
		await _taskCompletionSourceGameCreated.Task;
		return _joinedLobby.GameServer.id;
	}

	private void HandleLobbyGameCreated(LobbyGameCreated_t arg)
	{
		Debug.Log("LobbyGameCreated");
		_taskCompletionSourceGameCreated.SetResult(default(VoidResult));
	}

	private void HandleLobbyAskedToLeave(LobbyData arg0)
	{
		Debug.Log("LobbyAskedToLeave");
	}
}
