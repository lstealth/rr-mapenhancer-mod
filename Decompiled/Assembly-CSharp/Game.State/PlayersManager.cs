using System;
using System.Collections.Generic;
using System.Linq;
using Cameras;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.Notices;
using Game.Persistence;
using HeathenEngineering.SteamworksIntegration.API;
using Helpers;
using JetBrains.Annotations;
using Model.Ops;
using Network;
using Network.Client;
using Serilog;
using UnityEngine;

namespace Game.State;

public class PlayersManager : MonoBehaviour
{
	private enum HandleSnapshotPlayersContext
	{
		Restore,
		PlayerList
	}

	private struct PlayerCameraPosition
	{
		public readonly Vector3 Position;

		public readonly float Time;

		public PlayerCameraPosition(Vector3 position, float time)
		{
			Position = position;
			Time = time;
		}
	}

	private readonly Dictionary<PlayerId, RemotePlayer> _remotePlayers = new Dictionary<PlayerId, RemotePlayer>();

	private readonly Dictionary<PlayerId, PlayerCameraPosition> _lastKnownPositions = new Dictionary<PlayerId, PlayerCameraPosition>();

	private Dictionary<string, TrainCrew> _trainCrews = new Dictionary<string, TrainCrew>();

	private List<TrainCrew> _orderedTrainCrews = new List<TrainCrew>();

	private readonly LocalPlayer _localPlayer;

	private bool _hasNotifiedOfPlayers;

	private static PlayerId? _cachedPlayerId;

	private readonly Dictionary<PlayerId, AccessLevel> _cachedAccessLevels = new Dictionary<PlayerId, AccessLevel>();

	public IEnumerable<RemotePlayer> RemotePlayers => _remotePlayers.Values;

	public IReadOnlyList<TrainCrew> TrainCrews => _orderedTrainCrews;

	public static PlayerId PlayerId
	{
		get
		{
			PlayerId valueOrDefault = _cachedPlayerId.GetValueOrDefault();
			if (!_cachedPlayerId.HasValue)
			{
				valueOrDefault = new PlayerId(User.Client.Id);
				_cachedPlayerId = valueOrDefault;
			}
			return _cachedPlayerId.Value;
		}
	}

	public IEnumerable<IPlayer> AllPlayers
	{
		get
		{
			yield return LocalPlayer;
			if (!(Multiplayer.Client != null))
			{
				yield break;
			}
			foreach (RemotePlayer value in _remotePlayers.Values)
			{
				yield return value;
			}
		}
	}

	public LocalPlayer LocalPlayer => _localPlayer;

	[CanBeNull]
	public TrainCrew MyTrainCrew => TrainCrews.FirstOrDefault((TrainCrew crew) => crew.MemberPlayerIds.Contains(PlayerId));

	private void OnDestroy()
	{
		ClearRemotePlayers();
	}

	public void OnClientCreated(ClientManager client)
	{
		client.OnRemotePlayersDidChange += OnRemotePlayersDidChange;
	}

	public void OnWillUnloadMap()
	{
		ClearRemotePlayers();
		ClientManager client = Multiplayer.Client;
		if (client != null)
		{
			client.OnRemotePlayersDidChange -= OnRemotePlayersDidChange;
		}
	}

	private void OnRemotePlayersDidChange(Dictionary<string, Snapshot.Player> players)
	{
		_ = Multiplayer.Client;
		HandleSnapshotPlayers(players, HandleSnapshotPlayersContext.PlayerList);
	}

	public IPlayer PlayerForId(PlayerId playerId)
	{
		return AllPlayers.FirstOrDefault((IPlayer p) => p.PlayerId == playerId);
	}

	public string NameForPlayerId(PlayerId playerId)
	{
		foreach (IPlayer allPlayer in AllPlayers)
		{
			if (allPlayer.PlayerId == playerId)
			{
				return allPlayer.Name;
			}
		}
		PlayerRecordsClientManager playerRecordsClientManager = StateManager.Shared.PlayerRecordsClientManager;
		if (playerRecordsClientManager != null)
		{
			foreach (var (playerId3, playerRecord2) in playerRecordsClientManager.PlayerRecords)
			{
				if (playerId3 == playerId)
				{
					return playerRecord2.Name;
				}
			}
		}
		Log.Warning("Couldn't find player name for {playerId}", playerId);
		return "Unknown";
	}

	public void RestoreFromSnapshot(Dictionary<string, Snapshot.Player> players, Dictionary<string, Snapshot.TrainCrew> trainCrews)
	{
		HandleSnapshotPlayers(players, HandleSnapshotPlayersContext.Restore);
		SetTrainCrews(trainCrews);
	}

	private void HandleSnapshotPlayers(Dictionary<string, Snapshot.Player> players, HandleSnapshotPlayersContext context)
	{
		Log.Information("HandleSnapshotPlayers: {@players}", players.Select((KeyValuePair<string, Snapshot.Player> p) => new
		{
			PlayerId = p.Key,
			Name = p.Value.Name
		}));
		SplitSnapshotPlayers(players, out var disconnectedIds, out var connectedPlayers);
		NotifyOfDisconnected(disconnectedIds);
		ClearRemotePlayers();
		_cachedAccessLevels.Clear();
		foreach (KeyValuePair<string, Snapshot.Player> player2 in players)
		{
			player2.Deconstruct(out var key, out var value);
			string playerId = key;
			Snapshot.Player player = value;
			PlayerId playerId2 = new PlayerId(playerId);
			_cachedAccessLevels[playerId2] = player.AccessLevel;
			if (playerId2 == PlayerId)
			{
				if (context == HandleSnapshotPlayersContext.Restore)
				{
					RestoreCharacterPosition(player);
				}
				continue;
			}
			try
			{
				RemotePlayer remotePlayer = CreateRemotePlayer(playerId2, player.Name);
				CharacterPosition position = player.Position;
				remotePlayer.ConfigureAvatar(position.Position, position.RelativeToCarId, position.Forward, position.Look, player.Customization);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error handling snapshot player {playerId} {name}", playerId2, base.name);
			}
		}
		NotifyOfConnected(connectedPlayers);
		Messenger.Default.Send(default(PlayersDidChange));
	}

	private void RestoreCharacterPosition(Snapshot.Player player)
	{
		Log.Debug("RestoreCharacterPosition: {player}", player);
		CharacterPosition position = player.Position;
		if (position.Position == Vector3.zero)
		{
			return;
		}
		CameraSelector shared = CameraSelector.shared;
		if (!string.IsNullOrEmpty(position.RelativeToCarId))
		{
			if (!TrainController.Shared.TryGetCarForId(position.RelativeToCarId, out var car))
			{
				Log.Warning("Failed to restore position, car not found: {carId}", position.RelativeToCarId);
				return;
			}
			shared.JumpToCar(car, position.Position, Quaternion.LookRotation(position.Look));
			shared.ZoomToCar(car, select: false);
		}
		else
		{
			shared.JumpCharacterTo(new JumpTarget(position.Position, Quaternion.LookRotation(position.Look)));
			shared.MoveStrategyToPoint(position.Position.GameToWorld(), Quaternion.LookRotation(position.Look));
		}
	}

	private void SplitSnapshotPlayers(Dictionary<string, Snapshot.Player> players, out HashSet<PlayerId> disconnectedIds, out Dictionary<PlayerId, Snapshot.Player> connectedPlayers)
	{
		disconnectedIds = new HashSet<PlayerId>(_remotePlayers.Keys);
		connectedPlayers = new Dictionary<PlayerId, Snapshot.Player>();
		foreach (KeyValuePair<string, Snapshot.Player> player in players)
		{
			PlayerId playerId = new PlayerId(player.Key);
			disconnectedIds.Remove(playerId);
			if (!_remotePlayers.ContainsKey(playerId) && playerId != PlayerId)
			{
				Snapshot.Player value = player.Value;
				connectedPlayers[playerId] = value;
			}
		}
	}

	private void NotifyOfConnected(Dictionary<PlayerId, Snapshot.Player> connectedPlayers)
	{
		if (!connectedPlayers.Any())
		{
			return;
		}
		Log.Information("Connected: {players}", connectedPlayers);
		string text = string.Join(", ", from p in connectedPlayers
			orderby p.Value.Name
			select Hyperlink.To(new EntityReference(p.Key)));
		bool flag = connectedPlayers.Count != 1;
		if (_hasNotifiedOfPlayers || StateManager.IsHost)
		{
			Console.Log($"{DateTime.Now:t} {text} has connected.");
		}
		else
		{
			Console.Log(text + " " + (flag ? "are" : "is") + " connected.");
		}
		_hasNotifiedOfPlayers = true;
		NoticeManager shared = NoticeManager.Shared;
		foreach (var (playerId2, _) in connectedPlayers)
		{
			shared.PostEphemeralLocal(new EntityReference(playerId2), "conn", "Connected");
		}
	}

	private void NotifyOfDisconnected(HashSet<PlayerId> disconnectedIds)
	{
		NoticeManager shared = NoticeManager.Shared;
		foreach (PlayerId disconnectedId in disconnectedIds)
		{
			if (!_remotePlayers.TryGetValue(disconnectedId, out var value))
			{
				Log.Error("Couldn't find name of disconnected player {playerId}", disconnectedId);
				continue;
			}
			Log.Information("{name} {playerId} has disconnected", value.playerName, value.playerId);
			Console.Log($"{DateTime.Now:t} {Hyperlink.To(value)} has disconnected.");
			shared.PostEphemeralLocal(new EntityReference(value.playerId), "conn", "Disconnected");
		}
	}

	private RemotePlayer CreateRemotePlayer(PlayerId playerId, string playerName)
	{
		Log.Verbose("CreateRemotePlayer {playerId} {playerName}", playerId, playerName);
		GameObject obj = new GameObject("RemotePlayer " + playerId);
		obj.hideFlags = HideFlags.DontSave;
		obj.transform.SetParent(base.transform);
		RemotePlayer remotePlayer = obj.AddComponent<RemotePlayer>();
		remotePlayer.playerId = playerId;
		remotePlayer.playerName = playerName;
		_remotePlayers[playerId] = remotePlayer;
		return remotePlayer;
	}

	private void ClearRemotePlayers()
	{
		Log.Verbose("ClearRemotePlayers");
		foreach (KeyValuePair<PlayerId, RemotePlayer> remotePlayer in _remotePlayers)
		{
			UnityEngine.Object.Destroy(remotePlayer.Value.gameObject);
		}
		_remotePlayers.Clear();
		_lastKnownPositions.Clear();
	}

	public bool TrainCrewForId(string trainCrewId, out TrainCrew trainCrew)
	{
		if (string.IsNullOrEmpty(trainCrewId))
		{
			trainCrew = null;
			return false;
		}
		return _trainCrews.TryGetValue(trainCrewId, out trainCrew);
	}

	public string NameForTrainCrewId(string trainCrewId)
	{
		if (!TrainCrewForId(trainCrewId, out var trainCrew))
		{
			return "<Unknown>";
		}
		return trainCrew.Name;
	}

	public string TrainCrewIdFor(PlayerId playerId)
	{
		foreach (var (result, trainCrew2) in _trainCrews)
		{
			if (trainCrew2.MemberPlayerIds.Contains(playerId))
			{
				return result;
			}
		}
		return null;
	}

	public void HandleRequestTrainCrewMembership(PlayerId playerId, string trainCrewId, bool join)
	{
		StateManager.AssertIsHost();
		string text = null;
		TrainCrew value2;
		if (join)
		{
			if (trainCrewId != null && _trainCrews.TryGetValue(trainCrewId, out var value))
			{
				value.MemberPlayerIds.Add(playerId);
				text = trainCrewId;
				Multiplayer.Broadcast($"{Hyperlink.To(playerId)} joined train crew \"{value.Name}\"");
			}
			foreach (TrainCrew item in _trainCrews.Values.Where((TrainCrew tc) => tc.Id != trainCrewId))
			{
				item.MemberPlayerIds.Remove(playerId);
			}
		}
		else if (trainCrewId != null && _trainCrews.TryGetValue(trainCrewId, out value2))
		{
			value2.MemberPlayerIds.Remove(playerId);
			Multiplayer.Broadcast($"{Hyperlink.To(playerId)} left train crew \"{value2.Name}\"");
		}
		StateManager.ApplyLocal(new UpdateTrainCrews(TrainCrewsSnapshot()));
		if (text != null)
		{
			OpsController.Shared.SwitchListController.SendSwitchListUpdate(text);
		}
	}

	public void HandleUpdateTrainCrews(Dictionary<string, Snapshot.TrainCrew> trainCrews)
	{
		SetTrainCrews(trainCrews);
	}

	public void HandleRequestCreateTrainCrew(IPlayer sender, Snapshot.TrainCrew trainCrew)
	{
		StateManager.DebugAssertIsHost();
		trainCrew.Name = trainCrew.Name.Trim();
		if (trainCrew.Name.Length == 0)
		{
			throw new ArgumentException("Train crew name is empty", "trainCrew");
		}
		if (_trainCrews.Values.Any((TrainCrew crew) => crew.Name.Equals(trainCrew.Name)))
		{
			throw new ArgumentException("Train crew with this name already exists: " + trainCrew.Name, "trainCrew");
		}
		trainCrew.Id = IdGenerator.TrainCrew.Next();
		_trainCrews[trainCrew.Id] = new TrainCrew(trainCrew);
		Log.Information("Creating train crew {id} {name} with {members}", trainCrew.Id, trainCrew.Name, string.Join(", ", trainCrew.MemberPlayerIds));
		StateManager.ApplyLocal(new UpdateTrainCrews(TrainCrewsSnapshot()));
		Multiplayer.Broadcast($"{sender} created train crew \"{trainCrew.Name}\"");
	}

	public void HandleRequestDeleteTrainCrew(IPlayer sender, string trainCrewId)
	{
		StateManager.DebugAssertIsHost();
		if (!_trainCrews.TryGetValue(trainCrewId, out var value))
		{
			throw new ArgumentException("No train crew with id " + trainCrewId, "trainCrewId");
		}
		_trainCrews.Remove(trainCrewId);
		StateManager.ApplyLocal(new UpdateTrainCrews(TrainCrewsSnapshot()));
		Multiplayer.Broadcast($"{sender} disbanded train crew \"{value.Name}\"");
	}

	public void HandleRequestRenameTrainCrew(IPlayer sender, string trainCrewId, string newName, string newDesc)
	{
		if (string.IsNullOrEmpty(newName.Trim()))
		{
			throw new Exception("Invalid new name for train crew " + newName);
		}
		if (!TrainCrewForId(trainCrewId, out var trainCrew))
		{
			throw new Exception("No such train crew " + trainCrewId);
		}
		if (!(trainCrew.Name == newName) || !(trainCrew.Description == newDesc))
		{
			string text = trainCrew.Name;
			string text2 = trainCrew.Name;
			trainCrew.Name = newName;
			trainCrew.Description = newDesc;
			StateManager.ApplyLocal(new UpdateTrainCrews(TrainCrewsSnapshot()));
			if (text != newName)
			{
				Multiplayer.Broadcast($"{Hyperlink.To(sender)} renamed train crew \"{text}\" to \"{newName}\".");
			}
			else if (text2 != newDesc)
			{
				Multiplayer.Broadcast($"{Hyperlink.To(sender)} updated \"{newName}\".");
			}
		}
	}

	public void HandleRequestSetTrainCrewTimetableSymbol(IPlayer sender, string trainCrewId, string symbol)
	{
		if (!TrainCrewForId(trainCrewId, out var trainCrew))
		{
			throw new Exception("No such train crew " + trainCrewId);
		}
		if (string.IsNullOrEmpty(symbol))
		{
			symbol = null;
		}
		if (!(trainCrew.TimetableSymbol == symbol))
		{
			trainCrew.TimetableSymbol = symbol;
			StateManager.ApplyLocal(new UpdateTrainCrews(TrainCrewsSnapshot()));
			if (symbol == null)
			{
				Multiplayer.Broadcast("Train " + trainCrew.Name + " is no longer a timetable train.");
				return;
			}
			Multiplayer.Broadcast("Train " + trainCrew.Name + " is now timetable train " + symbol + ".");
		}
	}

	public void PopulateSnapshotForSave(ref Snapshot snapshot, ref Dictionary<string, PlayerRecord> playerStates)
	{
		snapshot.TrainCrews = TrainCrewsSnapshot();
		snapshot.players = null;
		playerStates = Multiplayer.PlayerRecordsForSave();
	}

	private void SetTrainCrews(Dictionary<string, Snapshot.TrainCrew> trainCrews)
	{
		_trainCrews = trainCrews.ToDictionary((KeyValuePair<string, Snapshot.TrainCrew> pair) => pair.Key, (KeyValuePair<string, Snapshot.TrainCrew> pair) => new TrainCrew(pair.Value));
		_orderedTrainCrews = _trainCrews.Values.OrderBy((TrainCrew tc) => tc.Name).ToList();
		Messenger.Default.Send(default(TrainCrewsDidChange));
	}

	private Dictionary<string, Snapshot.TrainCrew> TrainCrewsSnapshot()
	{
		return _trainCrews.ToDictionary((KeyValuePair<string, TrainCrew> kpv) => kpv.Key, (KeyValuePair<string, TrainCrew> kpv) => kpv.Value.ToSnapshot());
	}

	public void UpdateCameraPosition(Vector3 gamePosition, IPlayer sender)
	{
		_lastKnownPositions[sender.PlayerId] = new PlayerCameraPosition(gamePosition, Time.unscaledTime);
	}

	public bool IsPlayerCameraNear(Transform referenceTransform, float radius)
	{
		Vector3 b = WorldTransformer.WorldToGame(referenceTransform.position);
		float unscaledTime = Time.unscaledTime;
		foreach (var (_, playerCameraPosition2) in _lastKnownPositions)
		{
			if (!(unscaledTime - playerCameraPosition2.Time > 60f) && Vector3.Distance(playerCameraPosition2.Position, b) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public bool TryGetAccessLevel(PlayerId playerId, out AccessLevel accessLevel)
	{
		return _cachedAccessLevels.TryGetValue(playerId, out accessLevel);
	}

	public bool IsOnline(PlayerId playerId)
	{
		return _cachedAccessLevels.ContainsKey(playerId);
	}
}
