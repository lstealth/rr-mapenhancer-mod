using MessagePack;

namespace Game.Messages.OpsSnapshot;

[Union(0, typeof(InboundCarOrder))]
[Union(1, typeof(OutboundCarOrder))]
public interface ICarOrder
{
}
