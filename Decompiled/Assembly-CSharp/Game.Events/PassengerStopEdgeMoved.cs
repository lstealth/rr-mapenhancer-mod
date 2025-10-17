namespace Game.Events;

public struct PassengerStopEdgeMoved
{
	public string From;

	public string To;

	public PassengerStopEdgeMoved(string from, string to)
	{
		From = from;
		To = to;
	}
}
