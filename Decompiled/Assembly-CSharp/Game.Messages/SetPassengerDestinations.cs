using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct SetPassengerDestinations : IGameMessage
{
	[Key(0)]
	public string CarId { get; set; }

	[Key(1)]
	public List<string> Destinations { get; set; }

	public SetPassengerDestinations(string carId, List<string> destinations)
	{
		CarId = carId;
		Destinations = destinations;
	}
}
