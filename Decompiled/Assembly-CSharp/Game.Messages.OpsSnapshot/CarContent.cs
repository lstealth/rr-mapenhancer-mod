using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct CarContent
{
	[Key(0)]
	public string LoadId;

	[Key(1)]
	public string Description;
}
