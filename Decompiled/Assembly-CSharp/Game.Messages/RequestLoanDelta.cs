using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct RequestLoanDelta : IGameMessage
{
	[Key(0)]
	public int Delta { get; set; }

	public RequestLoanDelta(int delta)
	{
		Delta = delta;
	}
}
