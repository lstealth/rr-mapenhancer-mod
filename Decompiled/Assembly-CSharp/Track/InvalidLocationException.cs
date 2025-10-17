using System;
using Model;

namespace Track;

[Serializable]
public class InvalidLocationException : Exception
{
	public string location;

	public string entity;

	public string context;

	public InvalidLocationException(Location loc, Car car, Car.End end)
		: base("Car has invalid location")
	{
		location = loc.ToString();
		entity = car.DisplayName;
		context = end.ToString();
	}

	public InvalidLocationException(string message)
		: base(message)
	{
	}
}
