using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct CharacterPosition
{
	[Key(0)]
	public Vector3 Position { get; set; }

	[Key(1)]
	public string RelativeToCarId { get; set; }

	[Key(2)]
	public Vector3 Forward { get; set; }

	[Key(3)]
	public Vector3 Look { get; set; }

	public CharacterPosition(Vector3 position, string relativeToCarId, Vector3 forward, Vector3 look)
	{
		Position = position;
		RelativeToCarId = relativeToCarId;
		Forward = forward;
		Look = look;
	}
}
