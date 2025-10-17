using System;
using System.Collections.Generic;
using Game;
using KeyValue.Runtime;

namespace Model.Ops;

public struct WaitingPassengerGroup
{
	public string Origin;

	public int Count;

	public GameDateTime Boarded;

	public WaitingPassengerGroup(string origin, int count, GameDateTime boarded)
	{
		Origin = origin;
		Count = count;
		Boarded = boarded;
	}

	public override string ToString()
	{
		return $"{Count} {Origin} {Boarded}";
	}

	public static WaitingPassengerGroup FromPropertyValue(Value value)
	{
		if (value.Type != KeyValue.Runtime.ValueType.Dictionary)
		{
			throw new Exception("Unexpected type");
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new WaitingPassengerGroup
		{
			Origin = dictionaryValue["origin"].StringValue,
			Count = dictionaryValue["count"].IntValue,
			Boarded = new GameDateTime(dictionaryValue["boarded"].IntValue)
		};
	}

	public Value PropertyValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			["origin"] = Value.String(Origin),
			["count"] = Value.Int(Count),
			["boarded"] = Value.Int((int)Boarded.TotalSeconds)
		});
	}
}
