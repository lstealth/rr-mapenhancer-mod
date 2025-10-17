using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Serilog;
using Steamworks;
using UnityEngine;

namespace Network.Steam;

public class ServerLobbyHelper : IDisposable
{
	private LobbyData _createdLobby;

	public void Dispose()
	{
		DestroyLobby();
	}

	public Task<ClientLobbyHelper.VoidResult> CreateLobby(string lobbyName, LobbyType lobbyType, int maxMembers = 32)
	{
		DestroyLobby();
		TaskCompletionSource<ClientLobbyHelper.VoidResult> tcs = new TaskCompletionSource<ClientLobbyHelper.VoidResult>();
		Matchmaking.Client.CreateLobby((ELobbyType)lobbyType, maxMembers, delegate(EResult result, LobbyData lobby, bool ioError)
		{
			if (ioError)
			{
				tcs.SetException(new Exception("IO Error creating lobby"));
			}
			else if (result != EResult.k_EResultOK)
			{
				tcs.SetException(new Exception($"Error creating lobby: {result}"));
			}
			else
			{
				Debug.Log($"Created lobby: {lobby.SteamId}");
				_createdLobby = lobby;
				lobby["name"] = lobbyName;
				lobby["ver"] = Application.version;
				lobby["status"] = "pending";
				Matchmaking.Client.SetLobbyGameServer(lobby, 0u, 0, App.Client.Owner.id);
				tcs.SetResult(default(ClientLobbyHelper.VoidResult));
			}
		});
		return tcs.Task;
	}

	public void UpdateLobby(bool allowNewPlayers, bool hasPassword, string reportingMark)
	{
		LobbyData createdLobby = _createdLobby;
		if (createdLobby.IsValid)
		{
			Log.Information("UpdateLobby allowNewPlayers = {allowNewPlayers}, hasPassword = {hasPassword}", allowNewPlayers, hasPassword);
			if (!createdLobby.Name.StartsWith(reportingMark + " "))
			{
				createdLobby.Name = reportingMark + " " + createdLobby.Name;
			}
			createdLobby["status"] = "open";
			createdLobby["allowNew"] = (allowNewPlayers ? "1" : "");
			createdLobby["passworded"] = (hasPassword ? "1" : "");
			createdLobby["rpmk"] = SanitizeReportingMark(reportingMark);
		}
	}

	public static string SanitizeReportingMark(string str)
	{
		str = str.Substring(0, Mathf.Min(4, str.Length)).ToUpper();
		str = Regex.Replace(str, "[^A-Z]", "");
		return str;
	}

	private void DestroyLobby()
	{
		if (_createdLobby.IsValid)
		{
			_createdLobby["status"] = "destroyed";
			_createdLobby.Leave();
		}
	}
}
