namespace Network.Server;

public interface IServerDelegate
{
	bool ShouldAcceptConnection(ulong remotePlayerSteamId);

	void HandleMessage(IServerManager server, ClientId clientId, INetworkMessage message);

	void ClientDidConnect(IServerManager server, ClientId clientId, ulong remotePlayerSteamId);

	void ClientDidDisconnect(IServerManager server, ClientId clientId);
}
