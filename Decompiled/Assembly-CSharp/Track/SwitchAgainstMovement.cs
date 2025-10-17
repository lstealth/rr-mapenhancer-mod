using System;

namespace Track;

public class SwitchAgainstMovement : Exception
{
	public readonly TrackNode Node;

	public SwitchAgainstMovement(TrackNode node)
	{
		Node = node;
	}
}
