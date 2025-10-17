using MessagePack;
using Network.Messages;

namespace Network;

[Union(0, typeof(Hello))]
[Union(1, typeof(Goodbye))]
[Union(2, typeof(Login))]
[Union(3, typeof(ClientStatus))]
[Union(4, typeof(RequestActive))]
[Union(5, typeof(PlayerList))]
[Union(6, typeof(TimeSync))]
[Union(7, typeof(PasswordPrompt))]
[Union(8, typeof(NetworkMessageEnvelope))]
[Union(10, typeof(GameMessageEnvelope))]
[Union(11, typeof(SnapshotEnvelope))]
[Union(12, typeof(Alert))]
[Union(13, typeof(SetPlayerPosition))]
public interface INetworkMessage
{
}
