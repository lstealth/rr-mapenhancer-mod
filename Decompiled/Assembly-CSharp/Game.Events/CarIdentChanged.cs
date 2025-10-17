namespace Game.Events;

public struct CarIdentChanged
{
	public string CarId { get; }

	public CarIdentChanged(string carId)
	{
		CarId = carId;
	}
}
