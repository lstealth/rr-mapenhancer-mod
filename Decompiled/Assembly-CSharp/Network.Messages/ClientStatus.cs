using MessagePack;
using Network.Server;

namespace Network.Messages;

[MessagePackObject(false)]
public struct ClientStatus : INetworkMessage
{
	[Key(0)]
	public Network.Server.ClientStatus Status;

	[Key(1)]
	public string PlayerId;

	[Key(2)]
	public int AccessLevel;

	public ClientStatus(Network.Server.ClientStatus status, string playerId, int accessLevel)
	{
		Status = status;
		PlayerId = playerId;
		AccessLevel = accessLevel;
	}
}
