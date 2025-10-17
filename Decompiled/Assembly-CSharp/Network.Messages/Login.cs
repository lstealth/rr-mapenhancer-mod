using Game.Messages;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct Login : INetworkMessage
{
	[Key(0)]
	public string Name;

	[Key(1)]
	public string Password;

	[Key(2)]
	public Snapshot.CharacterCustomization Customization;

	public Login(string name, string password, Snapshot.CharacterCustomization customization)
	{
		Name = name;
		Password = password;
		Customization = customization;
	}
}
