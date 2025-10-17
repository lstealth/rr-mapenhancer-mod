using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct SetGladhandsConnected : IGameMessage
{
	[Key(0)]
	public string CarIdA;

	[Key(1)]
	public string CarIdB;

	[Key(2)]
	public bool Connected;

	public SetGladhandsConnected(string carIdA, string carIdB, bool connected)
	{
		CarIdA = carIdA;
		CarIdB = carIdB;
		Connected = connected;
	}
}
