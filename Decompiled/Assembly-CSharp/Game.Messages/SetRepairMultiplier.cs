using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct SetRepairMultiplier : IGameMessage
{
	[Key(0)]
	public string Id;

	[Key(1)]
	public float Multiplier;

	public SetRepairMultiplier(string id, float multiplier)
	{
		Id = id;
		Multiplier = multiplier;
	}

	public override string ToString()
	{
		return $"SetRepairMultiplier: {Id} {Multiplier}";
	}
}
