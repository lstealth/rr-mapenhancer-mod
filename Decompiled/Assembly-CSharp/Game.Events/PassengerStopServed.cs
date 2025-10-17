namespace Game.Events;

public struct PassengerStopServed
{
	public readonly string Identifier;

	public readonly int Offset;

	public readonly float CarCondition;

	public PassengerStopServed(string identifier, int offset, float carCondition)
	{
		Identifier = identifier;
		Offset = offset;
		CarCondition = carCondition;
	}
}
