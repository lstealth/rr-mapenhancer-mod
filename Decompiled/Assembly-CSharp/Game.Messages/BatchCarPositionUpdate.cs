using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct BatchCarPositionUpdate : IGameMessage
{
	[Key(0)]
	public uint Id;

	[Key(1)]
	public long Tick;

	[Key(2)]
	public Snapshot.TrackLocation StartLocation;

	[Key(3)]
	public float[] Positions;

	[Key(4)]
	public ushort[] Velocities;

	[Key(5)]
	public bool Critical;

	public BatchCarPositionUpdate(uint carSetId, long tick, Snapshot.TrackLocation startLocation, float[] positions, ushort[] velocities, bool critical)
	{
		Id = carSetId;
		Tick = tick;
		StartLocation = startLocation;
		Positions = positions;
		Velocities = velocities;
		Critical = critical;
	}
}
