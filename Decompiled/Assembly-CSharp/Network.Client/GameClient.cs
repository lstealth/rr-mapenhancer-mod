using System;
using System.Threading.Tasks;
using Game;
using Game.AccessControl;
using Game.Messages;
using Network.Messages;
using Network.Server;
using Serilog;
using UnityEngine;

namespace Network.Client;

public abstract class GameClient : MonoBehaviour
{
	public IClientDelegate ClientDelegate { get; set; }

	public Network.Server.ClientStatus ServerClientStatus { get; protected set; }

	public abstract bool IsConnectedOrConnecting { get; }

	protected ConnectionInfo ConnectionInfo { get; private set; }

	public PlayerId PlayerId { get; set; }

	public abstract Task Connect();

	public abstract void Disconnect();

	public abstract void SendNetworkMessage(INetworkMessage message, Channel channel);

	public virtual void Setup(ConnectionInfo connectionInfo)
	{
		ConnectionInfo = connectionInfo;
		MessagepackSupport.Setup();
	}

	public virtual void OnDestroy()
	{
	}

	public void Send(IGameMessage message, Channel channel)
	{
		if (ServerClientStatus != Network.Server.ClientStatus.Active)
		{
			Log.Warning("Will not send message {message}, status is {ServerClientStatus}", message, ServerClientStatus);
		}
		else
		{
			SendNetworkMessage(new GameMessageEnvelope(PlayerId.String, message), channel);
		}
	}

	protected void HandleMessage(INetworkMessage message)
	{
		if (!(message is TimeSync timeSync))
		{
			if (!(message is Network.Messages.ClientStatus clientStatus))
			{
				if (!(message is PasswordPrompt))
				{
					if (!(message is PlayerList playerList))
					{
						if (!(message is SnapshotEnvelope snapshotEnvelope))
						{
							if (!(message is Alert alert))
							{
								if (message is GameMessageEnvelope propertyValue)
								{
									if (propertyValue.gameMessage == null)
									{
										throw new ArgumentException("null gameMessage - missing union attribute?");
									}
									if (string.IsNullOrEmpty(propertyValue.sender))
									{
										throw new ArgumentException("Envelope has null or empty sender");
									}
									try
									{
										ClientDelegate.ClientDidReceiveGameMessage(new PlayerId(propertyValue.sender), propertyValue.gameMessage);
										return;
									}
									catch (Exception exception)
									{
										Log.Error(exception, "Error while handling {envelope}", propertyValue);
										return;
									}
								}
								if (message is SetPlayerPosition propertyValue2)
								{
									Log.Debug("Received SetPlayerPosition {setPos}", propertyValue2);
									try
									{
										CharacterPosition position = propertyValue2.Position;
										CameraSelector.shared.JumpCharacterTo(position.Position, position.RelativeToCarId, position.Look);
									}
									catch (Exception exception2)
									{
										Log.Error(exception2, "Failed to SetPlayerPosition; jumping to spawn.");
										CameraSelector.shared.JumpToSpawn();
									}
								}
							}
							else
							{
								ClientDelegate.ClientDidReceiveAlert(alert);
							}
						}
						else
						{
							ClientDelegate.ClientDidReceiveSnapshot(snapshotEnvelope.Snapshot);
						}
					}
					else
					{
						Log.Information("Received player list of {count} entries.", playerList.Players.Count);
						ClientDelegate.ClientDidReceivePlayerList(playerList.Players);
					}
				}
				else
				{
					ClientDelegate.ClientDidReceivePasswordPrompt();
				}
			}
			else
			{
				switch (clientStatus.Status)
				{
				case Network.Server.ClientStatus.Anonymous:
					SendLogin();
					break;
				}
				ServerClientStatus = clientStatus.Status;
				ClientDelegate.ClientStatusDidChange(clientStatus.Status, new PlayerId(clientStatus.PlayerId), (AccessLevel)clientStatus.AccessLevel);
			}
		}
		else
		{
			ClientDelegate.ClientDidReceiveTimeSync(timeSync);
		}
	}

	private void SendLogin()
	{
		SendNetworkMessage(new Login(ConnectionInfo.Username, ConnectionInfo.Password, ConnectionInfo.Customization), Channel.Message);
	}

	protected void SendHello()
	{
		SendNetworkMessage(new Hello
		{
			MajorVersion = Common.CurrentVersion.Major,
			MinorVersion = Common.CurrentVersion.Minor
		}, Channel.Message);
	}

	public void LoginWithPassword(string password)
	{
		ConnectionInfo connectionInfo = ConnectionInfo;
		connectionInfo.Password = password;
		ConnectionInfo = connectionInfo;
		SendLogin();
	}
}
