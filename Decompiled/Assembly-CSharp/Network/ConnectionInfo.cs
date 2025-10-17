using Game.Messages;
using Steamworks;

namespace Network;

public struct ConnectionInfo
{
	public readonly ConnectionMode Mode;

	public readonly CSteamID GameServerId;

	public readonly string Username;

	public string Password;

	public readonly Snapshot.CharacterCustomization Customization;

	public bool IsSingleplayer => Mode == ConnectionMode.Singleplayer;

	public bool IsMultiplayerServer => Mode == ConnectionMode.MultiplayerServer;

	public bool IsMultiplayerClient => Mode == ConnectionMode.MultiplayerClient;

	private ConnectionInfo(ConnectionMode mode, CSteamID gameServerId, string username, string password, Snapshot.CharacterCustomization customization)
	{
		Mode = mode;
		GameServerId = gameServerId;
		Username = username;
		Password = password;
		Customization = customization;
	}

	public static ConnectionInfo MultiplayerHost(string username, string password, Snapshot.CharacterCustomization customization)
	{
		return new ConnectionInfo(ConnectionMode.MultiplayerServer, CSteamID.Nil, username, password, customization);
	}

	public static ConnectionInfo MultiplayerClient(CSteamID gameServerId, string username, string password, Snapshot.CharacterCustomization customization)
	{
		return new ConnectionInfo(ConnectionMode.MultiplayerClient, gameServerId, username, password, customization);
	}

	public static ConnectionInfo Singleplayer(string username, Snapshot.CharacterCustomization customization)
	{
		return new ConnectionInfo(ConnectionMode.Singleplayer, CSteamID.Nil, username, null, customization);
	}
}
