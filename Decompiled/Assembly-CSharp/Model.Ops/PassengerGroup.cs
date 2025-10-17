using System;
using System.Collections.Generic;
using Game;
using KeyValue.Runtime;

namespace Model.Ops;

public struct PassengerGroup
{
	public string Origin;

	public string Destination;

	public int Count;

	public GameDateTime Boarded;

	public PassengerGroup(string origin, string destination, int count, GameDateTime boarded)
	{
		Origin = origin;
		Destination = destination;
		Count = count;
		Boarded = boarded;
	}

	public override string ToString()
	{
		return $"{Count} {Origin} {Boarded} -> {Destination}";
	}

	public static PassengerGroup FromPropertyValue(Value value)
	{
		if (value.Type != KeyValue.Runtime.ValueType.Dictionary)
		{
			throw new Exception("Unexpected type");
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new PassengerGroup
		{
			Origin = dictionaryValue["origin"].StringValue,
			Destination = dictionaryValue["dest"].StringValue,
			Count = dictionaryValue["count"].IntValue,
			Boarded = new GameDateTime(dictionaryValue["boarded"].IntValue)
		};
	}

	public Value PropertyValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			["origin"] = Value.String(Origin),
			["dest"] = Value.String(Destination),
			["count"] = Value.Int(Count),
			["boarded"] = Value.Int((int)Boarded.TotalSeconds)
		});
	}
}
