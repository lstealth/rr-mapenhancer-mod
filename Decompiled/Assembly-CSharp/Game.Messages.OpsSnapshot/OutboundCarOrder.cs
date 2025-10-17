using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct OutboundCarOrder : ICarOrder
{
	[Key(0)]
	public string OriginatorIndustryId;

	[Key(1)]
	public string CarId;
}
