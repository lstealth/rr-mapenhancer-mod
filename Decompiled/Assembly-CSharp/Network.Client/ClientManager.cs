using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using Network.Messages;
using Network.Server;
using Network.Steam;
using Serilog;
using UI.Common;
using UnityEngine;

namespace Network.Client;

public class ClientManager : MonoBehaviour, IClientDelegate
{
	private class TransactionCommitter : IDisposable
	{
		private readonly ClientManager _client;

		public TransactionCommitter(ClientManager client)
		{
			_client = client;
		}

		public void Dispose()
		{
			_client.TransactionCommit();
		}
	}

	private GameClient _client;

	private TaskCompletionSource<bool> _becameActiveCompletionSource;

	private TimeSynchronizer _timeSynchronizer;

	private string _password = "";

	private int _inTransaction;

	private readonly List<IGameMessage> _transactionMessages = new List<IGameMessage>();

	public PlayerId PlayerId { get; private set; }

	public long Tick
	{
		get
		{
			if (!(_timeSynchronizer != null))
			{
				return 0L;
			}
			return _timeSynchronizer.Tick;
		}
	}

	public bool IsClientStatusActive
	{
		get
		{
			GameClient client = _client;
			if ((object)client == null)
			{
				return false;
			}
			return client.ServerClientStatus == Network.Server.ClientStatus.Active;
		}
	}

	public AccessLevel AccessLevel { get; private set; }

	public event Action OnDisconnect;

	public event Action<Dictionary<string, Snapshot.Player>> OnRemotePlayersDidChange;

	public async Task Connect(ConnectionInfo info)
	{
		Disconnect();
		_client = CreateClient(info);
		_client.ClientDelegate = this;
		try
		{
			await _client.Connect();
			_timeSynchronizer = base.gameObject.AddComponent<TimeSynchronizer>();
			_timeSynchronizer.Client = _client;
		}
		catch
		{
			UnityEngine.Object.DestroyImmediate(_client);
			_client = null;
			throw;
		}
	}

	private GameClient CreateClient(ConnectionInfo info)
	{
		if (info.IsMultiplayerClient)
		{
			SteamClient steamClient = base.gameObject.AddComponent<SteamClient>();
			steamClient.Setup(info);
			return steamClient;
		}
		LocalGameClient localGameClient = base.gameObject.AddComponent<LocalGameClient>();
		localGameClient.Setup(info, HostManager.Shared);
		return localGameClient;
	}

	public void Disconnect()
	{
		if (_client != null)
		{
			try
			{
				this.OnDisconnect?.Invoke();
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error during OnDisconnect");
				Debug.LogException(exception);
			}
			UnityEngine.Object.DestroyImmediate(_client);
			_client = null;
		}
		if (_timeSynchronizer != null)
		{
			UnityEngine.Object.DestroyImmediate(_timeSynchronizer);
			_timeSynchronizer = null;
		}
	}

	public void Send(IGameMessage message, bool forceReliable = false)
	{
		if (_client == null)
		{
			Log.Warning("Send with null _socket");
			return;
		}
		if (_inTransaction > 0)
		{
			_transactionMessages.Add(message);
			return;
		}
		Channel channel = Multiplayer.ChannelForMessage(message, forceReliable);
		_client.Send(message, channel);
	}

	public void ClientDidDisconnect(int endReason)
	{
		Log.Error("ClientDidDisconnect: {disconnectReason}", (DisconnectReason)endReason);
		string text = endReason switch
		{
			1001 => null, 
			1002 => "The passenger compartment is full.", 
			2001 => "We couldn't verify your employee status with this railroad.", 
			2002 => "Your game version is not compatible with the server.", 
			2003 => "This railroad requires a password for new employees.", 
			5010 => "Server closed connection.", 
			2999 => "Server closed connection.", 
			5003 => "Connection timed out.", 
			_ => $"Error {(DisconnectReason)endReason}", 
		};
		if (text != null)
		{
			ModalAlertController.PresentOkay("Disconnected", text);
		}
		Disconnect();
	}

	public void ClientErrorOccurred(string displayText)
	{
		Log.Error("ClientErrorOccurred: {displayText}", displayText);
		Console.Log("ClientErrorOccurred: " + displayText);
		_becameActiveCompletionSource?.SetException(new Exception("ClientErrorOccurred"));
		this.OnDisconnect?.Invoke();
	}

	public void ClientStatusDidChange(Network.Server.ClientStatus clientStatus, PlayerId playerId, AccessLevel accessLevel)
	{
		Log.Information("ClientStatusDidChange {status} {playerId} {accessLevel}", clientStatus, playerId, accessLevel);
		PlayerId = playerId;
		_client.PlayerId = playerId;
		AccessLevel accessLevel2 = AccessLevel;
		AccessLevel = accessLevel;
		switch (clientStatus)
		{
		case Network.Server.ClientStatus.Authenticated:
			RequestActive();
			break;
		case Network.Server.ClientStatus.Active:
			_timeSynchronizer.Synchronize();
			_becameActiveCompletionSource?.SetResult(result: true);
			SendCharacter();
			if (accessLevel2 != accessLevel)
			{
				Messenger.Default.Send(new AccessLevelDidChange(accessLevel2, accessLevel));
			}
			break;
		default:
			throw new ArgumentOutOfRangeException("clientStatus", clientStatus, null);
		case Network.Server.ClientStatus.Initial:
		case Network.Server.ClientStatus.Anonymous:
			break;
		}
	}

	public void ClientDidReceiveGameMessage(PlayerId sender, IGameMessage message)
	{
		if (!sender.IsValid)
		{
			throw new ArgumentException($"Unexpected message with invalid sender: {message}");
		}
		StateManager.Shared.Handle(message, sender);
	}

	public void ClientDidReceivePlayerList(Dictionary<string, Snapshot.Player> players)
	{
		this.OnRemotePlayersDidChange?.Invoke(players);
	}

	public void ClientDidReceiveSnapshot(Snapshot snapshot)
	{
		Log.Debug("Snapshot: {sets} sets, {players} players, {properties} property objects", snapshot.CarSets.Count, snapshot.players?.Count, snapshot.Properties.Count);
		StateManager shared = StateManager.Shared;
		try
		{
			shared.PopulateFromRemoteSnapshot(snapshot);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception while populating from snapshot");
			Debug.LogException(exception);
			if (StateManager.IsHost)
			{
				shared.ReturnToMainMenuWithError("Error loading game", "An error occurred while loading this save. Please open a bug report and upload your save file!");
			}
			else
			{
				shared.ReturnToMainMenuWithError("Error loading server game", "An error occurred while loading data received from the server.");
			}
		}
	}

	public void ClientDidReceiveAlert(Alert alert)
	{
		WindowManager.Shared.Present(alert);
	}

	public void ClientDidReceiveTimeSync(TimeSync timeSync)
	{
		_timeSynchronizer.DidReceiveServerTick(timeSync.tick);
	}

	public void ClientDidReceivePasswordPrompt()
	{
		ModalAlertController.Present("Password Required", "A password is required to join this game.", _password, new(bool, string)[2]
		{
			(false, "Cancel"),
			(true, "Submit")
		}, delegate((bool, string) tuple)
		{
			var (flag, password) = tuple;
			if (flag)
			{
				_password = password;
				_client.LoginWithPassword(_password);
			}
			else
			{
				Disconnect();
			}
		});
	}

	public static Snapshot.CharacterCustomization MakeCharacterCustomizationUsingPreferences(bool lanternEnabled)
	{
		return Preferences.AvatarDescriptor.SettingAccessory("lantern", Value.Bool(lanternEnabled)).ToCharacterCustomization();
	}

	public void SendCharacter()
	{
		string multiplayerClientUsername = Preferences.MultiplayerClientUsername;
		Snapshot.CharacterCustomization customization = MakeCharacterCustomizationUsingPreferences(CameraSelector.shared.localAvatar.LanternEnabled);
		Send(new AddUpdateCharacter(multiplayerClientUsername, customization));
	}

	public async Task RequestActive()
	{
		_becameActiveCompletionSource = new TaskCompletionSource<bool>();
		_client.SendNetworkMessage(default(RequestActive), Channel.Message);
		await _becameActiveCompletionSource.Task;
		_becameActiveCompletionSource = null;
	}

	public IDisposable TransactionScope()
	{
		TransactionStart();
		return new TransactionCommitter(this);
	}

	public void TransactionStart()
	{
		_inTransaction++;
	}

	public void TransactionCommit()
	{
		_inTransaction--;
		if (_inTransaction <= 0 && _transactionMessages.Count != 0)
		{
			int num = _transactionMessages.FindIndex((IGameMessage tm) => tm is AddCars);
			if (num >= 0)
			{
				IGameMessage item = _transactionMessages[num];
				_transactionMessages.RemoveAt(num);
				_transactionMessages.Insert(0, item);
			}
			Log.Debug("Sending transaction: {messages}", _transactionMessages);
			Transaction transaction = new Transaction(new List<IGameMessage>(_transactionMessages));
			Send(transaction);
			_transactionMessages.Clear();
		}
	}
}
