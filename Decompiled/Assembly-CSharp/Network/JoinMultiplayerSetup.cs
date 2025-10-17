using Steamworks;

namespace Network;

public readonly struct JoinMultiplayerSetup : INetworkSetup
{
	public readonly CSteamID LobbyId;

	public readonly string Password;

	public JoinMultiplayerSetup(CSteamID lobbyId, string password)
	{
		LobbyId = lobbyId;
		Password = password;
	}
}
