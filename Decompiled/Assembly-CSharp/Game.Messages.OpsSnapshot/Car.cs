using MessagePack;

namespace Game.Messages.OpsSnapshot;

[MessagePackObject(false)]
public struct Car
{
	[Key(0)]
	public string Id;

	[Key(1)]
	public string CarTypeIdentifier;

	[Key(2)]
	public string AreaId;

	[Key(3)]
	public CarPosition? Position;

	[Key(4)]
	public CarDestination? Destination;

	[Key(5)]
	public CarContent Content;

	[Key(6)]
	public FlipInfo? FlipInfo;

	[Key(7)]
	public CarPosition OriginPosition;

	[Key(8)]
	public int OriginTime;

	[Key(9)]
	public bool DeliveryHandled;
}
