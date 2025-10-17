using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct SetPassengerAutoDestinations : IGameMessage
{
	[Key(0)]
	public string CarId { get; set; }

	[Key(1)]
	public bool Enabled { get; set; }

	public SetPassengerAutoDestinations(string carId, bool enabled)
	{
		CarId = carId;
		Enabled = enabled;
	}
}
