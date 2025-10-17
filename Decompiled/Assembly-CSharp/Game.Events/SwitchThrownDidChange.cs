using Track;

namespace Game.Events;

public struct SwitchThrownDidChange
{
	public readonly TrackNode Node;

	public SwitchThrownDidChange(TrackNode node)
	{
		Node = node;
	}
}
