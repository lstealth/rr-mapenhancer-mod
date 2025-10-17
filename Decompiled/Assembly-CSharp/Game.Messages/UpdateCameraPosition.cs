using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct UpdateCameraPosition : ICharacterMessage, IGameMessage
{
	[Key(0)]
	public Vector3 Position { get; set; }

	public UpdateCameraPosition(Vector3 position)
	{
		Position = position;
	}
}
