using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct FlipInfo
{
	[Key(0)]
	public int TimeRemaining;

	[Key(1)]
	public CarContent CarContent;

	[Key(2)]
	public CarDestination? CarDestination;
}
