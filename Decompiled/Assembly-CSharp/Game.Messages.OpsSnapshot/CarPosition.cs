using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct CarPosition
{
	[Key(0)]
	public string LocationId;

	[Key(1)]
	public int TrackIndex;
}
