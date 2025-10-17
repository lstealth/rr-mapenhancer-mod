using Network.Steam;

namespace Network;

public readonly struct StartMultiplayerHostSetup : INetworkSetup
{
	public readonly string LobbyName;

	public readonly LobbyType LobbyType;

	public StartMultiplayerHostSetup(string lobbyName, LobbyType lobbyType)
	{
		LobbyName = lobbyName;
		LobbyType = lobbyType;
	}
}
