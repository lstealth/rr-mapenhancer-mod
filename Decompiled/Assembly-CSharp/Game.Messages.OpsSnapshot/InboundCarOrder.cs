using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct InboundCarOrder : ICarOrder
{
	[Key(0)]
	public string OriginatorIndustryId;

	[Key(1)]
	public CarContent InboundContent;

	[Key(2)]
	public CarPosition InboundPosition;

	[Key(6)]
	public string CarTypeIdentifier;

	[Key(7)]
	public FlipInfo? FlipInfo;
}
