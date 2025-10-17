using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.AccessControl;
using Game.Messages;
using Game.Persistence;
using Game.State;
using HeathenEngineering.SteamworksIntegration;
using Helpers;
using MessagePack;
using Model;
using Network;
using Network.Buffers;
using Network.Messages;
using Network.Server;
using Network.Steam;
using Serilog;
using Track;
using UnityEngine;

namespace Game;

public class HostManager : IServerDelegate, IDisposable
{
	private class Client
	{
		public readonly ClientId ClientId;

		public readonly IServerManager Server;

		public Network.Server.ClientStatus Status;

		public PlayerInfo? PlayerInfo;

		public ulong RemotePlayerSteamId { get; }

		public PlayerId PlayerId => PlayerInfo?.PlayerId ?? PlayerId.Invalid;

		public Client(ClientId clientId, IServerManager server, ulong remotePlayerSteamId)
		{
			ClientId = clientId;
			Server = server;
			RemotePlayerSteamId = remotePlayerSteamId;
		}

		public override string ToString()
		{
			return ClientId.ToString();
		}

		public void Disconnect(DisconnectReason reason, string debugReason = null)
		{
			Server.DropClient(ClientId, (int)reason, debugReason);
		}

		public string NameForLog()
		{
			if (!PlayerInfo.HasValue)
			{
				string name = UserData.Get(RemotePlayerSteamId).Name;
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
				return $"Steam ID {RemotePlayerSteamId}";
			}
			PlayerInfo value = PlayerInfo.Value;
			if (value.SteamName == value.Name)
			{
				return value.SteamName;
			}
			return value.Name + " (" + value.SteamName + ")";
		}
	}

	private struct PlayerInfo
	{
		public readonly PlayerId PlayerId;

		public string Name;

		public readonly ulong SteamId;

		public readonly string SteamName;

		public Snapshot.CharacterCustomization Customization;

		public AccessLevel AccessLevel { get; set; }

		public PlayerInfo(PlayerId playerId, ulong steamId, string name, string steamName, AccessLevel accessLevel, Snapshot.CharacterCustomization customization)
		{
			PlayerId = playerId;
			SteamId = steamId;
			Name = name;
			SteamName = steamName;
			Customization = customization;
			AccessLevel = accessLevel;
		}
	}

	private readonly struct Routing
	{
		public enum Route
		{
			AllExcept,
			TrainCrew,
			Reject
		}

		public readonly Route route;

		public readonly string id;

		private Routing(Route route, string id)
		{
			this.route = route;
			this.id = id;
		}

		public static Routing Reject()
		{
			return new Routing(Route.Reject, null);
		}

		public static Routing AllExcept(string playerId)
		{
			return new Routing(Route.AllExcept, playerId);
		}

		public static Routing TrainCrew(string trainCrewId)
		{
			return new Routing(Route.TrainCrew, trainCrewId);
		}
	}

	private readonly HashSet<ClientId> _pendingRequestActive = new HashSet<ClientId>();

	private bool _hasLoadedSnapshot;

	private readonly Dictionary<ClientId, Client> _clients = new Dictionary<ClientId, Client>();

	private readonly List<Client> _sendToClients = new List<Client>();

	private readonly SendContext _sendContext = new SendContext();

	private UserData _cachedUserData;

	private PlayerId _cachedHostPlayerId;

	private Snapshot _snapshot = Snapshot.Empty();

	private Dictionary<PlayerId, PlayerRecord> _playerRecords = new Dictionary<PlayerId, PlayerRecord>();

	private readonly HashSet<Client> _queueForBannedDisconnect = new HashSet<Client>();

	private readonly NetworkBufferWriter _deepCopyBuffer = new NetworkBufferWriter();

	public const string HostOnlyKeyPrefix = "_";

	public static HostManager Shared { get; private set; }

	public Dictionary<string, Vector3[]> SnapshotCarBodyPositions { get; set; }

	private ulong MySteamId
	{
		get
		{
			if (!_cachedUserData.IsValid)
			{
				_cachedUserData = UserData.Me;
			}
			return _cachedUserData.SteamId;
		}
	}

	private PlayerId HostPlayerId
	{
		get
		{
			if (!_cachedHostPlayerId.IsValid)
			{
				_cachedHostPlayerId = new PlayerId(MySteamId);
			}
			return _cachedHostPlayerId;
		}
	}

	private int NumPassengersOnline => _playerRecords.Count((KeyValuePair<PlayerId, PlayerRecord> kv) => kv.Value.AccessLevel == AccessLevel.Passenger && IsOnline(kv.Key));

	public HostManager()
	{
		Shared = this;
	}

	public void Dispose()
	{
		_sendContext.Dispose();
		if (Shared == this)
		{
			Shared = null;
		}
	}

	private Client ClientForId(ClientId clientId)
	{
		if (!_clients.TryGetValue(clientId, out var value))
		{
			throw new ArgumentException($"No such clientId: {clientId}", "clientId");
		}
		return value;
	}

	private Client ClientForId(PlayerId playerId)
	{
		foreach (Client value in _clients.Values)
		{
			if (value.PlayerId.Equals(playerId))
			{
				return value;
			}
		}
		throw new ArgumentException($"No client for playerId: {playerId}", "playerId");
	}

	public bool ShouldAcceptConnection(ulong remotePlayerSteamId)
	{
		return true;
	}

	public void ClientDidConnect(IServerManager server, ClientId clientId, ulong remotePlayerSteamId)
	{
		Client value = new Client(clientId, server, remotePlayerSteamId);
		_clients[clientId] = value;
	}

	public void ClientDidDisconnect(IServerManager server, ClientId clientId)
	{
		_pendingRequestActive.Remove(clientId);
		if (_clients.TryGetValue(clientId, out var value))
		{
			_clients.Remove(clientId);
			if (value.PlayerId.IsValid)
			{
				PlayerDidDisconnect(value.PlayerId);
			}
		}
	}

	public void HandleMessage(IServerManager server, ClientId clientId, INetworkMessage message)
	{
		Client client = ClientForId(clientId);
		switch (client.Status)
		{
		case Network.Server.ClientStatus.Initial:
			HandleMessageInitial(message, client);
			break;
		case Network.Server.ClientStatus.Anonymous:
			HandleMessageAnonymous(message, client);
			break;
		case Network.Server.ClientStatus.Authenticated:
			HandleMessageAuthenticated(message, client);
			break;
		case Network.Server.ClientStatus.Active:
			HandleMessageActive(message, client);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		foreach (Client item in _queueForBannedDisconnect)
		{
			item.Disconnect(DisconnectReason.AccessDenied);
		}
		_queueForBannedDisconnect.Clear();
	}

	private void HandleMessageInitial(INetworkMessage message, Client client)
	{
		if (!(message is Hello hello))
		{
			if (message is Goodbye)
			{
				_ = (Goodbye)(object)message;
				client.Disconnect(DisconnectReason.Goodbye);
			}
			else
			{
				Log.Warning("Invalid message for client status: {messageName}", MessageName(message));
			}
		}
		else if (hello.MajorVersion != Common.MinimumVersion.Major || hello.MinorVersion < Common.MinimumVersion.Minor)
		{
			client.Disconnect(DisconnectReason.VersionMismatch, $"Outdated client. Server is running {Common.CurrentVersion.Major}.{Common.CurrentVersion.Minor}");
		}
		else
		{
			SetAndSendClientStatus(client, Network.Server.ClientStatus.Anonymous, AccessLevel.Undetermined);
			Log.Information("Client {client}: Running {major}.{minor}; now Anonymous", client, hello.MajorVersion, hello.MinorVersion);
		}
	}

	private void HandleMessageAnonymous(INetworkMessage message, Client client)
	{
		if (!(message is Login login))
		{
			if (message is Goodbye)
			{
				_ = (Goodbye)(object)message;
				client.Disconnect(DisconnectReason.Goodbye);
			}
			else
			{
				Log.Warning("Invalid message for client status: {message}", MessageName(message));
			}
			return;
		}
		if (!Authenticate(login.Password, client, out var accessLevel, out var disconnectReason))
		{
			if (disconnectReason == DisconnectReason.PasswordRequired)
			{
				SendToClient(client, default(PasswordPrompt));
			}
			else
			{
				client.Disconnect(disconnectReason);
			}
			return;
		}
		switch (accessLevel)
		{
		case AccessLevel.Banned:
			LogAuthDisconnect(client, "Access level is Banned.");
			client.Disconnect(DisconnectReason.AccessDenied);
			return;
		case AccessLevel.Passenger:
			if (NumPassengersOnline >= StateManager.Shared.Storage.PassengerLimit)
			{
				LogAuthDisconnect(client, $"Access level is Passenger and too many passengers are connected: {NumPassengersOnline} >= {StateManager.Shared.Storage.PassengerLimit}");
				client.Disconnect(DisconnectReason.NoMorePassengers);
				return;
			}
			break;
		}
		if (!ValidateUsername(ref login.Name, client.RemotePlayerSteamId))
		{
			LogAuthDisconnect(client, "Failed username validation: " + login.Name);
			client.Disconnect(DisconnectReason.AccessDenied);
			return;
		}
		UserData userData = UserData.Get(client.RemotePlayerSteamId);
		PlayerInfo value = new PlayerInfo(new PlayerId(client.RemotePlayerSteamId), client.RemotePlayerSteamId, login.Name, userData.Name, accessLevel, login.Customization);
		client.PlayerInfo = value;
		Log.Information("Client {clientId}: Authenticated as playerId=\"{playerId}\" name=\"{name}\", steamName = \"{steamName}\"", client.ClientId, value.PlayerId, value.Name, value.SteamName);
		SetAndSendClientStatus(client, Network.Server.ClientStatus.Authenticated, accessLevel);
	}

	private void HandlePendingRequestActive()
	{
		if (!_hasLoadedSnapshot || !_pendingRequestActive.Any())
		{
			return;
		}
		Log.Debug("Handling {count} pending active requests", _pendingRequestActive.Count);
		foreach (ClientId item in _pendingRequestActive)
		{
			Client client = ClientForId(item);
			AccessLevel accessLevel = client.PlayerInfo?.AccessLevel ?? AccessLevel.Undetermined;
			SetAndSendClientStatus(client, Network.Server.ClientStatus.Active, accessLevel);
			PostActivate(client.PlayerInfo.Value);
		}
		_pendingRequestActive.Clear();
	}

	private void HandleMessageAuthenticated(INetworkMessage message, Client client)
	{
		if (!(message is RequestActive))
		{
			if (message is Goodbye)
			{
				_ = (Goodbye)(object)message;
				client.Disconnect(DisconnectReason.Goodbye);
			}
			else
			{
				Log.Warning("Invalid message for client status: {message}", MessageName(message));
			}
		}
		else
		{
			_pendingRequestActive.Add(client.ClientId);
			HandlePendingRequestActive();
		}
	}

	private void HandleMessageActive(INetworkMessage message, Client client)
	{
		if (!(message is GameMessageEnvelope envelope))
		{
			if (message is TimeSync)
			{
				_ = (TimeSync)(object)message;
				SendToClient(client, new TimeSync(NetworkTime.systemTick));
			}
			else if (message is Goodbye)
			{
				_ = (Goodbye)(object)message;
				client.Disconnect(DisconnectReason.Goodbye);
			}
			else
			{
				Log.Warning("Invalid message for client status: {message}", MessageName(message));
			}
		}
		else
		{
			HandleGameMessage(client.PlayerId, envelope);
		}
	}

	private void SetAndSendClientStatus(Client client, Network.Server.ClientStatus status, AccessLevel accessLevel)
	{
		Log.Information("Client {client}: status = {status}", client, status);
		client.Status = status;
		SendToClient(client, new Network.Messages.ClientStatus(status, client.PlayerId.String, (int)accessLevel));
	}

	private static string MessageName(INetworkMessage message)
	{
		if (message is GameMessageEnvelope gameMessageEnvelope)
		{
			return $"GameMessage: {gameMessageEnvelope.gameMessage.GetType()}";
		}
		return message.GetType().ToString();
	}

	private void SendToClient(Client client, INetworkMessage message)
	{
		_sendToClients.Clear();
		_sendToClients.Add(client);
		SendToClients(message);
	}

	public void SendTo(PlayerId playerId, INetworkMessage message)
	{
		Client client = ClientForId(playerId);
		if (client.Status != Network.Server.ClientStatus.Active)
		{
			Log.Warning("HostManager won't send {message} to client with status {status}", MessageName(message), client.Status);
		}
		else
		{
			SendToClient(client, message);
		}
	}

	public void SendTo(HashSet<PlayerId> playerIds, INetworkMessage message)
	{
		_sendToClients.Clear();
		foreach (KeyValuePair<ClientId, Client> client in _clients)
		{
			Client value = client.Value;
			if (value.Status == Network.Server.ClientStatus.Active && playerIds.Contains(value.PlayerId))
			{
				_sendToClients.Add(value);
			}
		}
		SendToClients(message);
	}

	private void SendToClients(INetworkMessage message)
	{
		_sendContext.ClearRecipients();
		foreach (Client sendToClient in _sendToClients)
		{
			if (sendToClient.Server is SteamServer steamServer)
			{
				if (steamServer.TryGetClientConnection(sendToClient.ClientId, out var connection))
				{
					_sendContext.AddRecipient(connection);
				}
				else
				{
					Log.Error("Missing connection for client {client}", sendToClient);
				}
			}
			else
			{
				sendToClient.Server.SendTo(sendToClient.ClientId, message);
			}
		}
		if (_sendContext.Send(message))
		{
			return;
		}
		foreach (var (propertyValue, eResult) in _sendContext.ErroredRecipients())
		{
			Log.Error("Send error for {connection}: {result}", propertyValue, eResult);
			ClientId other = new ClientId(propertyValue.m_HSteamNetConnection);
			foreach (Client value in _clients.Values)
			{
				if (value.ClientId.Equals(other))
				{
					value.Disconnect(DisconnectReason.HostClosedConnection, $"Error on Send: {eResult}");
					break;
				}
			}
		}
	}

	public void SendToAll(INetworkMessage message)
	{
		SendToAllExcept(message, null);
	}

	private void SendToAllExcept(INetworkMessage message, PlayerId? exceptPlayerId)
	{
		_sendToClients.Clear();
		foreach (var (_, client2) in _clients)
		{
			if ((!exceptPlayerId.HasValue || !client2.PlayerId.Equals(exceptPlayerId.Value)) && client2.Status == Network.Server.ClientStatus.Active)
			{
				_sendToClients.Add(client2);
			}
		}
		SendToClients(message);
	}

	private void SendToAccessLevelAndUp(AccessLevel minAccessLevel, INetworkMessage message)
	{
		_sendToClients.Clear();
		foreach (var (_, client2) in _clients)
		{
			if (client2.PlayerInfo.HasValue && client2.PlayerInfo.Value.AccessLevel >= minAccessLevel && client2.Status == Network.Server.ClientStatus.Active)
			{
				_sendToClients.Add(client2);
			}
		}
		SendToClients(message);
	}

	private bool Authenticate(string password, Client client, out AccessLevel accessLevel, out DisconnectReason disconnectReason)
	{
		disconnectReason = DisconnectReason.Invalid;
		PlayerId playerId = new PlayerId(client.RemotePlayerSteamId);
		if (playerId == HostPlayerId)
		{
			accessLevel = AccessLevel.President;
			return true;
		}
		GameStorage storage = StateManager.Shared.Storage;
		if (_playerRecords.TryGetValue(playerId, out var value))
		{
			accessLevel = value.AccessLevel;
			return true;
		}
		if (storage.AllowNewPlayers)
		{
			if (string.IsNullOrEmpty(storage.NewPlayerPasswordHash))
			{
				accessLevel = storage.DefaultAccessLevel;
				return true;
			}
			if (storage.CheckNewPlayerPassword(password))
			{
				accessLevel = storage.DefaultAccessLevel;
				return true;
			}
			accessLevel = AccessLevel.Undetermined;
			if (string.IsNullOrEmpty(password))
			{
				LogAuthDisconnect(client, "Password is required, no password supplied.");
				disconnectReason = DisconnectReason.PasswordRequired;
			}
			else
			{
				LogAuthDisconnect(client, "Password is required, does not match.");
				disconnectReason = DisconnectReason.PasswordRequired;
			}
			return false;
		}
		LogAuthDisconnect(client, "Player unknown; not accepting new players.");
		accessLevel = AccessLevel.Undetermined;
		disconnectReason = DisconnectReason.AccessDenied;
		return false;
	}

	private void LogAuthDisconnect(Client client, string message)
	{
		if (Preferences.HostAuthLogging)
		{
			string text = client.NameForLog();
			Console.Log("Client Auth Failed for <noparse>" + text + "</noparse>: " + message);
		}
		Log.Warning("Will disconnect client for auth failure {playerInfo}: {message}", client.PlayerInfo, message);
	}

	private bool IsOnline(PlayerId playerId)
	{
		return _snapshot.players.ContainsKey(playerId.String);
	}

	private bool ValidateUsername(ref string username, ulong remotePlayerSteamId)
	{
		username = StringSanitizer.SanitizeName(username);
		if (string.IsNullOrWhiteSpace(username))
		{
			Log.Error("ValidateUsername would fail: '{username}' is null or whitespace", username);
			return true;
		}
		foreach (var (_, playerRecord2) in _playerRecords)
		{
			if (playerRecord2.SteamId != remotePlayerSteamId && playerRecord2.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase))
			{
				Log.Error("ValidateUsername would fail: '{username}' matches record '{other}' but Steam IDs differ: {yours} vs {theirs}", username, playerRecord2.Name, remotePlayerSteamId, playerRecord2.SteamId);
				return true;
			}
		}
		return true;
	}

	private void PostActivate(PlayerInfo playerInfo)
	{
		PlayerId playerId = playerInfo.PlayerId;
		CharacterPosition position = InitialCharacterPosition(playerId);
		Snapshot.Player value = new Snapshot.Player(playerInfo.Name, playerInfo.AccessLevel, playerInfo.Customization, position);
		_snapshot.players[playerId.String] = value;
		UpdatePlayerRecord(playerId, delegate(PlayerRecord record)
		{
			record.Name = playerInfo.Name;
			record.LastConnected = DateTime.Now;
			record.SteamId = playerInfo.SteamId;
			if (record.AccessLevel != playerInfo.AccessLevel)
			{
				record.AccessLevel = playerInfo.AccessLevel;
				record.AccessLevelChanged = DateTime.Now;
			}
			return record;
		});
		SendSnapshotTo(playerId);
		SendPlayerList();
		SendPlayerRecords();
		SendTo(playerId, new SetPlayerPosition(position));
	}

	private CharacterPosition InitialCharacterPosition(PlayerId playerId)
	{
		if (_playerRecords.TryGetValue(playerId, out var value) && IsValidPosition(value.Position))
		{
			return value.Position;
		}
		PositionRotation defaultSpawn = CameraSelector.shared.DefaultSpawn;
		return new CharacterPosition(defaultSpawn.Position, null, defaultSpawn.Rotation * Vector3.forward, defaultSpawn.Rotation * Vector3.forward);
	}

	private static bool IsValidPosition(CharacterPosition pos)
	{
		if (!StateManager.Shared.HasRestoredProperties)
		{
			return true;
		}
		if (!string.IsNullOrEmpty(pos.RelativeToCarId))
		{
			return TrainController.Shared.CarForId(pos.RelativeToCarId) != null;
		}
		return true;
	}

	private void PlayerDidDisconnect(PlayerId playerId)
	{
		_snapshot.players.Remove(playerId.String);
		SendPlayerList();
	}

	private void SendSnapshotTo(PlayerId playerId)
	{
		Snapshot.Map map = _snapshot.map;
		GameDateTime now = TimeWeather.Now;
		map.TimeOfDay = now.Hours;
		map.Day = now.Day;
		_snapshot.map = map;
		TrainController shared = TrainController.Shared;
		if (shared.Cars.Count > 0)
		{
			_snapshot.Version = 1;
			_snapshot.Cars.Clear();
			foreach (Car car in shared.Cars)
			{
				_snapshot.Cars[car.id] = car.Snapshot();
			}
		}
		Log.Debug("Sending Snapshot");
		SendTo(playerId, new SnapshotEnvelope(DeepCopy(_snapshot)));
	}

	private void SendPlayerList()
	{
		PlayerList playerList = new PlayerList(_snapshot.players);
		SendToAll(playerList);
	}

	private void SendPlayerRecords()
	{
		string sender = PlayersManager.PlayerId.String;
		SendToAccessLevelAndUp(AccessLevel.Trainmaster, new GameMessageEnvelope(sender, new PlayerRecords(PlayerRecordsForSave())));
	}

	public void HandleGameMessage(PlayerId playerId, GameMessageEnvelope envelope)
	{
		envelope.sender = playerId.String;
		Routing routing = RoutingForMessage(playerId, envelope);
		if (routing.route == Routing.Route.Reject)
		{
			StateManager.Shared.HostRejectMessage(playerId, envelope.gameMessage);
			return;
		}
		RecordState(envelope);
		switch (routing.route)
		{
		case Routing.Route.AllExcept:
			SendToAllExcept(envelope, new PlayerId(routing.id));
			break;
		case Routing.Route.TrainCrew:
			SendTo(TrainCrewPlayerIds(routing.id), envelope);
			break;
		}
	}

	public void LoadSnapshot(Snapshot snapshot, Dictionary<string, PlayerRecord> playerRecords, Dictionary<string, Vector3[]> carBodyPositions)
	{
		snapshot.players = _snapshot.players;
		_snapshot = snapshot;
		ref Dictionary<string, SwitchList> switchLists = ref _snapshot.SwitchLists;
		if (switchLists == null)
		{
			switchLists = new Dictionary<string, SwitchList>();
		}
		ref Dictionary<string, Snapshot.TrainCrew> trainCrews = ref _snapshot.TrainCrews;
		if (trainCrews == null)
		{
			trainCrews = new Dictionary<string, Snapshot.TrainCrew>();
		}
		_playerRecords = playerRecords.ToDictionary((KeyValuePair<string, PlayerRecord> kv) => new PlayerId(kv.Key), (KeyValuePair<string, PlayerRecord> kv) => kv.Value);
		StateManager.Shared.ApplySnapshotMap(_snapshot.map);
		SnapshotCarBodyPositions = carBodyPositions ?? new Dictionary<string, Vector3[]>();
		SendToAll(new SnapshotEnvelope(DeepCopy(snapshot)));
		_hasLoadedSnapshot = true;
		HandlePendingRequestActive();
	}

	private TMessage DeepCopy<TMessage>(TMessage input)
	{
		_deepCopyBuffer.Clear();
		MessagePackSerializer.Serialize(_deepCopyBuffer, input);
		TMessage result = MessagePackSerializer.Deserialize<TMessage>(new ReadOnlyMemory<byte>(_deepCopyBuffer.Array, 0, _deepCopyBuffer.ArrayLength));
		_deepCopyBuffer.Reset();
		return result;
	}

	public Dictionary<string, PlayerRecord> PlayerRecordsForSave()
	{
		return _playerRecords.ToDictionary((KeyValuePair<PlayerId, PlayerRecord> kv) => kv.Key.String, (KeyValuePair<PlayerId, PlayerRecord> kv) => kv.Value);
	}

	private HashSet<PlayerId> TrainCrewPlayerIds(string trainCrewId)
	{
		if (_snapshot.TrainCrews.TryGetValue(trainCrewId, out var value))
		{
			return value.MemberPlayerIds.Select((string id) => new PlayerId(id)).ToHashSet();
		}
		Log.Warning("Unknown train crew: {trainCrewId}", trainCrewId);
		return new HashSet<PlayerId>();
	}

	private AccessLevel AccessLevelForPlayerId(PlayerId playerId)
	{
		if (playerId == new PlayerId(MySteamId))
		{
			return AccessLevel.President;
		}
		if (_playerRecords.TryGetValue(playerId, out var value))
		{
			return value.AccessLevel;
		}
		Log.Error("Failed to find access level for player {playerId}", playerId);
		return AccessLevel.Undetermined;
	}

	public static bool CheckAuthorizedToSendMessage(IGameMessage message, PlayerId senderPlayerId, AccessLevel senderAccessLevel)
	{
		if (message is Transaction transaction)
		{
			foreach (IGameMessage message2 in transaction.Messages)
			{
				if (!CheckAuthorizedToSendMessage(message2, senderPlayerId, senderAccessLevel))
				{
					return false;
				}
			}
			return true;
		}
		object[] customAttributes = message.GetType().GetCustomAttributes(typeof(IMessageAuthorizationRuleAttribute), inherit: true);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			if (!(customAttributes[i] as IMessageAuthorizationRuleAttribute).CheckAuthorization(senderPlayerId, senderAccessLevel, message))
			{
				return false;
			}
		}
		return true;
	}

	private Routing RoutingForMessage(PlayerId senderPlayerId, GameMessageEnvelope envelope)
	{
		AccessLevel senderAccessLevel = AccessLevelForPlayerId(senderPlayerId);
		if (!CheckAuthorizedToSendMessage(envelope.gameMessage, senderPlayerId, senderAccessLevel))
		{
			Log.Warning("Reject message {message}; authorization check failed: {senderPlayerId}", envelope.gameMessage.GetType(), senderPlayerId);
			return Routing.Reject();
		}
		Routing result = Routing.AllExcept(senderPlayerId.String);
		IGameMessage gameMessage = envelope.gameMessage;
		if (gameMessage is SwitchListUpdate)
		{
			return Routing.TrainCrew(((SwitchListUpdate)(object)gameMessage).TrainCrewId);
		}
		return result;
	}

	private void RecordState(GameMessageEnvelope envelope)
	{
		IGameMessage gameMessage = envelope.gameMessage;
		if (!(gameMessage is ICarMessage message))
		{
			if (!(gameMessage is ICharacterMessage message2))
			{
				if (gameMessage is AddCars addCars)
				{
					{
						foreach (Snapshot.Car car in addCars.Cars)
						{
							_snapshot.Cars[car.id] = car;
						}
						return;
					}
				}
				if (gameMessage is RemoveCars removeCars)
				{
					{
						foreach (string carId in removeCars.CarIds)
						{
							_snapshot.Cars.Remove(carId);
							for (int num = _snapshot.CarAir.Count - 1; num >= 0; num--)
							{
								if (_snapshot.CarAir[num].CarIds.Contains(carId))
								{
									_snapshot.CarAir.RemoveAt(num);
								}
							}
							_snapshot.Properties.Remove(carId);
						}
						return;
					}
				}
				if (!(gameMessage is CarSetAdd carSetAdd))
				{
					if (!(gameMessage is CarSetRemove carSetRemove))
					{
						if (!(gameMessage is CarSetChangeCars { Set: var set }))
						{
							if (!(gameMessage is BatchCarPositionUpdate update))
							{
								if (!(gameMessage is BatchCarAirUpdate update2))
								{
									if (!(gameMessage is SetSwitch setSwitch))
									{
										if (!(gameMessage is PropertyChange propertyChange))
										{
											if (!(gameMessage is UpdateTrainCrews updateTrainCrews))
											{
												Snapshot.Car value2;
												if (!(gameMessage is SetCarTrainCrew setCarTrainCrew))
												{
													Snapshot.Car value;
													if (!(gameMessage is CarSetBardo carSetBardo))
													{
														if (!(gameMessage is SwitchListUpdate switchListUpdate))
														{
															if (!(gameMessage is TurntableUpdateStopIndex turntableUpdateStopIndex))
															{
																if (!(gameMessage is Transaction transaction))
																{
																	return;
																}
																{
																	foreach (IGameMessage message3 in transaction.Messages)
																	{
																		RecordState(new GameMessageEnvelope(envelope.sender, message3));
																	}
																	return;
																}
															}
															_snapshot.Turntables[turntableUpdateStopIndex.TurntableId] = new Snapshot.TurntableState(turntableUpdateStopIndex.Angle, turntableUpdateStopIndex.StopIndex);
														}
														else
														{
															_snapshot.SwitchLists[switchListUpdate.TrainCrewId] = switchListUpdate.SwitchList;
														}
													}
													else if (_snapshot.Cars.TryGetValue(carSetBardo.CarId, out value))
													{
														value.Bardo = carSetBardo.Bardo;
														_snapshot.Cars[carSetBardo.CarId] = value;
													}
												}
												else if (_snapshot.Cars.TryGetValue(setCarTrainCrew.CarId, out value2))
												{
													value2.TrainCrewId = setCarTrainCrew.TrainCrewId;
													_snapshot.Cars[setCarTrainCrew.CarId] = value2;
												}
											}
											else
											{
												_snapshot.TrainCrews = updateTrainCrews.TrainCrews;
											}
										}
										else
										{
											SetSnapshotProperty(propertyChange.ObjectId, propertyChange.Key, propertyChange.Value);
										}
									}
									else if (setSwitch.Thrown)
									{
										_snapshot.thrownSwitchIds.Add(setSwitch.NodeId);
									}
									else
									{
										_snapshot.thrownSwitchIds.Remove(setSwitch.NodeId);
									}
								}
								else
								{
									RecordBatchCarAirUpdate(update2);
								}
							}
							else
							{
								RecordBatchCarPositionUpdate(update);
							}
						}
						else
						{
							Log.Debug("CarSetChangeCars {id}", set.Id);
							uint id = set.Id;
							if (!_snapshot.CarSets.ContainsKey(id))
							{
								Log.Error("RecordState CarSetChangeCars: No such set {carSetId}", id);
								return;
							}
							_snapshot.CarSets[id] = set;
							UpdateSnapshotCarsUsingCarSet(set);
						}
					}
					else
					{
						Log.Debug("CarSetRemove {id}", carSetRemove.SetId);
						if (!_snapshot.CarSets.ContainsKey(carSetRemove.SetId))
						{
							Log.Error("RecordState CarSetRemove: No such set {carSetId}", carSetRemove.SetId);
						}
						_snapshot.CarSets.Remove(carSetRemove.SetId);
					}
				}
				else
				{
					Log.Debug("CarSetAdd {id}", carSetAdd.Set.Id);
					if (_snapshot.CarSets.ContainsKey(carSetAdd.Set.Id))
					{
						Log.Error("RecordState CarSetAdd: Already contains set {carSetId}", carSetAdd.Set.Id);
					}
					_snapshot.CarSets[carSetAdd.Set.Id] = carSetAdd.Set;
					UpdateSnapshotCarsUsingCarSet(carSetAdd.Set);
				}
			}
			else
			{
				RecordState(envelope.sender, message2);
			}
		}
		else
		{
			RecordState(envelope.sender, message);
		}
	}

	public void SetSnapshotProperty(string objectId, string key, IPropertyValue value)
	{
		if (!_snapshot.Properties.TryGetValue(objectId, out var value2))
		{
			value2 = new Dictionary<string, IPropertyValue>();
		}
		value2[key] = value;
		_snapshot.Properties[objectId] = value2;
	}

	private void UpdateSnapshotCarsUsingCarSet(Snapshot.CarSet set)
	{
		for (int i = 0; i < set.CarIds.Count; i++)
		{
			string key = set.CarIds[i];
			Snapshot.Car value = _snapshot.Cars[key];
			value.FrontIsA = set.FrontIsAs[i];
			_snapshot.Cars[key] = value;
		}
	}

	private void RecordBatchCarPositionUpdate(BatchCarPositionUpdate update)
	{
		uint id = update.Id;
		if (!_snapshot.CarSets.TryGetValue(id, out var value))
		{
			Log.Error($"CarSet not found: {id}");
			return;
		}
		if (value.CarIds.Count != update.Positions.Length || value.CarIds.Count != update.Velocities.Length)
		{
			throw new Exception($"Snapshot contains {value.CarIds.Count} car ids but update has {update.Positions.Length}, {update.Velocities.Length}");
		}
		TrainController shared = TrainController.Shared;
		for (int i = 0; i < value.CarIds.Count; i++)
		{
			string text = value.CarIds[i];
			if (_snapshot.Cars.TryGetValue(text, out var value2))
			{
				if (shared.TryGetCarForId(text, out var car))
				{
					value2.Location = Graph.CreateSnapshotTrackLocation(car.SnapshotLocation);
					value2.FrontIsA = car.FrontIsA;
				}
				value2.velocity = Mathf.HalfToFloat(update.Velocities[i]);
				_snapshot.Cars[text] = value2;
			}
		}
		value.Positions = update.Positions.ToList();
		_snapshot.CarSets[id] = value;
	}

	private void RecordBatchCarAirUpdate(BatchCarAirUpdate update)
	{
		for (int num = _snapshot.CarAir.Count - 1; num >= 0; num--)
		{
			if (Overlaps(_snapshot.CarAir[num].CarIds, update.CarIds))
			{
				_snapshot.CarAir.RemoveAt(num);
			}
		}
		_snapshot.CarAir.Add(update);
	}

	private static bool Overlaps(string[] idsA, string[] idsB)
	{
		return idsA.Any(((IEnumerable<string>)idsB).Contains<string>);
	}

	private void RecordState(string senderId, ICarMessage message)
	{
	}

	private void RecordState(string senderId, ICharacterMessage message)
	{
		Snapshot.Player value = _snapshot.players[senderId];
		PlayerId playerId = new PlayerId(senderId);
		if (message is AddUpdateCharacter)
		{
			AddUpdateCharacter addUpdateCharacter = (AddUpdateCharacter)(object)message;
			addUpdateCharacter.Name = StringSanitizer.SanitizeName(addUpdateCharacter.Name);
			if (string.IsNullOrEmpty(addUpdateCharacter.Name))
			{
				throw new Exception("Name is empty");
			}
			value.Name = addUpdateCharacter.Name;
			value.Customization = addUpdateCharacter.Customization;
			UpdatePlayerRecord(playerId, delegate(PlayerRecord state)
			{
				state.Name = addUpdateCharacter.Name;
				return state;
			});
		}
		else if (message is UpdateCharacterPosition)
		{
			UpdateCharacterPosition updateCharacterPosition = (UpdateCharacterPosition)(object)message;
			value.Position = updateCharacterPosition.Position;
			UpdatePlayerRecord(playerId, delegate(PlayerRecord state)
			{
				state.Position = updateCharacterPosition.Position;
				return state;
			});
		}
		_snapshot.players[senderId] = value;
	}

	private void UpdatePlayerRecord(PlayerId playerId, Func<PlayerRecord, PlayerRecord> action)
	{
		if (!_playerRecords.TryGetValue(playerId, out var value))
		{
			value = default(PlayerRecord);
		}
		PlayerRecord value2 = action(value);
		value2.Updated = DateTime.Now;
		_playerRecords[playerId] = value2;
	}

	public void SetAccessLevel(PlayerId playerId, AccessLevel targetAccessLevel, IPlayer sender)
	{
		StateManager.AssertIsHost();
		if (HostPlayerId == playerId)
		{
			Log.Warning("Ignore SetAccessLevel for host");
			Multiplayer.SendError(sender, "Can't set host's access level.");
			return;
		}
		if (!_playerRecords.TryGetValue(playerId, out var value))
		{
			throw new Exception("Record not found");
		}
		if (value.AccessLevel == targetAccessLevel)
		{
			return;
		}
		AccessLevel accessLevel = value.AccessLevel;
		value.AccessLevel = targetAccessLevel;
		value.AccessLevelChanged = DateTime.Now;
		_playerRecords[playerId] = value;
		AnnounceAccessChange(sender, value, accessLevel, targetAccessLevel);
		foreach (ClientId key in _clients.Keys)
		{
			Client client = _clients[key];
			if (client.Status != Network.Server.ClientStatus.Active)
			{
				continue;
			}
			ref PlayerInfo? playerInfo = ref client.PlayerInfo;
			if (playerInfo.HasValue && !(playerInfo.GetValueOrDefault().PlayerId != playerId))
			{
				PlayerInfo value2 = client.PlayerInfo.Value;
				value2.AccessLevel = targetAccessLevel;
				client.PlayerInfo = value2;
				SetAndSendClientStatus(client, Network.Server.ClientStatus.Active, targetAccessLevel);
				if (targetAccessLevel == AccessLevel.Banned)
				{
					Log.Information("Queuing client for disconnect, new access level for {playerId} is Banned.", playerId);
					_queueForBannedDisconnect.Add(client);
				}
			}
		}
		SendPlayerRecords();
	}

	private static void AnnounceAccessChange(IPlayer sender, PlayerRecord record, AccessLevel oldAccessLevel, AccessLevel targetAccessLevel)
	{
		string name = record.Name;
		string message = ((targetAccessLevel == AccessLevel.Banned) ? $"{Hyperlink.To(sender)} has banned {name}." : ((targetAccessLevel <= oldAccessLevel) ? $"{Hyperlink.To(sender)} has demoted {name} to {targetAccessLevel}." : $"{Hyperlink.To(sender)} has promoted {name} to {targetAccessLevel}."));
		Multiplayer.Broadcast(message);
	}

	public void RemovePlayerRecord(PlayerId playerId, IPlayer sender)
	{
		if (!_playerRecords.TryGetValue(playerId, out var _))
		{
			throw new Exception("Record not found");
		}
		if (IsOnline(playerId))
		{
			Multiplayer.SendError(sender, "Can't remove online player record.");
			return;
		}
		_playerRecords.Remove(playerId);
		SendPlayerRecords();
	}
}
