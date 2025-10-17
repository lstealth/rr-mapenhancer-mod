using UnityEngine;

namespace Game.Events;

public struct WorldDidMoveEvent
{
	public Vector3 Offset;

	public WorldDidMoveEvent(Vector3 offset)
	{
		Offset = offset;
	}
}
