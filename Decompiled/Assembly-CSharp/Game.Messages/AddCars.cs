using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct AddCars : IGameMessage
{
	[Key(0)]
	public List<Snapshot.Car> Cars;

	public AddCars(List<Snapshot.Car> cars)
	{
		Cars = cars;
	}
}
