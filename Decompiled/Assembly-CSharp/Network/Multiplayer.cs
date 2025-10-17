using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game;
using Game.Messages;
using Game.Persistence;
using Game.State;
using HeathenEngineering.SteamworksIntegration;
using JetBrains.Annotations;
using Network.Client;
using Network.Messages;
using Network.Steam;
using Serilog;
using Steamworks;
using UI.Common;
using UnityEngine;

namespace Network;

public static class Multiplayer
{
	private static GameObject _serverGameObject;

	private static GameObject _clientGameObject;

	private static ClientLobbyHelper _clientLobbyHelper;

	private static ServerLobbyHelper _serverLobbyHelper;

	private static SteamServer _steamServer;

	public static bool IsClientActive => Client?.IsClientStatusActive ?? false;

	public static ulong MySteamId => UserData.Me.SteamId;

	[CanBeNull]
	public static ClientManager Client { get; private set; }

	public static ConnectionMode Mode { get; private set; }

	private static HostManager Host { get; set; }

	public static bool IsHost
	{
		get
		{
			if (Host == null)
			{
				return !Application.isPlaying;
			}
			return true;
		}
	}

	public static ClientLobbyHelper ClientLobbyHelper => _clientLobbyHelper ?? (_clientLobbyHelper = new ClientLobbyHelper());

	private static Snapshot.CharacterCustomization CharacterCustomizationForConnect => ClientManager.MakeCharacterCustomizationUsingPreferences(lanternEnabled: false);

	public static void PrepareHostIfNeeded(INetworkSetup networkSetup)
	{
		Host?.Dispose();
		Host = null;
		if (!(networkSetup is StartSingleplayerSetup) && !(networkSetup is StartMultiplayerHostSetup))
		{
			if (!(networkSetup is JoinMultiplayerSetup))
			{
				throw new ArgumentException($"Unknown networkSetup {networkSetup}", "networkSetup");
			}
		}
		else
		{
			Host = new HostManager();
		}
	}

	public static async Task ConnectClient(INetworkSetup networkSetup)
	{
		CreateClient();
		string username = Preferences.MultiplayerClientUsername;
		Snapshot.CharacterCustomization customization = CharacterCustomizationForConnect;
		if (!(networkSetup is StartSingleplayerSetup))
		{
			if (!(networkSetup is JoinMultiplayerSetup joinSetup))
			{
				if (networkSetup is StartMultiplayerHostSetup startMultiplayerHostSetup)
				{
					Mode = ConnectionMode.MultiplayerServer;
					await StartMultiplayerServer(startMultiplayerHostSetup.LobbyName, startMultiplayerHostSetup.LobbyType);
					await Client.Connect(ConnectionInfo.MultiplayerHost(username, "", customization));
				}
			}
			else
			{
				Mode = ConnectionMode.MultiplayerClient;
				Log.Debug("Joining Lobby...");
				await ClientLobbyHelper.JoinLobby(joinSetup.LobbyId);
				Log.Debug("Getting GameServerId...");
				CSteamID gameServerId = await ClientLobbyHelper.GetGameServerId(joinSetup.LobbyId);
				Log.Debug("Got GameServerId.");
				await Client.Connect(ConnectionInfo.MultiplayerClient(gameServerId, username, joinSetup.Password, customization));
			}
		}
		else
		{
			Mode = ConnectionMode.Singleplayer;
			if (string.IsNullOrWhiteSpace(username))
			{
				username = "Player";
			}
			await Client.Connect(ConnectionInfo.Singleplayer(username, customization));
		}
	}

	public static async Task StartMultiplayerServer(string lobbyName, LobbyType lobbyType)
	{
		_serverGameObject = new GameObject
		{
			name = "Server"
		};
		UnityEngine.Object.DontDestroyOnLoad(_serverGameObject);
		_steamServer = _serverGameObject.AddComponent<SteamServer>();
		_steamServer.Delegate = Host;
		_steamServer.StartListening();
		_serverLobbyHelper = new ServerLobbyHelper();
		await _serverLobbyHelper.CreateLobby(lobbyName, lobbyType);
	}

	public static void StopMultiplayerServer()
	{
		_serverLobbyHelper.Dispose();
		UnityEngine.Object.DestroyImmediate(_serverGameObject);
		_steamServer = null;
	}

	public static void UpdateLobbyFlags()
	{
		if (!(_steamServer == null) && _serverLobbyHelper != null)
		{
			GameStorage storage = StateManager.Shared.Storage;
			_serverLobbyHelper.UpdateLobby(storage.AllowNewPlayers, storage.HasNewPlayerPassword, storage.RailroadMark);
		}
	}

	private static void CreateClient()
	{
		_clientGameObject = new GameObject
		{
			name = "ClientManager"
		};
		UnityEngine.Object.DontDestroyOnLoad(_clientGameObject);
		ClientManager clientManager = _clientGameObject.AddComponent<ClientManager>();
		clientManager.OnDisconnect += OnClientDisconnect;
		Client = clientManager;
		StateManager.Shared.PlayersManager.OnClientCreated(clientManager);
	}

	public static void StopServer()
	{
		if (_steamServer != null)
		{
			StopMultiplayerServer();
		}
		Host?.Dispose();
		Host = null;
	}

	public static void SendError(IPlayer player, string message, AlertLevel alertLevel = AlertLevel.Error)
	{
		Log.Debug("SendError to {player}: {message}", player, message);
		Alert alert = new Alert(AlertStyle.Toast, alertLevel, message, TimeWeather.Now.TotalSeconds);
		if (player.IsRemote)
		{
			Host.SendTo(player.PlayerId, alert);
		}
		else
		{
			WindowManager.Shared.Present(alert);
		}
	}

	public static void Broadcast(string message)
	{
		Log.Information("Broadcast: {message}", message);
		Alert alert = new Alert(AlertStyle.Console, AlertLevel.Info, message, TimeWeather.Now.TotalSeconds);
		if (Host == null)
		{
			WindowManager.Shared.Present(alert);
		}
		else
		{
			Host.SendToAll(alert);
		}
	}

	private static void OnClientDisconnect()
	{
		Client = null;
		UnityEngine.Object.DestroyImmediate(_clientGameObject);
		_clientGameObject = null;
		_clientLobbyHelper?.Dispose();
		_clientLobbyHelper = null;
		StateManager.Shared.ReturnToMainMenu();
	}

	public static Channel ChannelForMessage(IGameMessage message, bool forceReliable = false)
	{
		if (!(message is AddCars) && !(message is SwitchListUpdate) && !(message is Transaction))
		{
			if (!(message is UpdateCharacterPosition) && !(message is UpdateCameraPosition))
			{
				if (!(message is BatchCarPositionUpdate batchCarPositionUpdate))
				{
					if (!(message is TurntableUpdateAngle))
					{
						if (message is PlaySoundAtPosition)
						{
							if (!forceReliable)
							{
								return Channel.Movement;
							}
							return Channel.Message;
						}
						return Channel.Message;
					}
					if (!forceReliable)
					{
						return Channel.Movement;
					}
					return Channel.Message;
				}
				if (!forceReliable && !batchCarPositionUpdate.Critical)
				{
					return Channel.Movement;
				}
				return Channel.Message;
			}
			if (!forceReliable)
			{
				return Channel.Movement;
			}
			return Channel.Message;
		}
		return Channel.Data;
	}

	public static Channel ChannelForMessage(INetworkMessage message)
	{
		if (!(message is GameMessageEnvelope gameMessageEnvelope))
		{
			if (message is SnapshotEnvelope)
			{
				return Channel.Data;
			}
			return Channel.Message;
		}
		return ChannelForMessage(gameMessageEnvelope.gameMessage);
	}

	public static Dictionary<string, PlayerRecord> PlayerRecordsForSave()
	{
		return Host.PlayerRecordsForSave();
	}
}
