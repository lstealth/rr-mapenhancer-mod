using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct PlaySoundNotification : IGameMessage
{
	[Key(0)]
	public string Name { get; set; }

	[Key(1)]
	public float Volume { get; set; }

	[Key(2)]
	public float Pitch { get; set; }

	public PlaySoundNotification(string name, float volume, float pitch)
	{
		Name = name;
		Volume = volume;
		Pitch = pitch;
	}
}
