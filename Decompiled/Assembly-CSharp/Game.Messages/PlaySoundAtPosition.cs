using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct PlaySoundAtPosition : IGameMessage
{
	[Key(0)]
	public long Tick { get; set; }

	[Key(1)]
	public string Name { get; set; }

	[Key(2)]
	public Vector3 Position { get; set; }

	[Key(3)]
	public float Volume { get; set; }

	[Key(4)]
	public float Pitch { get; set; }

	[Key(5)]
	public int Distance { get; set; }

	[Key(6)]
	public string GroupPath { get; set; }

	[Key(7)]
	public int Priority { get; set; }

	public PlaySoundAtPosition(long tick, string name, Vector3 position, float volume, float pitch, int distance, string groupPath, int priority)
	{
		Tick = tick;
		Name = name;
		Position = position;
		Volume = volume;
		Pitch = pitch;
		Distance = distance;
		GroupPath = groupPath;
		Priority = priority;
	}
}
