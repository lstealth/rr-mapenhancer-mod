using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct UpdateCharacterPosition : ICharacterMessage, IGameMessage
{
	[Key(0)]
	public CharacterPosition Position { get; set; }

	[Key(1)]
	public Vector3 Velocity { get; set; }

	[Key(2)]
	public CharacterPose Pose { get; set; }

	[Key(3)]
	public long Tick { get; set; }

	public UpdateCharacterPosition(CharacterPosition position, Vector3 velocity, CharacterPose pose, long tick)
	{
		Position = position;
		Velocity = velocity;
		Pose = pose;
		Tick = tick;
	}
}
