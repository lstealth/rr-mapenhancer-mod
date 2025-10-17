using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct CarSetBardo : IGameMessage
{
	[Key(0)]
	public string CarId;

	[Key(1)]
	public string Bardo;

	public CarSetBardo(string carId, string bardo)
	{
		CarId = carId;
		Bardo = bardo;
	}

	public override string ToString()
	{
		return "CarSetBardo: " + CarId + " " + Bardo;
	}
}
