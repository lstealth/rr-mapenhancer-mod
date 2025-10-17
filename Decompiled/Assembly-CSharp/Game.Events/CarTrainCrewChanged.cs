namespace Game.Events;

public struct CarTrainCrewChanged
{
	public string CarId { get; }

	public CarTrainCrewChanged(string carId)
	{
		CarId = carId;
	}
}
