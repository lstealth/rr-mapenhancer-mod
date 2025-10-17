using System;
using System.Collections.Generic;
using KeyValue.Runtime;

namespace Model.AI;

public struct OrderWaypoint : IEquatable<OrderWaypoint>
{
	public string LocationString;

	public string CoupleToCarId;

	public bool WantsCouple => !string.IsNullOrEmpty(CoupleToCarId);

	public Value PropertyValue => Value.Dictionary(new Dictionary<string, Value>
	{
		{ "loc", LocationString },
		{ "cpl", CoupleToCarId }
	});

	public OrderWaypoint(string locationString, string coupleToCarId)
	{
		LocationString = locationString;
		CoupleToCarId = coupleToCarId;
	}

	public static OrderWaypoint? FromPropertyValue(Value value)
	{
		if (value.IsNull)
		{
			return null;
		}
		return new OrderWaypoint(value["loc"], value["cpl"]);
	}

	public bool Equals(OrderWaypoint other)
	{
		if (LocationString == other.LocationString)
		{
			return CoupleToCarId == other.CoupleToCarId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is OrderWaypoint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(LocationString, CoupleToCarId);
	}
}
