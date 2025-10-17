using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct LedgerRequest : IGameMessage
{
	[Key(0)]
	public float Start;

	[Key(1)]
	public float End;

	public LedgerRequest(float start, float end)
	{
		Start = start;
		End = end;
	}
}
