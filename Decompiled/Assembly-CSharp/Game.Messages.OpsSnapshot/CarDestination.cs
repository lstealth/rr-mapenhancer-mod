using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct CarDestination
{
	[Key(0)]
	public string Name;

	[Key(1)]
	public CarPosition Position;
}
