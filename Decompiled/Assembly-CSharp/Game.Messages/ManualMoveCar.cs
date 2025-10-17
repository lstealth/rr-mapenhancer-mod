using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct ManualMoveCar : IGameMessage
{
	[Key(0)]
	public string CarId { get; set; }

	[Key(1)]
	public int Direction { get; set; }

	public ManualMoveCar(string carId, int direction)
	{
		CarId = carId;
		Direction = direction;
	}
}
