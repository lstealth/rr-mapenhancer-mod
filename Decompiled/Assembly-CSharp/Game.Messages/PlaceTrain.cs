using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct PlaceTrain : IGameMessage
{
	[MessagePackObject(false)]
	public struct Car
	{
		[Key(0)]
		public string PrototypeId;

		[Key(1)]
		public bool Flipped;

		[Key(2)]
		public string ReportingMark;

		[Key(3)]
		public string RoadNumber;

		[Key(4)]
		public string TrainCrewId;

		[Key(5)]
		public Dictionary<string, IPropertyValue> Properties;

		public Car(string prototypeId, bool flipped, string reportingMark, string roadNumber, string trainCrewId, Dictionary<string, IPropertyValue> properties)
		{
			PrototypeId = prototypeId;
			Flipped = flipped;
			ReportingMark = reportingMark;
			RoadNumber = roadNumber;
			TrainCrewId = trainCrewId;
			Properties = properties;
		}
	}

	[Key(0)]
	public Snapshot.TrackLocation Location;

	[Key(1)]
	public List<Car> Cars;

	[Key(2)]
	public List<string> CarIds;

	[Key(3)]
	public PlaceTrainHandbrakes Handbrakes;

	public PlaceTrain(Snapshot.TrackLocation location, List<Car> cars, List<string> carIds, PlaceTrainHandbrakes handbrakes)
	{
		Location = location;
		Cars = cars;
		CarIds = carIds;
		Handbrakes = handbrakes;
	}
}
