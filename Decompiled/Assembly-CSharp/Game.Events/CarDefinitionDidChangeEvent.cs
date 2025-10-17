namespace Game.Events;

public struct CarDefinitionDidChangeEvent
{
	public readonly string CarIdentifier;

	public CarDefinitionDidChangeEvent(string carIdentifier)
	{
		CarIdentifier = carIdentifier;
	}
}
