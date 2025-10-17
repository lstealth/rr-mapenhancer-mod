using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct Rerail : IGameMessage
{
	[Key(0)]
	public string[] CarIds { get; set; }

	[Key(1)]
	public float Amount { get; set; }

	public Rerail(string[] carIds, float amount)
	{
		CarIds = carIds;
		Amount = amount;
	}
}
