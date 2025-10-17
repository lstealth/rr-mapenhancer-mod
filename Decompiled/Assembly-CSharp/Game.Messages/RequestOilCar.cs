using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct RequestOilCar : IGameMessage
{
	[Key(0)]
	public string CarId;

	[Key(1)]
	public float Amount;

	public RequestOilCar(string carId, float amount)
	{
		CarId = carId;
		Amount = amount;
	}
}
