namespace Network.Server;

public interface IServerManager
{
	void DropClient(ClientId clientId, int steamworksReasonCode, string debugReason);

	void SendTo(ClientId clientId, INetworkMessage networkMessage);
}
