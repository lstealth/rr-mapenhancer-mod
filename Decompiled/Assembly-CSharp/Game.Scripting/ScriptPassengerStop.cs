using Model.Ops;

namespace Game.Scripting;

public class ScriptPassengerStop
{
	private readonly PassengerStop _passengerStop;

	public string identifier => _passengerStop.identifier;

	public string name => _passengerStop.DisplayName;

	internal ScriptPassengerStop(PassengerStop identifier)
	{
		_passengerStop = identifier;
	}

	public void offset_passengers_waiting(string destinationId, string originId, int offset)
	{
		_passengerStop.OffsetWaitingOpsCommand(destinationId, originId, TimeWeather.Now, offset);
	}

	public int get_passengers_waiting(string destinationId)
	{
		return _passengerStop.GetTotalWaitingForDestination(destinationId);
	}
}
