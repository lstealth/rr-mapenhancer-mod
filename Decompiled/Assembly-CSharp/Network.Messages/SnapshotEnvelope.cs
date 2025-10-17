using Game.Messages;
using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct SnapshotEnvelope : INetworkMessage
{
	[Key(0)]
	public Snapshot Snapshot;

	public SnapshotEnvelope(Snapshot snapshot)
	{
		Snapshot = snapshot;
	}

	public override string ToString()
	{
		return Snapshot.ToString();
	}
}
