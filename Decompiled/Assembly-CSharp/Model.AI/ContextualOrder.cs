using System.Collections.Generic;
using KeyValue.Runtime;

namespace Model.AI;

public struct ContextualOrder
{
	public enum OrderValue
	{
		Invalid,
		PassSignal,
		PassFlare,
		ResumeSpeed,
		BypassTimetable
	}

	public OrderValue Order;

	public string Context;

	public Value PropertyValue => Value.Dictionary(new Dictionary<string, Value>
	{
		{
			"order",
			(int)Order
		},
		{ "context", Context }
	});

	public ContextualOrder(OrderValue order, string context)
	{
		Order = order;
		Context = context;
	}

	public static ContextualOrder FromPropertyValue(Value value)
	{
		try
		{
			IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
			return new ContextualOrder((OrderValue)dictionaryValue["order"].IntValue, dictionaryValue["context"].StringValue);
		}
		catch
		{
			return default(ContextualOrder);
		}
	}
}
